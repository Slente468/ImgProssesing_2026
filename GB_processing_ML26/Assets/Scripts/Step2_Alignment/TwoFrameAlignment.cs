using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class TwoFrameAlignment : MonoBehaviour
{
    [Header("References")]
    public ORBAlignment orbAlignment;
    public Texture2D primeImage;
    public Texture2D firstFrame;
    public Texture2D lastFrame;

    [Header("Display")]
    public RawImage resultImage;        // Final mask
    public RawImage debugFirstImage;    // Show warped first frame
    public RawImage debugLastImage;     // Show warped last frame

    [Header("Settings")]
    public float subtractionThreshold = 0.1f;

    [Header("Drawing Mask Output")]
    [Tooltip("The extracted drawing mask (black background, white lines). Drag this into DrawingCoordinateExtractor.")]
    public Texture2D drawingMask;

    [Header("PNG Export Settings")]
    [Tooltip("Enable to automatically save the drawing mask as a PNG.")]
    public bool autoSavePNG = false;

    [Tooltip("Filename for the exported PNG (without extension).")]
    public string pngFileName = "drawing_mask";

    [Tooltip("Export folder path (relative to Assets folder).")]
    public string pngExportFolder = "Data/Masks/";

    void Start()
    {
        Debug.Log("TwoFrameAlignment Start() called!");
        ProcessFrames();
    }

    public void ProcessFrames()
    {
        if (firstFrame == null || lastFrame == null || primeImage == null)
        {
            Debug.LogError("Missing textures!");
            return;
        }

        if (orbAlignment == null)
        {
            Debug.LogError("ORBAlignment is not assigned!");
            return;
        }

        Debug.Log("Starting Two-Frame Alignment...");

        // Align FIRST frame
        Debug.Log("Aligning first frame...");
        Texture2D warpedFirst = orbAlignment.AlignAndReturnTexture(firstFrame, true);
        if (warpedFirst == null)
        {
            Debug.LogError("Failed to align first frame!");
            return;
        }

        // Show warped first frame for debugging
        if (debugFirstImage != null)
            debugFirstImage.texture = warpedFirst;

        // Align LAST frame
        Debug.Log("Aligning last frame...");
        Texture2D warpedLast = orbAlignment.AlignAndReturnTexture(lastFrame, true);
        if (warpedLast == null)
        {
            Debug.LogError("Failed to align last frame!");
            return;
        }

        // Show warped last frame for debugging
        if (debugLastImage != null)
            debugLastImage.texture = warpedLast;

        // Subtract
        Debug.Log("Subtracting images...");
        drawingMask = SubtractImages(warpedFirst, warpedLast);

        if (resultImage != null)
            resultImage.texture = drawingMask;

        Debug.Log("Two-Frame Alignment complete! Drawing mask is ready for coordinate extraction.");

        // Auto-save PNG if enabled
        if (autoSavePNG && drawingMask != null)
        {
            SaveMaskAsPNG(drawingMask, pngFileName);
        }
    }

    private Texture2D SubtractImages(Texture2D clean, Texture2D drawn)
    {
        int width = clean.width;
        int height = clean.height;
        Texture2D mask = new Texture2D(width, height, TextureFormat.ARGB32, false);

        Color[] cleanPixels = clean.GetPixels();
        Color[] drawnPixels = drawn.GetPixels();
        Color[] maskPixels = new Color[cleanPixels.Length];

        for (int i = 0; i < cleanPixels.Length; i++)
        {
            float diff = Vector3.Distance(
                new Vector3(cleanPixels[i].r, cleanPixels[i].g, cleanPixels[i].b),
                new Vector3(drawnPixels[i].r, drawnPixels[i].g, drawnPixels[i].b)
            );

            maskPixels[i] = diff > subtractionThreshold ? Color.white : Color.black;
        }

        mask.SetPixels(maskPixels);
        mask.Apply();
        return mask;
    }

    /// <summary>
    /// Saves the drawing mask as a PNG file with the given filename.
    /// </summary>
    public void SaveMaskAsPNG(Texture2D mask, string filename)
    {
        if (mask == null)
        {
            Debug.LogError("Cannot save null mask!");
            return;
        }

        // Create the full folder path
        string fullPath = Path.Combine(Application.dataPath, pngExportFolder);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        // Ensure filename has no invalid characters
        string safeFilename = string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        string filePath = Path.Combine(fullPath, safeFilename + ".png");

        // Encode and save
        byte[] bytes = mask.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);

        Debug.Log($"Drawing mask saved to: {filePath}");
        
        // Refresh Unity's asset database so the file appears in the Project window
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
    }

    /// <summary>
    /// Manually save the current drawing mask with a custom filename.
    /// Call this from the Inspector button or from another script.
    /// </summary>
    public void SaveCurrentMaskAsPNG(string customFilename = "")
    {
        if (drawingMask == null)
        {
            Debug.LogError("No drawing mask to save! Run ProcessFrames first.");
            return;
        }

        string filename = string.IsNullOrEmpty(customFilename) ? pngFileName : customFilename;
        SaveMaskAsPNG(drawingMask, filename);
    }

    // ============================================================
    // Optional: Editor Button for Manual Save
    // ============================================================
    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(TwoFrameAlignment))]
    public class TwoFrameAlignmentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TwoFrameAlignment script = (TwoFrameAlignment)target;

            UnityEditor.EditorGUILayout.Space(10);
            UnityEditor.EditorGUILayout.LabelField("PNG Export", UnityEditor.EditorStyles.boldLabel);

            // Show a preview of the save path
            string previewPath = Path.Combine(Application.dataPath, script.pngExportFolder, script.pngFileName + ".png");
            UnityEditor.EditorGUILayout.LabelField("Save Path:", previewPath);

            // Custom filename field
            string customName = UnityEditor.EditorGUILayout.TextField("Custom Filename", script.pngFileName);
            if (customName != script.pngFileName)
            {
                script.pngFileName = customName;
            }

            // Save button
            if (GUILayout.Button("Save Drawing Mask as PNG"))
            {
                script.SaveCurrentMaskAsPNG(script.pngFileName);
            }
        }
    }
    #endif
}