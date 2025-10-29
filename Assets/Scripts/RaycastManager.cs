using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using System;
using UnityEngine.Analytics;
using UnityEngine.UI;
using Unity.Collections;

public class RaycastManager : MonoBehaviour
{
    public AROcclusionManager occlusionManager;
    public int x = 0;
    public int y = 0;
    public Transform startPoint;
    public bool trigger = false;
    public TMP_Text Text;
    public UnityEngine.UI.RawImage rawImage;

    private static int DepthWidth = 0;
    private static int DepthHeight = 0;
    private Texture2D depthTexture;
    private static short[] depthArray;

    public void GetDepth()
    {
        trigger = trigger ? false : true;
    }

    private void Update()
    {
        if (occlusionManager == null) return;

        if (trigger)
        {
            if (occlusionManager != null)
            {
                // Texture envDepthTexture;
                if (occlusionManager.TryAcquireEnvironmentDepthCpuImage(out XRCpuImage image) && image != null)
                {
                    UpdateRawImage(rawImage, image);
                    Text.text = $"Work";
                    int cx = image.width / 2;
                    int cy = image.height / 2;
                    CopyDepthImageToArray(image);

                    // Compute depth
                    float depthMeters = GetDepthFromXY(cx, cy, depthArray);
                    Text.text = $"Depth @ ({cx},{cy}) = {0} m";
                    // Text.text = "work";
                    return;
                }
            }
            Text.text = "not work";
            // Debug.Log("Depth at (" + x + ", " + y + "): " + depth + " meters.");
        }
    }

    // private void UpdateEnvironmentDepthImage()
    // {
    //     if (occlusionManager &&
    //         occlusionManager.TryAcquireEnvironmentDepthCpuImage(out XRCpuImage image))
    //     {
    //         using (image)
    //         {
    //             UpdateRawImage(ref _depthTexture, image, TextureFormat.R16);
    //             var _depthWidth = image.width;
    //             var _depthHeight = image.height;
    //         }
    //     }
    //     var byteBuffer = _depthTexture.GetRawTextureData();
    //     Buffer.BlockCopy(byteBuffer, 0, _depthArray, 0, byteBuffer.Length);
    // }

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

        // if (x >= DepthWidth || x < 0 || y >= DepthHeight || y < 0)
        // {
        //     return -1f;
        // }

        var depthIndex = (y * DepthWidth) + x;
        var depthInShort = depthArray[depthIndex];
        var depthInMeters = depthInShort * 0.001f;
        return depthInMeters;
    }

    // private static void UpdateRawImage(ref Texture2D texture, XRCpuImage cpuImage, TextureFormat format)
    // {
    //     if (texture == null || texture.width != cpuImage.width || texture.height != cpuImage.height)
    //     {
    //         texture = new Texture2D(cpuImage.width, cpuImage.height, format, false);
    //     }

    //     var conversionParams = new XRCpuImage.ConversionParams(cpuImage, format, XRCpuImage.Transformation.None);
    //     var rawTextureData = texture.GetRawTextureData<byte>();
    //     cpuImage.Convert(conversionParams, rawTextureData);
    //     texture.Apply();
    // }

    private static void UpdateRawImage(UnityEngine.UI.RawImage rawImage, XRCpuImage cpuImage)
    {

        var texture = rawImage.texture as Texture2D;

        if (texture == null || texture.width != cpuImage.width || texture.height != cpuImage.height)
        {
            texture = new Texture2D(cpuImage.width, cpuImage.height, cpuImage.format.AsTextureFormat(), false);
            rawImage.texture = texture;
        }
        // var conversionParams = new XRCpuImage.ConversionParams(cpuImage, cpuImage.format.AsTextureFormat(), XRCpuImage.Transformation.MirrorX);
        var conversionParams = new XRCpuImage.ConversionParams(cpuImage, TextureFormat.R16, XRCpuImage.Transformation.MirrorX);
        // Get the Texture2D's underlying pixel buffer.
        var rawTextureData = texture.GetRawTextureData<byte>();
        // Make sure the destination buffer is large enough to hold the converted data (they should be the same size)
        // Debug.Assert(rawTextureData.Length == cpuImage.GetConvertedDataSize(conversionParams.outputDimensions, conversionParams.outputFormat),
        //     "The Texture2D is not the same size as the converted data.");
        cpuImage.Convert(conversionParams, rawTextureData);
        texture.Apply();

        DepthWidth = cpuImage.width;
        DepthHeight = cpuImage.height;

        var byteBuffer = texture.GetRawTextureData();
        Buffer.BlockCopy(byteBuffer, 0, depthArray, 0, byteBuffer.Length);

        // Get the aspect ratio for the current texture.
        var textureAspectRatio = (float)texture.width / texture.height;

        // Determine the raw image rectSize preserving the texture aspect ratio, matching the screen orientation,
        // and keeping a minimum dimension size.
        const float minDimension = 480.0f;
        var maxDimension = Mathf.Round(minDimension * textureAspectRatio);
        var rectSize = new Vector2(maxDimension, minDimension);
        //var rectSize = new Vector2(minDimension, maxDimension);   //Portrait
        rawImage.rectTransform.sizeDelta = rectSize;
    }


    private void CopyDepthImageToArray(XRCpuImage cpuImage)
    {
        int totalPixels = cpuImage.width * cpuImage.height; 
        if (depthArray == null || depthArray.Length != totalPixels)
        {
            depthArray = new short[totalPixels];
        }
        if (depthTexture == null || depthTexture.width != cpuImage.width || depthTexture.height != cpuImage.height)
        {
            depthTexture = new Texture2D(cpuImage.width, cpuImage.height, TextureFormat.R16, false);
        }
        var conversionParams = new XRCpuImage.ConversionParams(cpuImage, TextureFormat.R16, XRCpuImage.Transformation.MirrorX);

        int dataSize = cpuImage.GetConvertedDataSize(conversionParams);
        var nativeBuffer = new NativeArray<byte>(dataSize, Allocator.Temp);
        cpuImage.Convert(conversionParams, nativeBuffer);

        byte[] byteData = new byte[dataSize];
        nativeBuffer.CopyTo(byteData);

        int byteCount = totalPixels * sizeof(short);
        if (byteCount > byteData.Length)
            byteCount = byteData.Length;

        Buffer.BlockCopy(byteData, 0, depthArray, 0, byteCount);

        depthTexture.LoadRawTextureData(nativeBuffer);
        depthTexture.Apply();
        nativeBuffer.Dispose();
    }

}
