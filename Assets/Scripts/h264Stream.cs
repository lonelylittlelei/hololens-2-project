// Class that encapsulates the h264 decoder functionality
// as well as the stream parameters (eg. width, height, textures, etc.)
// Each source of h264 stream should have an instance of this class

// TODO:
//   - The c++ getoutput can return multiple outputs in one frame
//   - This is currently only doing frame in and frame out
//   - this can lead to bloat if frames are comming in faster than the framerate
//   - because the texture update is queued on the main thread
//   - instead should have a queue of frames and update the texture at the framerate
//   - this means that some frames will be dropped if they come in too fast
//  - but they still need to be processed


// NOTE: At the moment we are not handling frame size changes, so might crash if this happens


// TODO:   
//  - Move h264 into an actual unity object and clone instead of creating with script
//  - Make multiple of these objects to make sure the decoder is loading correctly before using
//  - Put the "getoutput" from the decoder into the update() of the new object to keep it from running multiple times within a cycle
//  - Reduce number of copies that is happening in the decoder (change dll)
//  - Change the aspect ratio update to work again


using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Net.Sockets;
using System.Threading;

public class h264Stream : MonoBehaviour
{

#if UNITY_EDITOR
    private const string DllName = "MFh264Decoder.dll";
#else
    private const string DllName = "MFh264Decoder_ARM64.dll";
#endif

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public extern static IntPtr CreateDecoder();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public extern static void ReleaseDecoder(IntPtr decoder);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public extern static int InitializeDecoder(IntPtr decoder, int width, int height);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public extern static int SubmitInputToDecoder(IntPtr decoder, byte[] pInData, int dwInSize);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public extern static bool GetOutputFromDecoder(IntPtr decoder, byte[] pOutData, int dwOutSize);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public extern static int GetFrameWidth(IntPtr decoder);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public extern static int GetFrameHeight(IntPtr decoder);


    private int m_width;
    private int m_height;

    // For textures, materials and image output
    private Texture2D yPlaneTexture;
    private Texture2D uvPlaneTexture;
    private byte[] m_outputData;

    byte[] m_yPlane;
    byte[] m_uvPlane;

    private Shader nv12Shader;
    public Material nv12Material;               // Not actually used, but include it here so that it forces it and the shader to go across

    public bool IsInitialized { get; private set; } = false;

    private IntPtr decoderInstance = IntPtr.Zero;       // Pointer to the decoder instance

    private bool hasOutput = false;

    // For output thread
    private Thread outputThread;


    // NOTE:  The shader will not copy to the Hololens if it does not exist SOMEWHERE in the editor project
    // I have attached it to the video panel prototype in the editor, but will be replaced on initialization with the instanced one


    void Start()
    {

    }

    void Update()
    {
        if (!IsInitialized) return;

        if (hasOutput)
        {
            UpdateTextures();
            hasOutput = false;
        }
    }

    void OnDestroy()
    {
        IsInitialized = false;

        // Delete textures
        yPlaneTexture = null;
        uvPlaneTexture = null;


        // Stop output thread        
        if (outputThread != null && outputThread.IsAlive)
        {
            outputThread.Join();
        }


        if (decoderInstance != IntPtr.Zero)
        {
            ReleaseDecoder(decoderInstance);
            decoderInstance = IntPtr.Zero;
        }
    }

    private void GetOutputThread()
    {
        while (IsInitialized)
        {
            GetOutput();
            Thread.Sleep(30); // Adjust this delay as needed        
        }
    }



    public int Initialize(int width, int height)
    {
        m_width = width;
        m_height = height;

        // Create decoder instance
        decoderInstance = CreateDecoder();
        if (decoderInstance == IntPtr.Zero)
        {
            UnityEngine.Debug.LogError("Failed to create decoder instance.");
            return -1; // Indicate failure
        }
        else if (decoderInstance != IntPtr.Zero)
        {
            UnityEngine.Debug.Log("Decoder instance created.");
        }

        int hr = InitializeDecoder(decoderInstance, width, height);
        if (hr != 0)
        {
            UnityEngine.Debug.LogError($"Failed to initialize decoder: HRESULT {hr}");
            return -1;
        }
        else
        {
            UnityEngine.Debug.Log("Decoder initialized.");
        }

        // Create the textures  -- uncapped
        yPlaneTexture = new Texture2D(width, height, TextureFormat.R8, false);
        uvPlaneTexture = new Texture2D(width / 2, height / 2, TextureFormat.RG16, false); // Assuming width and height are even -- UV is half the size of Y

        // Following commented out as now using the shader from the editor

        // Create a new basic material with the NV12 shader
        // This will allow each objects material to be different
        nv12Shader = Shader.Find("Custom/YUVtoRGBHololens");
        // Check if the shader is found
        if (nv12Shader == null)
        {
            UnityEngine.Debug.LogError("Shader not found");
        }
        nv12Material = new Material(nv12Shader);

        // Set the shader texture references
        nv12Material.SetTexture("_YTex", yPlaneTexture);
        nv12Material.SetTexture("_UVTex", uvPlaneTexture);

        // Initialize m_outputData to be the size of the frame = width * height * 3 / 2
        m_outputData = new byte[width * height * 3 / 2];

        IsInitialized = true;

        // Start the get output thread loop
        outputThread = new Thread(GetOutputThread);
        //outputThread.Start();


        return 0;
    }

    public int GetWidth()
    {
        if (decoderInstance == IntPtr.Zero) return 1;
        return GetFrameWidth(decoderInstance);     // Returns the width of the internal frame buffer
    }

    public int GetHeight()
    {
        if (decoderInstance == IntPtr.Zero) return 1;
        return GetFrameHeight(decoderInstance);   // Returns the height of the internal frame buffer
    }

    public float GetAspectRatio()
    {
        return (float)GetWidth() / (float)GetHeight();
    }

    public void GetYPlaneTexture(ref Texture2D yPlaneTexture)
    {
        yPlaneTexture = this.yPlaneTexture;
    }

    public void GetUVPlaneTexture(ref Texture2D uvPlaneTexture)
    {
        uvPlaneTexture = this.uvPlaneTexture;
    }

    public void GetTextures(ref Texture2D yPlaneTexture, ref Texture2D uvPlaneTexture)
    {
        yPlaneTexture = this.yPlaneTexture;
        uvPlaneTexture = this.uvPlaneTexture;
    }

    public Material GetMaterial()
    {
        return nv12Material;
    }

    public void SetWidthAndHeight(int width, int height)
    {
        m_width = width;
        m_height = height;
    }

    public int ProcessFrame(byte[] inData, int width, int height)
    {
        // Check if the decoder is initialized
        if (decoderInstance == IntPtr.Zero) return -1;

        // The caller needs to privde the width and height as it may not be the same as the internal buffer

        int submitResult = SubmitInputToDecoder(decoderInstance, inData, inData.Length);
        if (submitResult != 0)  // Failed
        {
            return -1;
        }

        return 0;
    }

    public int GetOutput()
    {
        if (decoderInstance == IntPtr.Zero) return -1;
        hasOutput = false;      // Reset hasOutput

        // Process output
        // This may not return anything on the first few frames
        int width = m_width;
        int height = m_height;

        // Make sure that m_outputData is at least as big as width * height * 3 / 2
        if (m_outputData == null || m_outputData.Length < width * height * 3 / 2)
        {
            m_outputData = new byte[width * height * 3 / 2];
        }
        // Make sure that yPlane and uvPlane are at least as big as we need
        if (m_yPlane == null || m_yPlane.Length < width * height)
        {
            m_yPlane = new byte[width * height];
        }
        if (m_uvPlane == null || m_uvPlane.Length < width * height / 2)         // half the size of Y
        {
            m_uvPlane = new byte[width * height / 2];
        }


        bool getOutputResult = GetOutputFromDecoder(decoderInstance, m_outputData, m_outputData.Length);        // This returns true if successful    
        if (getOutputResult)
        {
            hasOutput = true;
            return 0;
        }
        else
        {
            // Probably just need more input
            //UnityEngine.Debug.Log("Returned not S_OK");
            // Failed to get output 
            // hasOutput should already be false            
            return -1;
        }
    }

    private void UpdateTextures()
    {
        int width = m_width;
        int height = m_height;

        // Get Y and UV size
        int ySize = width * height;
        int uvSize = width * height / 2;

        System.Buffer.BlockCopy(m_outputData, 0, m_yPlane, 0, ySize);
        System.Buffer.BlockCopy(m_outputData, ySize, m_uvPlane, 0, uvSize);


        // TODO: Process all frames, but only output the most recent. 
        // The network controller should dump frames into the decoder as fast as possible


        // // Update the textures on the main thread

        //check size of texture and resize if necessary
        if (yPlaneTexture.width != m_width || yPlaneTexture.height != m_height)
        {
            yPlaneTexture.Resize(m_width, m_height);
        }
        if (uvPlaneTexture.width != m_width / 2 || uvPlaneTexture.height != m_height / 2)
        {
            uvPlaneTexture.Resize(m_width / 2, m_height / 2);
        }

        yPlaneTexture.LoadRawTextureData(m_yPlane);
        yPlaneTexture.Apply(false);                     // False = asynchronous

        uvPlaneTexture.LoadRawTextureData(m_uvPlane);
        uvPlaneTexture.Apply(false);                    // False = asynchronous

    }

}