using System.Linq;
using UnityEngine;

public struct Cube
{
    public Vector3 position;
    public Color color;
}

public class ComputeShaderTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public ComputeShader computeShader;
    public RenderTexture renderTexture;

    public int cubeCountResolution = 5;

    void Start()
    {
        //int[] intData = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        renderTexture = new RenderTexture(256, 256, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        /*Debug.Log(string.Join(",", intData));

        ComputeBuffer buffer = new ComputeBuffer(intData.Length, sizeof(int));
        buffer.SetData(intData);
        int kernelId = computeShader.FindKernel("CSMain");
        computeShader.SetBuffer(kernelId, "Numbers", buffer);
        computeShader.Dispatch(kernelId, 2, 1, 1);
        buffer.GetData(intData);
        Debug.Log(string.Join(",", intData));*/

        int kernelId = computeShader.FindKernel("CSMain");
        computeShader.SetTexture(kernelId, "Result", renderTexture);
        computeShader.SetFloat("Resolution", 256);
        computeShader.Dispatch(kernelId, 256 / 8, 256 / 8, 1);

        //computeShader.SetTexture(0, "Result", renderTexture);
        //computeShader.SetFloat("Resolution", renderTexture.width);
        //computeShader.SetBuffer(0, "")


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
