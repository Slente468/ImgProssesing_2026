using UnityEngine;
using System.Collections;

public class TestAlignmentWithVideo : MonoBehaviour
{
    public VideoFrameExtractor frameExtractor;
    public ORBAlignment orbAlignment;
    
    void Start()
    {
        // Wait for the video to prepare and extract the first frame
        StartCoroutine(WaitForFirstFrame());
    }
    
    IEnumerator WaitForFirstFrame()
    {
        // Start extracting
        frameExtractor.StartExtracting();
        
        // Wait until the first frame is ready
        while (frameExtractor.GetCurrentFrame() == null)
        {
            yield return new WaitForEndOfFrame();
        }
        
        Debug.Log("First frame extracted: " + frameExtractor.GetCurrentFrame().width + "x" + frameExtractor.GetCurrentFrame().height);
        
        // Now feed it to ORBAlignment
        if (orbAlignment != null)
        {
            // We need to modify ORBAlignment to accept a Texture2D at runtime
            // For now, just assign it manually
            orbAlignment.videoFrame = frameExtractor.GetCurrentFrame();
            
            // Trigger alignment
            orbAlignment.AlignImages();
        }
        else
        {
            Debug.LogWarning("ORBAlignment script not assigned.");
        }
    }
}