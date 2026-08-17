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
    public RawImage resultImage;        // Final mask
    public RawImage debugFirstImage;    // NEW: Show warped first frame
    public RawImage debugLastImage;     // NEW: Show warped last frame

    [Header("Settings")]
    public float subtractionThreshold = 0.1f;

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
        Texture2D drawingMask = SubtractImages(warpedFirst, warpedLast);

        if (resultImage != null)
            resultImage.texture = drawingMask;

        Debug.Log("Two-Frame Alignment complete!");
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
}