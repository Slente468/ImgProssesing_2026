using UnityEngine;
using UnityEngine.UI;

public class TwoFrameAlignment : MonoBehaviour
{
    [Header("References")]
    public ORBAlignment orbAlignment;
    public Texture2D primeImage;
    public Texture2D firstFrame;
    public Texture2D lastFrame;

    [Header("Display")]
    public RawImage resultImage;

    [Header("Settings")]
    public float subtractionThreshold = 0.1f;

    // ============================================================
    // ADD THIS: Auto-start when the scene loads
    // ============================================================
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

        // Step 1: Align FIRST frame independently
        Debug.Log("Aligning first frame...");
        Texture2D warpedFirst = orbAlignment.AlignAndReturnTexture(firstFrame, true);
        if (warpedFirst == null)
        {
            Debug.LogError("Failed to align first frame!");
            return;
        }

        // Step 2: Align LAST frame independently
        Debug.Log("Aligning last frame...");
        Texture2D warpedLast = orbAlignment.AlignAndReturnTexture(lastFrame, true);
        if (warpedLast == null)
        {
            Debug.LogError("Failed to align last frame!");
            return;
        }

        // Step 3: Both frames are now 800x767 and aligned. Subtract them.
        Debug.Log("Subtracting images...");
        Texture2D drawingMask = SubtractImages(warpedFirst, warpedLast);

        // Step 4: Display the result
        if (resultImage != null)
        {
            resultImage.texture = drawingMask;
        }

        Debug.Log("Two-Frame Alignment complete! Drawing mask ready.");
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

    // Optional: Manual start method if you want to call it from elsewhere
    public void StartProcessing()
    {
        ProcessFrames();
    }
}