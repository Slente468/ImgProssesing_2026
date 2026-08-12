using UnityEngine;
using OpenCVForUnity.CoreModule;

/// <summary>
/// Manual conversion between Unity Texture2D and OpenCV Mat.
/// No dependency on Utils, Convert, or any version-specific helpers.
/// </summary>
public static class Texture2DMatConverter
{
    /// <summary>
    /// Converts a Unity Texture2D to an OpenCV Mat (8UC3 - 8-bit, 3 channels, BGR format).
    /// </summary>
    public static Mat Texture2DToMat(Texture2D tex)
{
    if (tex == null)
    {
        Debug.LogError("Texture2D is null!");
        return null;
    }

    // Check if texture is readable
    try
    {
        // Try to get pixels to see if it's readable
        Color32[] test = tex.GetPixels32(0);
    }
    catch (UnityException e)
    {
        Debug.LogError("Texture is not readable: " + tex.name + ". Please enable 'Read/Write Enabled' in the texture import settings. Error: " + e.Message);
        return null;
    }

    // Get pixel data
    Color32[] pixels = tex.GetPixels32();
    int width = tex.width;
    int height = tex.height;

    // Create Mat with 8UC3 (8-bit, 3 channels - BGR)
    Mat mat = new Mat(height, width, CvType.CV_8UC3);

    // Convert Unity Color32 (RGBA) to OpenCV BGR
    byte[] data = new byte[pixels.Length * 3];
    for (int i = 0; i < pixels.Length; i++)
    {
        data[i * 3 + 0] = pixels[i].b; // B
        data[i * 3 + 1] = pixels[i].g; // G
        data[i * 3 + 2] = pixels[i].r; // R
    }

    mat.put(0, 0, data);
    return mat;
}
    /// <summary>
    /// Converts an OpenCV Mat (8UC3 - BGR format) to a Unity Texture2D.
    /// </summary>
    public static Texture2D MatToTexture2D(Mat mat)
    {
        if (mat == null || mat.empty())
        {
            Debug.LogError("Mat is null or empty!");
            return null;
        }

        int width = mat.width();
        int height = mat.height();

        // Read pixel data from Mat (BGR format)
        byte[] data = new byte[width * height * 3];
        mat.get(0, 0, data);

        // Convert OpenCV BGR to Unity Color32 (RGBA)
        Color32[] pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(
                data[i * 3 + 2], // R
                data[i * 3 + 1], // G
                data[i * 3 + 0], // B
                255               // A (fully opaque)
            );
        }

        Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }
}