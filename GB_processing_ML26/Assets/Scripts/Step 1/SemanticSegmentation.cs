using UnityEngine;
using UnityEngine.UI;
using Unity.InferenceEngine;
using System.Collections.Generic;

public class SemanticSegmentation : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your prime image (800x767) here")]
    public Texture2D primeImage;

    [Tooltip("Drag your DeepLabV3-MobileNet ONNX file here")]
    public ModelAsset modelAsset;

    [Tooltip("Drag a RawImage from your Canvas here to display the zone map")]
    public RawImage zoneMapDisplay;

    [Header("Model Settings")]
    public int inputWidth = 520;    // Model expects 520x520
    public int inputHeight = 520;

    [Header("Zone Colors")]
    public List<Color> zoneColors = new List<Color>();

    private Worker worker;
    private Tensor<float> inputTensor;
    private Texture2D zoneMapTexture;

    void Start()
    {
        if (modelAsset == null || primeImage == null)
        {
            Debug.LogError("Missing model asset or prime image!");
            return;
        }

        if (zoneColors.Count == 0) InitializeZoneColors();
        RunSegmentation();
    }

    void RunSegmentation()
    {
        Debug.Log($"Starting semantic segmentation with input size {inputWidth}x{inputHeight}...");

        // Load the model
        Model model = ModelLoader.Load(modelAsset);
        LogModelInfo(model); // Debug: list all inputs and outputs

        // Create the worker
        worker = new Worker(model, BackendType.GPUCompute);

        // Convert texture to tensor with correct format and size
        inputTensor = TextureToTensor(primeImage, inputWidth, inputHeight);

        // Log the tensor shape for debugging
        Debug.Log($"Created tensor with shape: {inputTensor.shape}");

        // Schedule inference
        worker.Schedule(inputTensor);

        // Get output tensor (still on GPU)
        Tensor<float> outputTensor = null;

        // Try different output names
        string[] possibleOutputNames = { "output", "output_0", "output_1", "out", "pred", "logits", "sigmoid", "softmax" };
        foreach (string name in possibleOutputNames)
        {
            try
            {
                outputTensor = worker.PeekOutput(name) as Tensor<float>;
                if (outputTensor != null)
                {
                    Debug.Log($"Found output tensor with name: '{name}'");
                    break;
                }
            }
            catch
            {
                // Ignore - try the next name
            }
        }

        // If none of the above work, try getting the first output by index
        if (outputTensor == null)
        {
            try
            {
                Debug.Log("Trying to get the first output by index (0)...");
                outputTensor = worker.PeekOutput(0) as Tensor<float>;
                if (outputTensor != null)
                {
                    Debug.Log("Found output tensor by index 0");
                }
            }
            catch
            {
                // Ignore
            }
        }

        if (outputTensor == null)
        {
            Debug.LogError("Failed to get output tensor!");
            return;
        }

        // Readback data from GPU to CPU
        using (Tensor<float> cpuCopy = outputTensor.ReadbackAndClone() as Tensor<float>)
        {
            if (cpuCopy == null)
            {
                Debug.LogError("Failed to readback tensor data!");
                return;
            }

            // Convert to zone map
            zoneMapTexture = TensorToZoneMap(cpuCopy, zoneColors);
        }

        // Display the zone map
        if (zoneMapDisplay != null)
            zoneMapDisplay.texture = zoneMapTexture;

        Debug.Log($"Semantic segmentation complete! Zone map: {zoneMapTexture.width}x{zoneMapTexture.height}");

        // Clean up
        outputTensor.Dispose();
    }

    private void LogModelInfo(Model model)
    {
        // Log all model inputs
        Debug.Log("=== Model Inputs ===");
        foreach (var input in model.inputs)
        {
            Debug.Log($"Input: {input.name} - Shape: {input.shape}");
        }

        // Log all model outputs
        Debug.Log("=== Model Outputs ===");
        foreach (var output in model.outputs)
        {
            // In Sentis 2.6.1, output.name is a string and output.shape is a TensorShape
            Debug.Log($"Output: {output.name} - Shape: {output.shape}");
        }

        // Log all model layers (first 10 only to avoid spam)
        Debug.Log("=== Model Layers (first 10) ===");
        int count = 0;
        foreach (var layer in model.layers)
        {
            if (count < 10)
            {
                // Layer doesn't have a Name property - use GetType().Name
                Debug.Log($"Layer {count}: {layer.GetType().Name}");
            }
            count++;
        }
        if (count > 10)
            Debug.Log($"... and {count - 10} more layers");
    }

    private Tensor<float> TextureToTensor(Texture2D texture, int targetWidth, int targetHeight)
    {
        // Force resize to exactly the target dimensions
        Texture2D resizedTexture = ResizeTexture(texture, targetWidth, targetHeight);
        
        // Verify the resize worked
        if (resizedTexture.width != targetWidth || resizedTexture.height != targetHeight)
        {
            Debug.LogError($"Resize failed! Expected {targetWidth}x{targetHeight}, got {resizedTexture.width}x{resizedTexture.height}");
        }

        Color[] pixels = resizedTexture.GetPixels();
        
        // For NCHW format: [Batch, Channels, Height, Width]
        float[] pixelData = new float[pixels.Length * 3];
        
        int numPixels = pixels.Length;
        
        // Separate channels for NCHW format
        for (int i = 0; i < numPixels; i++)
        {
            pixelData[i] = pixels[i].r;                 // R channel
            pixelData[i + numPixels] = pixels[i].g;      // G channel
            pixelData[i + numPixels * 2] = pixels[i].b;  // B channel
        }
        
        // NCHW format: [Batch, Channels, Height, Width]
        int[] shape = new int[] { 1, 3, targetHeight, targetWidth };
        Tensor<float> tensor = new Tensor<float>(new TensorShape(shape), pixelData);
        
        return tensor;
    }

    private Texture2D TensorToZoneMap(Tensor<float> tensor, List<Color> colors)
    {
        // Download data from tensor
        float[] data = tensor.DownloadToArray();

        // Output is in NCHW format: [Batch, Channels, Height, Width]
        int height = (int)tensor.shape[2];
        int width = (int)tensor.shape[3];
        int numClasses = (int)tensor.shape[1];

        Debug.Log($"Output tensor shape: {width}x{height}x{numClasses} classes");

        Texture2D zoneMap = new Texture2D(width, height, TextureFormat.ARGB32, false);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = y * width + x;
                int classIndex = 0;
                float maxScore = float.MinValue;

                // For NCHW format, class scores are at: [batch, class, y, x]
                for (int c = 0; c < numClasses; c++)
                {
                    int dataIndex = c * height * width + y * width + x;
                    float score = data[dataIndex];
                    if (score > maxScore)
                    {
                        maxScore = score;
                        classIndex = c;
                    }
                }

                pixels[pixelIndex] = classIndex < colors.Count ? colors[classIndex] : Color.white;
            }
        }

        zoneMap.SetPixels(pixels);
        zoneMap.Apply();
        return zoneMap;
    }

    private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        // Create a temporary RenderTexture
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        
        // Copy the source texture to the RenderTexture
        Graphics.Blit(source, rt);
        
        // Read back from the RenderTexture to a new Texture2D
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        
        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.ARGB32, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        
        return result;
    }

    private void InitializeZoneColors()
    {
        zoneColors = new List<Color>()
        {
            new Color(0, 0, 0, 1),       // 0: Background
            new Color(0, 0, 1, 1),       // 1: Aeroplane
            new Color(0, 1, 0, 1),       // 2: Bicycle
            new Color(0, 1, 1, 1),       // 3: Bird
            new Color(1, 0, 0, 1),       // 4: Boat
            new Color(1, 0, 1, 1),       // 5: Bottle
            new Color(1, 1, 0, 1),       // 6: Bus
            new Color(1, 1, 1, 1),       // 7: Car
            new Color(0.5f, 0, 0, 1),    // 8: Cat
            new Color(0.5f, 0, 0.5f, 1), // 9: Chair
            new Color(0.5f, 0.5f, 0, 1), // 10: Cow
            new Color(0, 0.5f, 0, 1),    // 11: Dining Table
            new Color(0, 0.5f, 0.5f, 1), // 12: Dog
            new Color(0, 0, 0.5f, 1),    // 13: Horse
            new Color(0.5f, 0.5f, 0.5f, 1), // 14: Motorbike
            new Color(0.75f, 0.25f, 0, 1),  // 15: Person
            new Color(0.25f, 0.75f, 0, 1),  // 16: Potted Plant
            new Color(0.75f, 0, 0.25f, 1),  // 17: Sheep
            new Color(0.25f, 0, 0.75f, 1),  // 18: Sofa
            new Color(0, 0.75f, 0.25f, 1),  // 19: Train
            new Color(0.75f, 0.25f, 0.75f, 1), // 20: TV
        };
    }

    public Texture2D GetZoneMap() => zoneMapTexture;

    void OnDestroy()
    {
        worker?.Dispose();
        inputTensor?.Dispose();
    }
}