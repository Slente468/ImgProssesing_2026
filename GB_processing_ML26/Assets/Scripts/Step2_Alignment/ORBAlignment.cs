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
    // Don't auto-align on Start—wait for SetVideoFrame() to be called
    // Just check that primeImage is assigned
    if (primeImage == null)
        Debug.LogWarning("Prime image not assigned in ORBAlignment!");

    // Remove the auto-align call to prevent the warning
    // if (primeImage != null && videoFrame != null)
    //     AlignImages();
}

    public void AlignImages()
    {
        if (primeImage == null || videoFrame == null)
        {
            Debug.LogError("Prime image or video frame is null!");
            return;
        }

        // Convert using our own converter (no Utils dependency)
        Mat primeMat = Texture2DMatConverter.Texture2DToMat(primeImage);
        Mat frameMat = Texture2DMatConverter.Texture2DToMat(videoFrame);

        if (primeMat == null || frameMat == null)
        {
            Debug.LogError("Failed to convert textures to Mats!");
            return;
        }

        // Convert to grayscale
        Mat primeGray = new Mat();
        Mat frameGray = new Mat();
        Imgproc.cvtColor(primeMat, primeGray, Imgproc.COLOR_BGR2GRAY);
        Imgproc.cvtColor(frameMat, frameGray, Imgproc.COLOR_BGR2GRAY);

     // Replace this ORB code:
     // ORB feature detector
     // ORB orb = ORB.create();
     // MatOfKeyPoint keypointsPrime = new MatOfKeyPoint();
      // MatOfKeyPoint keypointsFrame = new MatOfKeyPoint();
     // Mat descriptorsPrime = new Mat();
     // Mat descriptorsFrame = new Mat();
     // orb.detectAndCompute(primeGray, new Mat(), keypointsPrime, descriptorsPrime);
     // orb.detectAndCompute(frameGray, new Mat(), keypointsFrame, descriptorsFrame);

     // With AKAZE:
     AKAZE akaze = AKAZE.create();
     MatOfKeyPoint keypointsPrime = new MatOfKeyPoint();
     MatOfKeyPoint keypointsFrame = new MatOfKeyPoint();
     Mat descriptorsPrime = new Mat();
     Mat descriptorsFrame = new Mat();

     akaze.detectAndCompute(primeGray, new Mat(), keypointsPrime, descriptorsPrime);
     akaze.detectAndCompute(frameGray, new Mat(), keypointsFrame, descriptorsFrame);
        
   
        // Check if enough keypoints found
        if (keypointsPrime.toArray().Length < 4 || keypointsFrame.toArray().Length < 4)
        {
            Debug.LogError("Not enough keypoints found for alignment. Need at least 4.");
            return;
        }

        // Match features using BFMatcher
        // Try: fully qualified NormTypes, or use 0 (HAMMING) going with second becuse error
        // BFMatcher matcher = new BFMatcher(OpenCVForUnity.CoreModule.NormTypes_HAMMING2); 
        //BFMatcher matcher = new BFMatcher(0); //Norm ORB

        BFMatcher matcher = new BFMatcher(4); // 4 = L1 (for AKAZE)
        MatOfDMatch matches = new MatOfDMatch();
        matcher.match(descriptorsPrime, descriptorsFrame, matches);

        // Find Homography
        List<DMatch> matchList = matches.toList();
        List<Point> primePoints = new List<Point>();
        List<Point> framePoints = new List<Point>();

        matchList.Sort((a, b) => a.distance.CompareTo(b.distance));
        int numGoodMatches = Mathf.Min(10, matchList.Count);
        
        if (numGoodMatches < 4)
        {
            Debug.LogError("Not enough good matches to compute Homography. Found: " + numGoodMatches);
            return;
        }

        for (int i = 0; i < numGoodMatches; i++)
        {
            primePoints.Add(keypointsPrime.toList()[matchList[i].queryIdx].pt);
            framePoints.Add(keypointsFrame.toList()[matchList[i].trainIdx].pt);
        }

        MatOfPoint2f primePointsMat = new MatOfPoint2f();
        MatOfPoint2f framePointsMat = new MatOfPoint2f();
        primePointsMat.fromList(primePoints);
        framePointsMat.fromList(framePoints);

        // Calculate Homography matrix
        Mat homography = Calib3d.findHomography(primePointsMat, framePointsMat, Calib3d.RANSAC, 3.0);

        if (homography.empty())
        {
            Debug.LogError("Homography calculation failed!");
            return;
        }

        // Warp the video frame to match the prime image's dimensions
        Mat warpedFrame = new Mat();
        Size primeSize = primeMat.size();
        Imgproc.warpPerspective(frameMat, warpedFrame, homography, primeSize);

        // Convert back to Texture2D using our own converter
        Texture2D resultTexture = Texture2DMatConverter.MatToTexture2D(warpedFrame);

        if (resultTexture == null)
        {
            Debug.LogError("Failed to convert warped Mat to Texture2D!");
            return;
        }

        // Display if assigned
        if (displayImage != null)
        {
            displayImage.texture = resultTexture;
        }

        Debug.Log("Alignment complete! Warped texture is ready.");
    }

    public void SetVideoFrame(Texture2D frame)
    {
        videoFrame = frame;
        AlignImages();
    }
}
