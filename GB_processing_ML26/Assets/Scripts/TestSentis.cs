using UnityEngine;
using Unity.Sentis;

public class TestSentis : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Sentis is installed! Version: " + Application.unityVersion);
        // Try to create a simple tensor to confirm it works
        TensorFloat testTensor = new TensorFloat(new int[] { 1, 3, 3, 3 });
        Debug.Log("Tensor created successfully!");
        testTensor.Dispose();
    }
}
