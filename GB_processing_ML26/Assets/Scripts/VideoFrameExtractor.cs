using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class VideoFrameExtractor : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoClip videoClip;
    public bool autoPlay = true;

    [Header("Frame Output")]
    public Texture2D currentFrame;
    public int targetWidth = 800;
    public int targetHeight = 767;
    public RawImage previewImage;

    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private bool isFrameReady = false;

    void Start()
    {
        if (autoPlay)
            StartExtracting();
    }

    public void StartExtracting()
    {
        if (videoClip == null)
        {
            Debug.LogError("No VideoClip assigned to VideoFrameExtractor!");
            return;
        }

        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.clip = videoClip;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;

        // Create RenderTexture
        renderTexture = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        renderTexture.useMipMap = false;
        renderTexture.Create();

        videoPlayer.targetTexture = renderTexture;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        videoPlayer.Prepare();
        
        // Don't rely on prepareCompleted—just start
        videoPlayer.Play();
        Debug.Log("VideoPlayer started. Waiting for first frame...");
    }

    /// <summary>
    /// Manually extract the first frame by polling.
    /// </summary>
    public IEnumerator GetFirstFrameCoroutine(System.Action<Texture2D> callback)
    {
        Debug.Log("GetFirstFrameCoroutine started. Polling for first frame...");

        int waitCount = 0;
        int maxWait = 300; // ~10 seconds at 30fps

        while (waitCount < maxWait)
        {
            waitCount++;

            // Check if the video is playing and has rendered a frame
            if (videoPlayer.isPlaying && videoPlayer.texture != null)
            {
                // Give it one more frame to render properly
                yield return new WaitForEndOfFrame();
                
                // Capture the frame
                RenderTexture.active = renderTexture;

                if (currentFrame == null || currentFrame.width != targetWidth || currentFrame.height != targetHeight)
                {
                    currentFrame = new Texture2D(targetWidth, targetHeight, TextureFormat.ARGB32, false);
                }

                currentFrame.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
                currentFrame.Apply();

                RenderTexture.active = null;

                // Show preview
                if (previewImage != null)
                {
                    previewImage.texture = currentFrame;
                }

                // Pause the video after capturing the first frame
                videoPlayer.Pause();
                Debug.Log("First frame captured: " + currentFrame.width + "x" + currentFrame.height);

                callback?.Invoke(currentFrame);
                yield break;
            }

            // Debug every 50 frames
            if (waitCount % 50 == 0)
                Debug.Log("Waiting for first frame... (" + waitCount + "/" + maxWait + ")");

            yield return new WaitForEndOfFrame();
        }

        // If we get here, we timed out
        Debug.LogError("Timed out waiting for first frame after " + maxWait + " frames!");

        // Fallback: try to capture whatever is in the RenderTexture
        if (renderTexture != null)
        {
            Debug.Log("Attempting fallback capture...");
            RenderTexture.active = renderTexture;

            if (currentFrame == null || currentFrame.width != targetWidth || currentFrame.height != targetHeight)
            {
                currentFrame = new Texture2D(targetWidth, targetHeight, TextureFormat.ARGB32, false);
            }

            currentFrame.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            currentFrame.Apply();

            RenderTexture.active = null;

            if (previewImage != null)
            {
                previewImage.texture = currentFrame;
            }

            Debug.Log("Fallback capture complete.");
            callback?.Invoke(currentFrame);
        }
        else
        {
            callback?.Invoke(null);
        }
    }

    public Texture2D GetCurrentFrame()
    {
        return currentFrame;
    }

    public long GetTotalFrameCount()
    {
        if (videoPlayer != null)
            return (long)videoPlayer.frameCount;
        else
            return 0;
    }

    public void Play()
    {
        if (videoPlayer != null)
            videoPlayer.Play();
    }

    public void Pause()
    {
        if (videoPlayer != null)
            videoPlayer.Pause();
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
        if (renderTexture != null)
            renderTexture.Release();
        if (currentFrame != null)
            Destroy(currentFrame);
    }
}