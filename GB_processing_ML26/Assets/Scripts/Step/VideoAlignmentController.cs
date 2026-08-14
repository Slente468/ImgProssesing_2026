/*using UnityEngine;
using System.Collections;

public class VideoAlignmentController : MonoBehaviour
{
    [Header("References")]
    public VideoFrameExtractor frameExtractor;
    public ORBAlignment orbAlignment;

    [Header("Settings")]
    public bool autoProcess = true;

    private bool isProcessing = false;

    void Start()
    {
        Debug.Log("VideoAlignmentController Start() called!");
        if (autoProcess)
            StartProcessing();
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

        Debug.Log("Both references are assigned. Starting...");

        if (isProcessing)
            return;

        isProcessing = true;
        StartCoroutine(ProcessVideo());
    }

    IEnumerator ProcessVideo()
    {
        Debug.Log("ProcessVideo() started.");

        // Start extracting (this prepares the video)
        frameExtractor.StartExtracting();
        Debug.Log("StartExtracting() called. Waiting for first frame...");

        // Get the first frame using the new coroutine
        Texture2D firstFrame = null;
        yield return StartCoroutine(frameExtractor.GetFirstFrameCoroutine((frame) => {
            firstFrame = frame;
        }));

        if (firstFrame == null)
        {
            Debug.LogError("Failed to get first frame!");
            isProcessing = false;
            yield break;
        }

        Debug.Log("First frame acquired: " + firstFrame.width + "x" + firstFrame.height);

        // Check the prime image
        if (orbAlignment.primeImage == null)
        {
            Debug.LogError("Prime image is not assigned in ORBAlignment!");
            isProcessing = false;
            yield break;
        }

        Debug.Log("Prime image: " + orbAlignment.primeImage.width + "x" + orbAlignment.primeImage.height);

        // Assign the frame to ORBAlignment
        Debug.Log("Calling SetVideoFrame() with first frame...");
        orbAlignment.SetVideoFrame(firstFrame);

        // Wait a moment for alignment to complete
        yield return new WaitForEndOfFrame();

        Debug.Log("Video processing complete (Step 1 & 2 done). Ready for Step 3.");
        isProcessing = false;
    }
}*/