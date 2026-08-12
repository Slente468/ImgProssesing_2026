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
    private System.Action<Texture2D> onFrameReadyCallback;

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

        // Create RenderTexture - SET PROPERTIES FIRST
        renderTexture = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
        renderTexture.useMipMap = false; // Set BEFORE Create()
        renderTexture.Create(); // Now create it
        
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        videoPlayer.Prepare();
        videoPlayer.prepareCompleted += OnVideoPrepared;
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("Video prepared. Frame count: " + vp.frameCount);
        videoPlayer.Play();
        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.frameReady += OnFrameReady;
    }

    void OnFrameReady(VideoPlayer vp, long frameIdx)
    {
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

        // Pause after first frame to see preview
        if (frameIdx == 0)
        {
            vp.Pause();
            Debug.Log("Video paused after first frame. Click Play in the VideoPlayer Inspector to continue.");
        }

        if (onFrameReadyCallback != null)
        {
            onFrameReadyCallback.Invoke(currentFrame);
        }
    }

    public Texture2D GetCurrentFrame()
    {
        return currentFrame;
    }

    public void GetNextFrame(System.Action<Texture2D> callback)
    {
        if (videoPlayer == null || !videoPlayer.isPlaying)
        {
            Debug.LogWarning("VideoPlayer is not playing.");
            return;
        }
        onFrameReadyCallback = callback;
    }

    public long GetTotalFrameCount()
    {
        if (videoPlayer != null)
            return (long)videoPlayer.frameCount;
        else
            return 0;
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.frameReady -= OnFrameReady;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
        if (renderTexture != null)
            renderTexture.Release();
        if (currentFrame != null)
            Destroy(currentFrame);
    }
}