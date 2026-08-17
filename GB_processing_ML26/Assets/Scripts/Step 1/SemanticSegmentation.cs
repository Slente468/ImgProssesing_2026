/*using UnityEngine;
using UnityEngine.UI;
  // Main Sentis namespace
using Unity.Sentis.Layers;  // For tensor operations
using System.Collections.Generic;

public class SemanticSegmentation : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your prime image (800x767) here")]
    public Texture2D primeImage;
    
    [Tooltip("Drag your DeepLabV3-MobileNet ONNX file here")]
    public Unity.InferenceEngine.ModelAsset modelAsset;
    
    [Tooltip("Drag a RawImage from your Canvas here to display the zone map")]
    public RawImage zoneMapDisplay;

    [Header("Model Settings")]
    public int inputWidth = 513;    // DeepLabV3-Plus-MobileNet uses 513x513
    public int inputHeight = 513;

    [Header("Zone Colors")]
    public List<Color> zoneColors = new List<Color>();

    // Sentis objects
    private IWorker worker;
    private TensorFloat inputTensor;
    private Texture2D zoneMapTexture;

    void Start()
    {
        if (modelAsset == null)
        {
            Debug.LogError("No model asset assigned! Drag your ONNX file to the Model Asset field.");
            return;
        }

        if (primeImage == null)
        {
            Debug.LogError("No prime image assigned! Drag your image to the Prime Image field.");
            return;
        }

        if (zoneMapDisplay == null)
        {
            Debug.LogWarning("No zone map display assigned! Drag a RawImage to the Zone Map Display field.");
        }

        // Initialize zone colors (you can customize these)
        if (zoneColors.Count == 0)
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

        RunSegmentation();
    }

    void RunSegmentation()
    {
        Debug.Log("Starting semantic segmentation...");

        // Step 1: Convert prime image to tensor
        inputTensor = TextureToTensor(primeImage, inputWidth, inputHeight);

        // Step 2: Load the model
        Unity.InferenceEngine.Model model = Unity.InferenceEngine.ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(Unity.InferenceEngine.BackendType.GPUCompute, model);

        // Step 3: Run inference
        worker.Execute(inputTensor);

        // Step 4: Get the output (segmentation map)
        // The output name is typically "output" or the last layer name
        TensorFloat outputTensor = worker.PeekOutput() as TensorFloat;
        if (outputTensor == null)
        {
            Debug.LogError("Failed to get output tensor! Check the model output name.");
            return;
        }

        // Step 5: Convert output to zone map texture
        zoneMapTexture = TensorToZoneMap(outputTensor, zoneColors);

        // Step 6: Display the zone map
        if (zoneMapDisplay != null)
        {
            zoneMapDisplay.texture = zoneMapTexture;
        }

        Debug.Log($"Semantic segmentation complete! Zone map: {zoneMapTexture.width}x{zoneMapTexture.height}");

        // Clean up
        outputTensor.Dispose();
    }

    /// <summary>
    /// Converts a Texture2D to a TensorFloat (resized to model input size)
    /// </summary>
    private TensorFloat TextureToTensor(Texture2D texture, int targetWidth, int targetHeight)
    {
        // Resize texture to model input size
        Texture2D resizedTexture = ResizeTexture(texture, targetWidth, targetHeight);

        // Get pixel data
        Color[] pixels = resizedTexture.GetPixels();
        float[] pixelData = new float[pixels.Length * 3];

        // Convert RGB to float array (normalized 0-1)
        for (int i = 0; i < pixels.Length; i++)
        {
            pixelData[i * 3 + 0] = pixels[i].r;
            pixelData[i * 3 + 1] = pixels[i].g;
            pixelData[i * 3 + 2] = pixels[i].b;
        }

        // Create tensor (NHWC format: Batch, Height, Width, Channels)
        int[] shape = new int[] { 1, targetHeight, targetWidth, 3 };
        TensorFloat tensor = new TensorFloat(shape, pixelData);

        return tensor;
    }

    /// <summary>
    /// Converts a segmentation output tensor to a color-coded zone map texture
    /// </summary>
    private Texture2D TensorToZoneMap(TensorFloat tensor, List<Color> colors)
    {
        // Get the tensor data
        float[] data = tensor.ToReadOnlyArray();

        // The output shape is typically [1, H, W, N] where N is number of classes
        // For DeepLabV3-Plus-MobileNet, the shape is [1, 513, 513, 21]
        int height = (int)tensor.shape[1];
        int width = (int)tensor.shape[2];
        int numClasses = (int)tensor.shape[3];

        Debug.Log($"Output tensor shape: {width}x{height}x{numClasses}");

        Texture2D zoneMap = new Texture2D(width, height, TextureFormat.ARGB32, false);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = y * width + x;
                int classIndex = 0;
                float maxScore = float.MinValue;

                // Find the class with the highest probability for this pixel
                for (int c = 0; c < numClasses; c++)
                {
                    float score = data[pixelIndex * numClasses + c];
                    if (score > maxScore)
                    {
                        maxScore = score;
                        classIndex = c;
                    }
                }

                // Assign color based on class index
                if (classIndex < colors.Count)
                    pixels[pixelIndex] = colors[classIndex];
                else
                    pixels[pixelIndex] = Color.white; // Default color for unknown classes
            }
        }

        zoneMap.SetPixels(pixels);
        zoneMap.Apply();

        return zoneMap;
    }

    /// <summary>
    /// Resizes a texture to the target dimensions
    /// </summary>
    private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(targetWidth, targetHeight);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    /// <summary>
    /// Public method to get the zone map texture
    /// </summary>
    public Texture2D GetZoneMap()
    {
        return zoneMapTexture;
    }

    /// <summary>
    /// Public method to get a specific pixel's zone color from the zone map
    /// </summary>
    public Color GetZoneAtPixel(int x, int y)
    {
        if (zoneMapTexture == null) return Color.white;
        return zoneMapTexture.GetPixel(x, y);
    }

    void OnDestroy()
    {
        if (worker != null)
            worker.Dispose();
        if (inputTensor != null)
            inputTensor.Dispose();
    }
}*/