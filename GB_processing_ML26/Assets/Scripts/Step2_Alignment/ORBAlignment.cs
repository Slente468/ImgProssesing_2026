using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.Calib3dModule;
using System.Collections.Generic;

public class ORBAlignment : MonoBehaviour
{
    public Texture2D primeImage;
    public Texture2D videoFrame;
    public RawImage displayImage;

    void Start()
    {
        if (primeImage == null)
            Debug.LogWarning("Prime image not assigned in ORBAlignment!");
    }

    // ============================================================
    // ORIGINAL METHOD: Aligns and displays the result
    // ============================================================
    public void AlignImages()
    {
        if (primeImage == null || videoFrame == null)
        {
            Debug.LogError("Prime image or video frame is null!");
            return;
        }

        Texture2D result = PerformAlignment(videoFrame);
        
        if (result != null && displayImage != null)
        {
            displayImage.texture = result;
            Debug.Log("Alignment complete! Display updated.");
        }
    }

    // ============================================================
    // PUBLIC: Aligns and returns the result (preserves display)
    // ============================================================
    public Texture2D AlignAndReturnTexture(Texture2D inputFrame, bool preserveDisplay = false)
    {
        if (inputFrame == null)
        {
            Debug.LogError("Input frame is null!");
            return null;
        }

        // Save current display texture
        Texture2D savedDisplay = null;
        if (displayImage != null && displayImage.texture != null)
        {
            // Try to safely get the current texture
            if (displayImage.texture is Texture2D)
                savedDisplay = (Texture2D)displayImage.texture;
        }

        // Run alignment
        Texture2D result = PerformAlignment(inputFrame);

        // Restore display if requested
        if (preserveDisplay && displayImage != null)
        {
            displayImage.texture = savedDisplay;
        }
        else if (!preserveDisplay && displayImage != null && result != null)
        {
            // If not preserving, update the display with the result
            displayImage.texture = result;
        }

        return result;
    }

    // ============================================================
    // CORE ALIGNMENT: Returns a Texture2D directly
    // ============================================================
    private Texture2D PerformAlignment(Texture2D inputFrame)
{
    if (inputFrame == null || primeImage == null)
    {
        Debug.LogError("Prime image or input frame is null!");
        return null;
    }

    Debug.Log("Starting alignment...");

    // Convert using our own converter (bc unity were difficult ånd so wås the OPenCV thing (skill issue))
    Mat primeMat = Texture2DMatConverter.Texture2DToMat(primeImage);
    Mat frameMat = Texture2DMatConverter.Texture2DToMat(inputFrame);

    if (primeMat == null || frameMat == null)
    {
        Debug.LogError("Failed to convert textures to Mats!");
        return null;
    }

    // --- FIX: Force both frames to be the same size BEFORE feature detection ---
    // Resize frame to match prime image dimensions if they differ
    if (frameMat.width() != primeMat.width() || frameMat.height() != primeMat.height())
    {
        Debug.Log($"Resizing frame from {frameMat.width()}x{frameMat.height()} to {primeMat.width()}x{primeMat.height()}");
        Mat resizedFrame = new Mat();
        Imgproc.resize(frameMat, resizedFrame, primeMat.size());
        frameMat = resizedFrame;
    }

    // Convert to grayscale
    Mat primeGray = new Mat();
    Mat frameGray = new Mat();
    Imgproc.cvtColor(primeMat, primeGray, Imgproc.COLOR_BGR2GRAY);
    Imgproc.cvtColor(frameMat, frameGray, Imgproc.COLOR_BGR2GRAY);

    // --- OPTIONAL? MORE LIKE NEED: Add CLAHE for contrast enhancement ---
    CLAHE clahe = Imgproc.createCLAHE(2.0, new Size(8, 8));
    Mat primeEnhanced = new Mat();
    Mat frameEnhanced = new Mat();
    clahe.apply(primeGray, primeEnhanced);
    clahe.apply(frameGray, frameEnhanced);
    // Use enhanced versions for feature detection
    primeGray = primeEnhanced;
    frameGray = frameEnhanced;

    // AKAZE feature detector (used ORB before)
    AKAZE akaze = AKAZE.create();
    MatOfKeyPoint keypointsPrime = new MatOfKeyPoint();
    MatOfKeyPoint keypointsFrame = new MatOfKeyPoint();
    Mat descriptorsPrime = new Mat();
    Mat descriptorsFrame = new Mat();

    akaze.detectAndCompute(primeGray, new Mat(), keypointsPrime, descriptorsPrime);
    akaze.detectAndCompute(frameGray, new Mat(), keypointsFrame, descriptorsFrame);

    // Check if enough keypoints found
    int primeKeypoints = keypointsPrime.toArray().Length;
    int frameKeypoints = keypointsFrame.toArray().Length;
    
    if (primeKeypoints < 4 || frameKeypoints < 4)
    {
        Debug.LogError($"Not enough keypoints. Prime: {primeKeypoints}, Frame: {frameKeypoints}");
        return null;
    }

    // Match features using BFMatcher with L1 norm (for AKAZE)
    BFMatcher matcher = new BFMatcher(4); // 4 = L1 norm
    MatOfDMatch matches = new MatOfDMatch();
    matcher.match(descriptorsPrime, descriptorsFrame, matches);

    // Find Homography
    List<DMatch> matchList = matches.toList();

    if (matchList.Count < 4)
    {
        Debug.LogError($"Not enough matches! Found: {matchList.Count}");
        return null;
    }

    // Sort and take top matches
    matchList.Sort((a, b) => a.distance.CompareTo(b.distance));
    int numGoodMatches = Mathf.Min(100, matchList.Count); // Increased from 50 to 100

    List<Point> primePoints = new List<Point>();
    List<Point> framePoints = new List<Point>();

    for (int i = 0; i < numGoodMatches; i++)
    {
        primePoints.Add(keypointsPrime.toList()[matchList[i].queryIdx].pt);
        framePoints.Add(keypointsFrame.toList()[matchList[i].trainIdx].pt);
    }

    MatOfPoint2f primePointsMat = new MatOfPoint2f();
    MatOfPoint2f framePointsMat = new MatOfPoint2f();
    primePointsMat.fromList(primePoints);
    framePointsMat.fromList(framePoints);

    // --- FIX: Correct Homography order (frame → prime) (this one is påin)---
    Mat homography = Calib3d.findHomography(framePointsMat, primePointsMat, Calib3d.RANSAC, 3.0);

    if (homography.empty())
    {
        Debug.LogError("Homography calculation failed!");
        return null;
    }

    // --- FIX: Ensure warp output is exactly prime size ---
    Mat warpedFrame = new Mat();
    Size primeSize = primeMat.size(); // 800x767
    Imgproc.warpPerspective(frameMat, warpedFrame, homography, primeSize);

    // --- DEBUG: Log the size of the warped frame ---
    Debug.Log($"Warped frame size: {warpedFrame.width()}x{warpedFrame.height()}");

    // Convert back to Texture2D
    Texture2D resultTexture = Texture2DMatConverter.MatToTexture2D(warpedFrame);

    if (resultTexture == null)
    {
        Debug.LogError("Failed to convert warped Mat to Texture2D!");
        return null;
    }

    Debug.Log($"Alignment complete! Result: {resultTexture.width}x{resultTexture.height}");
    return resultTexture;
}
    public void SetVideoFrame(Texture2D frame)
    {
        videoFrame = frame;
        AlignImages();
    }
    
}
