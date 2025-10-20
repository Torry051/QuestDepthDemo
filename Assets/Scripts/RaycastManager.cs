using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using System;

public class RaycastManager : MonoBehaviour
{
    public AROcclusionManager occlusionManager;
    public int x = 0;
    public int y = 0;
    public Transform startPoint;
    public bool trigger = false;
    Texture2D _depthTexture;
    short[] _depthArray;

    private static int DepthWidth = 0;
    private static int DepthHeight = 0;

    private void Update()
    {
        if (trigger)
        {
            trigger = false;
            UpdateEnvironmentDepthImage();
            float depth = GetDepthFromXY(x, y, _depthArray);
            Debug.Log("Depth at (" + x + ", " + y + "): " + depth + " meters.");
        }
    }

    private void UpdateEnvironmentDepthImage()
    {
        if (occlusionManager &&
            occlusionManager.TryAcquireEnvironmentDepthCpuImage(out XRCpuImage image))
        {
            using (image)
            {
                UpdateRawImage(ref _depthTexture, image, TextureFormat.R16);
                var _depthWidth = image.width;
                var _depthHeight = image.height;
            }
        }
        var byteBuffer = _depthTexture.GetRawTextureData();
        Buffer.BlockCopy(byteBuffer, 0, _depthArray, 0, byteBuffer.Length);
    }

    public static float GetDepthFromUV(Vector2 uv, short[] depthArray)
    {
        int depthX = (int)(uv.x * (DepthWidth - 1));
        int depthY = (int)(uv.y * (DepthHeight - 1));

        return GetDepthFromXY(depthX, depthY, depthArray);
    }

    // Obtain the depth value in meters at the specified x, y location.
    public static float GetDepthFromXY(int x, int y, short[] depthArray)
    {
        // if (!Initialized)
        // {
        //     return InvalidDepthValue;
        // }

        if (x >= DepthWidth || x < 0 || y >= DepthHeight || y < 0)
        {
            return -1f;
        }

        var depthIndex = (y * DepthWidth) + x;
        var depthInShort = depthArray[depthIndex];
        var depthInMeters = depthInShort * 0.001f;
        return depthInMeters;
    }

    private static void UpdateRawImage(ref Texture2D texture, XRCpuImage cpuImage, TextureFormat format)
    {
        if (texture == null || texture.width  != cpuImage.width || texture.height != cpuImage.height)
        {
            texture = new Texture2D(cpuImage.width, cpuImage.height, format, false);
        }

        var conversionParams = new XRCpuImage.ConversionParams(cpuImage, format, XRCpuImage.Transformation.None);
        var rawTextureData = texture.GetRawTextureData<byte>();
        cpuImage.Convert(conversionParams, rawTextureData);
        texture.Apply();
    }
}
