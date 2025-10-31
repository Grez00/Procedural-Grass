using UnityEngine;
using System;

public class PrefixSum : MonoBehaviour
{
    [SerializeField] ComputeShader prefixShader;
    [SerializeField] int[] data;
    private ComputeBuffer outputBuffer;
    private int[] result;

    void Start()
    {
        outputBuffer = new ComputeBuffer(data.Length, sizeof(int), ComputeBufferType.Structured);
        outputBuffer.SetData(data);

        ParallelPrefixSum(outputBuffer);
        result = new int[data.Length];
        outputBuffer.GetData(result);

        int[] seqResult = SequentialPrefixSum(data);

        Debug.Log("Sequential Result: " + string.Join(",", seqResult));
        Debug.Log("Parallel Result: " + string.Join(",", result));
    }

    void ParallelPrefixSum(ComputeBuffer outputBuffer)
    {
        prefixShader.SetBuffer(0, "_Result", outputBuffer);
        prefixShader.SetInt("_ArrayLength", data.Length);

        uint threadGroupSizeX;
        prefixShader.GetKernelThreadGroupSizes(0, out threadGroupSizeX, out _, out _);
        int threadGroupsX = Mathf.CeilToInt((float)data.Length / threadGroupSizeX);
        prefixShader.Dispatch(0, threadGroupsX, 1, 1);
    }
    
    int[] SequentialPrefixSum(int[] inputArray)
    {
        int[] outputArray = (int[])inputArray.Clone();
        for (int i = 1; i < inputArray.Length; i++)
        {
            outputArray[i] = outputArray[i] + outputArray[i - 1];
        }
        return outputArray;
    }
}
