using UnityEngine;
using UnityEngine.UI;
using Unity.InferenceEngine;
using System.Collections.Generic;
using System.IO;

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
    public int inputWidth = 520;
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

        // 1. Load the model
        Model model = ModelLoader.Load(modelAsset);
        LogModelInfo(model);

        // 2. Create the worker
        worker = new Worker(model, BackendType.GPUCompute);

        // 3. Convert texture to tensor
        inputTensor = TextureToTensor(primeImage, inputWidth, inputHeight);
        Debug.Log($"Created tensor with shape: {inputTensor.shape}");

        // 4. Schedule inference (non-blocking)
        worker.Schedule(inputTensor);

        // 5. Get the output tensor by index 0 - it's an integer tensor
        Tensor<int> outputTensor = null;

        try
        {
            outputTensor = worker.PeekOutput(0) as Tensor<int>;
            if (outputTensor != null)
            {
                Debug.Log("Found output tensor by index 0 as Int");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Trying index 0 failed: {e.Message}");
        }

        if (outputTensor == null)
        {
            Debug.LogError("Failed to get output tensor!");
            return;
        }

        // 6. Download the data to a CPU array (blocks until inference is complete)
        int[] data = outputTensor.DownloadToArray();

        // 7. Convert the data to a zone map
        zoneMapTexture = TensorToZoneMapFromIntArray(data, outputTensor.shape, zoneColors);

        // Display the zone map
        if (zoneMapDisplay != null)
            zoneMapDisplay.texture = zoneMapTexture;

        Debug.Log($"Semantic segmentation complete! Zone map: {zoneMapTexture.width}x{zoneMapTexture.height}");
    }

    private void LogModelInfo(Model model)
    {
        Debug.Log("=== Model Inputs ===");
        foreach (var input in model.inputs)
            Debug.Log($"Input: {input.name}");

        Debug.Log("=== Model Outputs ===");
        foreach (var output in model.outputs)
            Debug.Log($"Output: {output.name} (Index: {output.index})");

        Debug.Log("=== Model Layers (first 10) ===");
        int count = 0;
        foreach (var layer in model.layers)
        {
            if (count < 10)
                Debug.Log($"Layer {count}: {layer.GetType().Name}");
            count++;
        }
        if (count > 10)
            Debug.Log($"... and {count - 10} more layers");
    }

    private void ListAvailableOutputs()
    {
        Debug.Log("=== Available Outputs ===");
        int index = 0;
        while (true)
        {
            try
            {
                var output = worker.PeekOutput(index);
                if (output == null)
                    break;
                Debug.Log($"Output Index {index}: {output}");
                index++;
            }
            catch
            {
                break;
            }
        }
    }

    private Tensor<float> TextureToTensor(Texture2D texture, int targetWidth, int targetHeight)
    {
        Texture2D resizedTexture = ResizeTexture(texture, targetWidth, targetHeight);
        Color[] pixels = resizedTexture.GetPixels();
        float[] pixelData = new float[pixels.Length * 3];
        int numPixels = pixels.Length;

        for (int i = 0; i < numPixels; i++)
        {
            pixelData[i] = pixels[i].r;
            pixelData[i + numPixels] = pixels[i].g;
            pixelData[i + numPixels * 2] = pixels[i].b;
        }

        int[] shape = new int[] { 1, 3, targetHeight, targetWidth };
        return new Tensor<float>(new TensorShape(shape), pixelData);
    }

    private Texture2D TensorToZoneMapFromIntArray(int[] data, TensorShape shape, List<Color> colors)
    {
        int height = (int)shape[1];
        int width = (int)shape[2];

        Debug.Log($"Output tensor shape: {width}x{height} (integer class indices)");

        Texture2D zoneMap = new Texture2D(width, height, TextureFormat.ARGB32, false);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = y * width + x;
                int classIndex = data[pixelIndex];

                if (classIndex < 0) classIndex = 0;
                if (classIndex >= colors.Count) classIndex = colors.Count - 1;

                pixels[pixelIndex] = colors[classIndex];
            }
        }

        zoneMap.SetPixels(pixels);
        zoneMap.Apply();
        return zoneMap;
    }

    private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);

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
            new Color(0, 0, 0, 1), new Color(0, 0, 1, 1), new Color(0, 1, 0, 1),
            new Color(0, 1, 1, 1), new Color(1, 0, 0, 1), new Color(1, 0, 1, 1),
            new Color(1, 1, 0, 1), new Color(1, 1, 1, 1), new Color(0.5f, 0, 0, 1),
            new Color(0.5f, 0, 0.5f, 1), new Color(0.5f, 0.5f, 0, 1), new Color(0, 0.5f, 0, 1),
            new Color(0, 0.5f, 0.5f, 1), new Color(0, 0, 0.5f, 1), new Color(0.5f, 0.5f, 0.5f, 1),
            new Color(0.75f, 0.25f, 0, 1), new Color(0.25f, 0.75f, 0, 1), new Color(0.75f, 0, 0.25f, 1),
            new Color(0.25f, 0, 0.75f, 1), new Color(0, 0.75f, 0.25f, 1), new Color(0.75f, 0.25f, 0.75f, 1)
        };
    }

    public Texture2D GetZoneMap() => zoneMapTexture;

    // ============================================================
    // SAVE ZONE MAP AS PNG - SINGLE CLEAN VERSION
    // ============================================================
    public void SaveZoneMapAsPNG(string customFilename = "")
    {
        if (zoneMapTexture == null)
        {
            Debug.LogError("❌ No zone map to save! Run segmentation first.");
            return;
        }

        string folderPath = Path.Combine(Application.dataPath, "Data/Segmentation/");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filename = string.IsNullOrEmpty(customFilename) ? "zone_map" : customFilename;
        // Make sure filename is safe
        string safeFilename = string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        string filePath = Path.Combine(folderPath, safeFilename + ".png");

        byte[] bytes = zoneMapTexture.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);
        Debug.Log($"✅ Semantic Zone Map saved to: {filePath}");

        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
    }

    void OnDestroy()
    {
        worker?.Dispose();
        inputTensor?.Dispose();
    }
}