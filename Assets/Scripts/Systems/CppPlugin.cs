using UnityEngine;
using System;                         // IntPtr
using System.Text;                    // StringBuilder
using System.IO;                      // Stream
using System.Collections;             // IEnumerator
using System.Runtime.InteropServices; // DllImport

public class CppPlugin : MonoBehaviour 
{
    // The C++ Plugin is predominantly used for Asynchronous Texture loading as Texture2D's only load Synchronously.
    //
    // It works with the following function order:
    // (1) Init() calls glGenTextures() for m_initMaxNumTextures and allocates m_pWorkingMemory for pixel loading
    // (2) LoadIntoWorkingMemoryFromImagePath() calls through to stbi_load() and sets pixels into m_pWorkingMemory
    // (3) CreateEmptyTexture() calls glTexImage2D() hence allocating the actual texture
    // (4) LoadScanlinesIntoTextureFromWorkingMemory() is called repeatedly until all scanlines are uploaded to the texture through glTexSubImage2D
    // (5) Finally CreateExternalTexture() is called with the texture that’s been created beneath us! 
    // (6) Terminate() calls glDeleteTextures() and delete[] on m_pWorkingMemory

    // **************************
    // C++ Plugin declerations
    // **************************

    [DllImport ("cppplugin")]
    private static extern IntPtr GetRenderEventFunc();

    [DllImport ("cppplugin")]
    private static extern void SetInitMaxNumTextures(int initMaxNumTextures);

    [DllImport ("cppplugin")]
    private static extern void SetMaxPixelsUploadedPerFrame(int maxPixelsUploadedPerFrame);

    [DllImport ("cppplugin")]
    private static extern void SetCurrTextureIndex(int currTextureIndex);

    [DllImport ("cppplugin")]
    private static extern bool IsLoadingIntoTexture();

    [DllImport ("cppplugin")]
    private static extern IntPtr GetCurrStoredTexturePtr();

    [DllImport ("cppplugin")]
    private static extern int GetCurrStoredImageWidth();

    [DllImport ("cppplugin")]
    private static extern int GetCurrStoredImageHeight();   

    [DllImport ("cppplugin")]
    private static extern bool LoadIntoWorkingMemoryFromImagePath(StringBuilder filePath);

    [DllImport ("cppplugin")]
    private static extern bool LoadIntoWorkingMemoryFromImageData(IntPtr pRawData, int dataLength);

    // **************************
    // Member Variables
    // **************************

    private const int kMaxPixelsUploadedPerFrame = 1 * 1024 * 1024;
    private const float kWaitForGLRenderCall = 2.0f/60.0f; // Wait 2 frames

    private const int kMaxNumTextures = 10; // 5 ImageSpheres + 1 Skybox + 4 spare textures
    private const int kLoadingTextureIndex = -1;
    private int[] m_textureIndexUsage;

    private Texture2D m_lastTextureOperatedOn;
    private ThreadJob m_threadJob;

    // These are functions that use OpenGL and hence must be run from the Render Thread!
    enum RenderFunctions
    {
        kInit = 0,
        kCreateEmptyTexture = 1,
        kLoadScanlinesIntoTextureFromWorkingMemory = 2,
        kTerminate = 3
    };

    // **************************
    // Public functions
    // *************************

    public void Start() // NOTE: Due to current underlying C++ implementation being single threaded, there can only be one of these
    {
        m_lastTextureOperatedOn = new Texture2D(2,2);

        m_textureIndexUsage = new int[kMaxNumTextures];

        SetMaxPixelsUploadedPerFrame(kMaxPixelsUploadedPerFrame);
        SetInitMaxNumTextures(kMaxNumTextures);
        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kInit);

        m_threadJob = new ThreadJob(this);
    }

    public void OnDestroy()
    {
        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kTerminate);
    }

    public int GetMaxNumTextures()
    {
        return kMaxNumTextures;
    }

    public int GetLoadingTextureIndex()
    {
        return kLoadingTextureIndex;
    }

    public void SetTextureInUse(int textureID, bool inUse)
    {
        if (textureID != -1) // -1 is the textureID for the loadingTexture!
        {
            if (inUse)
            {
                ++m_textureIndexUsage[textureID];
            }
            else
            {
                --m_textureIndexUsage[textureID];
            }

            if (Debug.isDebugBuild) Debug.Log("------- VREEL: TextureID = " + textureID + ", InUse = " + inUse);
            DebugPrintTextureIndexUsage();
        }
    }

    public IEnumerator LoadImageFromPath(string filePath, int textureIndex = -1)
    {
        if (textureIndex == -1) // This handles the case where LoadImageFromPath() is called without LoadImageFromPathIntoImageSphere()
        {
            textureIndex = GetAvailableTextureIndex();
        }

        yield return new WaitForEndOfFrame();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadImageFromPath() from filePath: " + filePath + ", with TextureIndex: " + textureIndex);
        StringBuilder filePathForCpp = new StringBuilder(filePath);

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadIntoWorkingMemoryFromImagePath(), on background thread!");
        yield return m_threadJob.WaitFor();
        bool ranJobSuccessfully = false;
        m_threadJob.Start( () => 
            ranJobSuccessfully = LoadIntoWorkingMemoryFromImagePath(filePathForCpp)
        );
        yield return m_threadJob.WaitFor();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished LoadIntoWorkingMemoryFromImagePath(), ran Job Successully = " + ranJobSuccessfully); 


        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling CreateEmptyTexture()");
        yield return new WaitForEndOfFrame();
        SetCurrTextureIndex(textureIndex);
        yield return new WaitForEndOfFrame();
        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kCreateEmptyTexture);
        yield return new WaitForSeconds(kWaitForGLRenderCall); // These waits need to be longer to ensure that GL.IssuePluginEvent() has gone through!
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished CreateEmptyTexture(), Texture Handle = " + GetCurrStoredTexturePtr() );


        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadScanlinesIntoTextureFromWorkingMemory()");
        while (IsLoadingIntoTexture())
        {            
            GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kLoadScanlinesIntoTextureFromWorkingMemory);
            yield return new WaitForSeconds(kWaitForGLRenderCall);
        }
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished LoadScanlinesIntoTextureFromWorkingMemory()");


        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling CreateExternalTexture(), size of Texture is Width x Height = " + GetCurrStoredImageWidth() + " x " + GetCurrStoredImageHeight());
        yield return new WaitForEndOfFrame();
        m_lastTextureOperatedOn =
            Texture2D.CreateExternalTexture(
                GetCurrStoredImageWidth(), 
                GetCurrStoredImageHeight(), 
                TextureFormat.RGBA32,           // Default textures have a format of ARGB32
                false,
                false,
                GetCurrStoredTexturePtr()
            );
        yield return new WaitForEndOfFrame();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished CreateExternalTexture()!");

        Resources.UnloadUnusedAssets();
    }

    public IEnumerator LoadImageFromPathIntoImageSphere(ImageSphereController imageSphereController, int sphereIndex, string filePathAndIdentifier)
    {
        int textureIndex = GetAvailableTextureIndex();
        yield return LoadImageFromPath(filePathAndIdentifier, textureIndex);

        /*
        yield return new WaitForEndOfFrame();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadImageFromPathIntoImageSphere() from filePath: " + filePathAndIdentifier + ", with TextureIndex: " + textureIndex);
        StringBuilder filePathForCpp = new StringBuilder(filePathAndIdentifier);

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadIntoWorkingMemoryFromImagePath(), on background thread!");
        yield return m_threadJob.WaitFor();
        bool ranJobSuccessfully = false;
        m_threadJob.Start( () => 
            ranJobSuccessfully = LoadIntoWorkingMemoryFromImagePath(filePathForCpp)
        );
        yield return m_threadJob.WaitFor();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished LoadIntoWorkingMemoryFromImagePath(), ran Job Successully = " + ranJobSuccessfully); 


        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling CreateEmptyTexture()");
        yield return new WaitForEndOfFrame();
        SetCurrTextureIndex(textureIndex);
        yield return new WaitForEndOfFrame();
        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kCreateEmptyTexture);
        yield return new WaitForSeconds(kWaitForGLRenderCall); // These waits need to be longer to ensure that GL.IssuePluginEvent() has gone through!
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished CreateEmptyTexture(), Texture Handle = " + GetCurrStoredTexturePtr() );


        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadScanlinesIntoTextureFromWorkingMemory()");
        while (IsLoadingIntoTexture())
        {            
            GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kLoadScanlinesIntoTextureFromWorkingMemory);
            yield return new WaitForSeconds(kWaitForGLRenderCall);
        }
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished LoadScanlinesIntoTextureFromWorkingMemory()");


        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling CreateExternalTexture(), size of Texture is Width x Height = " + GetCurrStoredImageWidth() + " x " + GetCurrStoredImageHeight());
        yield return new WaitForEndOfFrame();
        Texture2D newTexture =
            Texture2D.CreateExternalTexture(
                GetCurrStoredImageWidth(), 
                GetCurrStoredImageHeight(), 
                TextureFormat.RGBA32,           // Default textures have a format of ARGB32
                false,
                false,
                GetCurrStoredTexturePtr()
            );
        yield return new WaitForEndOfFrame();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished CreateExternalTexture()!");
        */


        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling SetImageAtIndex()");
        imageSphereController.SetImageAtIndex(sphereIndex, m_lastTextureOperatedOn, filePathAndIdentifier, textureIndex);
        yield return new WaitForEndOfFrame();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished SetImageAtIndex()");

        Resources.UnloadUnusedAssets();
    }   

    public IEnumerator LoadImageFromStreamIntoImageSphere(ImageSphereController imageSphereController, int sphereIndex, Stream imageStream, string imageIdentifier)
    {        
        int textureIndex = GetAvailableTextureIndex();
        yield return new WaitForEndOfFrame();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadImageFromStreamIntoImageSphere() for image: " + imageIdentifier + ", with TextureIndex: " + textureIndex);

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling ToByteArray(), on background thread!");
        yield return m_threadJob.WaitFor();
        bool ranJobSuccessfully = false;
        byte[] myBinary = null;
        m_threadJob.Start( () => 
            ranJobSuccessfully = ToByteArray(imageStream, ref myBinary)
        );
        yield return m_threadJob.WaitFor();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished ToByteArray(), ran Job Successully = " + ranJobSuccessfully);

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadIntoWorkingMemoryFromImagePath(), on background thread!");
        GCHandle rawDataHandle = GCHandle.Alloc(myBinary, GCHandleType.Pinned);
        IntPtr rawDataPtr = rawDataHandle.AddrOfPinnedObject();
        yield return m_threadJob.WaitFor();
        ranJobSuccessfully = false;
        m_threadJob.Start( () => 
            ranJobSuccessfully = LoadIntoWorkingMemoryFromImageData(rawDataPtr, myBinary.Length)
        );
        yield return m_threadJob.WaitFor();
        rawDataHandle.Free();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished LoadIntoWorkingMemoryFromImagePath(), ran Job Successully = " + ranJobSuccessfully); 


        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling CreateEmptyTexture()");
        yield return new WaitForEndOfFrame();
        SetCurrTextureIndex(textureIndex);
        yield return new WaitForEndOfFrame();
        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kCreateEmptyTexture);
        yield return new WaitForSeconds(kWaitForGLRenderCall); // These waits need to be longer to ensure that GL.IssuePluginEvent() has gone through!
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished CreateEmptyTexture(), Texture Handle = " + GetCurrStoredTexturePtr() );


        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadScanlinesIntoTextureFromWorkingMemory()");
        while (IsLoadingIntoTexture())
        {            
            GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kLoadScanlinesIntoTextureFromWorkingMemory);
            yield return new WaitForSeconds(kWaitForGLRenderCall); // These waits need to be longer to ensure that GL.IssuePluginEvent() has gone through!
        }
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished LoadScanlinesIntoTextureFromWorkingMemory()");


        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling CreateExternalTexture(), size of Texture is Width x Height = " + GetCurrStoredImageWidth() + " x " + GetCurrStoredImageHeight());
        yield return new WaitForEndOfFrame();
        m_lastTextureOperatedOn =
            Texture2D.CreateExternalTexture(
                GetCurrStoredImageWidth(), 
                GetCurrStoredImageHeight(), 
                TextureFormat.RGBA32,           // Default textures have a format of ARGB32
                false,
                false,
                GetCurrStoredTexturePtr()
            );
        yield return new WaitForEndOfFrame();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished CreateExternalTexture()!");


        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling SetImageAtIndex()");
        imageSphereController.SetImageAtIndex(sphereIndex, m_lastTextureOperatedOn, imageIdentifier, textureIndex);
        yield return new WaitForEndOfFrame();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished SetImageAtIndex()");

        Resources.UnloadUnusedAssets();
    }        

    // **************************
    // Private/Helper functions
    // **************************

    private bool ToByteArray(Stream stream, ref byte[] outBinary)
    {                
        const int kBlockSize = 1024;
        byte[] buf = new byte[kBlockSize];
        using( MemoryStream ms = new MemoryStream() ) 
        {            
            int byteCount = 0;
            do
            {
                byteCount = stream.Read(buf, 0, kBlockSize);
                ms.Write(buf, 0, byteCount);
            }
            while(stream.CanRead && byteCount > 0);

            outBinary = ms.ToArray();
        }

        return true;
    }

    private int GetAvailableTextureIndex()
    {
        for (int i = 0; i < kMaxNumTextures; i++)
        {            
            if (m_textureIndexUsage[i] == 0)
            {
                return i;
            }
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - We have no more textures available!!!");
        DebugPrintTextureIndexUsage();

        return -1;
    }

    private void DebugPrintTextureIndexUsage()
    {
        if (Debug.isDebugBuild) 
            Debug.Log("------- VREEL: " + m_textureIndexUsage[0] + ", " + m_textureIndexUsage[1] + ", " + m_textureIndexUsage[2] 
                + ", " + m_textureIndexUsage[3] + ", " + m_textureIndexUsage[4] + ", " + m_textureIndexUsage[5] 
                + ", " + m_textureIndexUsage[6] + ", " + m_textureIndexUsage[7] 
                + ", " + m_textureIndexUsage[8] + ", " + m_textureIndexUsage[9]);
    }
}