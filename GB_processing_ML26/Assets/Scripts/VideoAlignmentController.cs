using UnityEngine;
using System.Collections;

public class VideoAlignmentController : MonoBehaviour
{
    [Header("References")]
    public VideoFrameExtractor frameExtractor;
    public ORBAlignment orbAlignment;
    
    [Header("Settings")]
    public bool autoProcess = true;
    public float frameDelay = 0.1f;

    private bool isProcessing = false;

    void Start()
    {
        if (autoProcess)
            StartProcessing();
            Debug.Log("VideoAlignmentController Start() called!");
    
    }

   public void StartProcessing()
{
    Debug.Log("StartProcessing() called!");
    
    if (frameExtractor == null)
    {
        Debug.LogError("Frame Extractor is null! Did you assign it in the Inspector?");
        return;
    }
    
    if (orbAlignment == null)
    {
        Debug.LogError("ORB Alignment is null! Did you assign it in the Inspector?");
        return;
    }
    
    Debug.Log("Both references are assigned. Starting coroutine...");
    
    if (isProcessing)
        return;

    isProcessing = true;
    StartCoroutine(ProcessVideo());
}

    IEnumerator ProcessVideo()
    {
        Debug.Log("Starting video processing...");

        // Step 1: Extract the first frame
        frameExtractor.StartExtracting();
        
        // Wait for the first frame to be ready
        int waitCount = 0;
        while (frameExtractor.GetCurrentFrame() == null)
        {
            waitCount++;
            if (waitCount > 100) // Timeout after ~3 seconds
            {
                Debug.LogError("Timed out waiting for first frame!");
                isProcessing = false;
                yield break;
            }
            yield return new WaitForEndOfFrame();
        }

        Texture2D firstFrame = frameExtractor.GetCurrentFrame();
        Debug.Log("First frame extracted: " + firstFrame.width + "x" + firstFrame.height);

        // Step 2: Check if the first frame has content
        if (firstFrame == null)
        {
            Debug.LogError("First frame is null!");
            isProcessing = false;
            yield break;
        }

        // Step 3: Log the prime image
        if (orbAlignment.primeImage == null)
        {
            Debug.LogError("Prime image is not assigned in ORBAlignment!");
            isProcessing = false;
            yield break;
        }
        Debug.Log("Prime image: " + orbAlignment.primeImage.width + "x" + orbAlignment.primeImage.height);

        // Step 4: Align the first frame
        Debug.Log("Calling SetVideoFrame...");
        orbAlignment.SetVideoFrame(firstFrame);
        
        // Wait a moment for alignment to complete
        yield return new WaitForEndOfFrame();

        Debug.Log("Video processing complete.");
        isProcessing = false;
    }
}

