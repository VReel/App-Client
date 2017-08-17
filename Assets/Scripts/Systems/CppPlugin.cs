using UnityEngine;
using System;                         // IntPtr
using System.Text;                    // StringBuilder
using System.IO;                      // Stream
using System.Collections;             // IEnumerator
using System.Runtime.InteropServices; // DllImport

public class CppPlugin
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
    private static extern void SetMaxImageWidth(int maxImageWidth);

    [DllImport ("cppplugin")]
    private static extern void SetRGB565On(bool rgb565On);

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

    private WaitForEndOfFrame m_waitForEndOfFrame;
    private WaitForSeconds m_waitForSeconds;

    private MonoBehaviour m_owner;
    private Texture2D m_lastTextureOperatedOn;
    private ThreadJob m_threadJob;   

    // These are functions that use OpenGL and hence must be run from the Render Thread!
    enum RenderFunctions
    {
        kInit = 0,
        kCreateEmptyTexture = 1,
        kLoadScanlinesIntoTextureFromWorkingMemory = 2,
        kRenewTextureHandle = 3,
        kTerminate = 4
    };

    // **************************
    // Public functions
    // **************************

    // NOTE: Due to current underlying C++ implementation being single threaded, there can only be one of these
    public CppPlugin(MonoBehaviour owner, int maxNumTextures)
    {
        m_owner = owner;
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: A CppPlugin object was Created by = " + m_owner.name);

        m_lastTextureOperatedOn = new Texture2D(2,2);

        SetMaxPixelsUploadedPerFrame(kMaxPixelsUploadedPerFrame);
        SetInitMaxNumTextures(maxNumTextures);
        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kInit);

        m_waitForEndOfFrame = new WaitForEndOfFrame();
        m_waitForSeconds = new WaitForSeconds(kWaitForGLRenderCall);

        m_threadJob = new ThreadJob(owner);
    }

    ~CppPlugin()
    {
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: A CppPlugin object was Destructed by = " + m_owner.name);

        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kTerminate);
    }
        
    public IEnumerator LoadImageFromPathIntoImageSphere(ImageSphereController imageSphereController, int sphereIndex, string filePathAndIdentifier, int textureIndex, int maxImageWidth)
    {
        StringBuilder filePathForCpp = new StringBuilder(filePathAndIdentifier);
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadImageFromPathIntoImageSphere() with sphereIndex : "  + sphereIndex + ", from filePath: " + filePathAndIdentifier + ", with TextureIndex: " + textureIndex + ", with MaxImageWidth: " + maxImageWidth);
        yield return null;

        SetRGB565On(Helper.kRGB565On);
        SetMaxImageWidth(maxImageWidth);

        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadIntoWorkingMemoryFromImagePath(), on background thread!");
        yield return m_threadJob.WaitFor();
        bool ranJobSuccessfully = false;
        m_threadJob.Start( () => 
            ranJobSuccessfully = LoadIntoWorkingMemoryFromImagePath(filePathForCpp)
        );
        yield return m_threadJob.WaitFor();
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished LoadIntoWorkingMemoryFromImagePath(), ran Job Successully = " + ranJobSuccessfully); 


        //TODO: Make CreateEmptyTexture() more efficient - the problem is simply that a glTexImage2D() call is slow with large textures!
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling CreateEmptyTexture() over textureIndex = " + textureIndex);
        yield return m_waitForEndOfFrame;
        SetCurrTextureIndex(textureIndex);
        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kRenewTextureHandle);
        yield return m_waitForSeconds; // These waits need to be longer to ensure that GL.IssuePluginEvent() has gone through!

        yield return null;
        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kCreateEmptyTexture);
        yield return m_waitForSeconds; // These waits need to be longer to ensure that GL.IssuePluginEvent() has gone through!
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished CreateEmptyTexture(), Texture Handle = " + GetCurrStoredTexturePtr() );


        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadScanlinesIntoTextureFromWorkingMemory()");
        while (IsLoadingIntoTexture())
        {            
            yield return m_waitForEndOfFrame;
            GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kLoadScanlinesIntoTextureFromWorkingMemory);
            yield return m_waitForSeconds;
        }
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished LoadScanlinesIntoTextureFromWorkingMemory()");


        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling CreateExternalTexture(), size of Texture is Width x Height = " + GetCurrStoredImageWidth() + " x " + GetCurrStoredImageHeight());
        yield return m_waitForEndOfFrame;
        m_lastTextureOperatedOn =
            Texture2D.CreateExternalTexture(
                GetCurrStoredImageWidth(), 
                GetCurrStoredImageHeight(), 
                Helper.kRGB565On ? TextureFormat.RGB565 : TextureFormat.RGB24, // Default textures have a format of ARGB32
                true,
                true,
                GetCurrStoredTexturePtr()
            );
        yield return null;
        m_lastTextureOperatedOn.filterMode = FilterMode.Trilinear;
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished CreateExternalTexture()!");


        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling SetImageAtIndex()");
        imageSphereController.SetImageAtIndex(sphereIndex, m_lastTextureOperatedOn, filePathAndIdentifier, textureIndex, true);
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished SetImageAtIndex()");

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Completed LoadImageFromPathIntoImageSphere() with sphereIndex : "  + sphereIndex + ", from filePath: " + filePathAndIdentifier + ", with TextureIndex: " + textureIndex);
    }   
           
    public IEnumerator LoadImageFromStreamIntoImageSphere(ImageSphereController imageSphereController, int sphereIndex, Stream imageStream, string imageIdentifier, int textureIndex, int contentLength)
    {
        yield return null;
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadImageFromStreamIntoImageSphere() with sphereIndex: " + sphereIndex + ", imageIdentifier: " + imageIdentifier + ", with TextureIndex: " + textureIndex);

        SetRGB565On(Helper.kRGB565On);
        SetMaxImageWidth(Helper.kMaxImageWidth);



        var startTime = DateTime.UtcNow;


        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling ToByteArray(), on background thread!");
        yield return m_threadJob.WaitFor();
        bool ranJobSuccessfully = false;
        byte[] myBinary = null;
        m_threadJob.Start( () => 
            ranJobSuccessfully = ToByteArray(imageStream, contentLength, ref myBinary)
        );
        yield return m_threadJob.WaitFor();
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished ToByteArray(), ran Job Successully = " + ranJobSuccessfully);


        //if (Debug.isDebugBuild) Debug.Log("------- VREEL-TEST: 1 " + (DateTime.UtcNow-startTime));
        startTime = DateTime.UtcNow;

        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadIntoWorkingMemoryFromImagePath(), on background thread!");
        GCHandle rawDataHandle = GCHandle.Alloc(myBinary, GCHandleType.Pinned);
        IntPtr rawDataPtr = rawDataHandle.AddrOfPinnedObject();
        yield return m_threadJob.WaitFor();
        ranJobSuccessfully = false;
        m_threadJob.Start( () => 
            ranJobSuccessfully = LoadIntoWorkingMemoryFromImageData(rawDataPtr, myBinary.Length)
        );
        yield return m_threadJob.WaitFor();
        rawDataHandle.Free();
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished LoadIntoWorkingMemoryFromImagePath(), ran Job Successully = " + ranJobSuccessfully); 


        //if (Debug.isDebugBuild) Debug.Log("------- VREEL-TEST: 2 " + (DateTime.UtcNow-startTime));
        startTime = DateTime.UtcNow;


        //TODO: Make CreateEmptyTexture() more efficient - the problem is simply that a glTexImage2D() call is slow with large textures!
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling CreateEmptyTexture()");
        yield return m_waitForEndOfFrame;
        SetCurrTextureIndex(textureIndex);
        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kRenewTextureHandle);
        yield return m_waitForSeconds; // These waits need to be longer to ensure that GL.IssuePluginEvent() has gone through!

        yield return null;
        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kCreateEmptyTexture);
        yield return m_waitForSeconds; // These waits need to be longer to ensure that GL.IssuePluginEvent() has gone through!
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished CreateEmptyTexture(), Texture Handle = " + GetCurrStoredTexturePtr() );


        //if (Debug.isDebugBuild) Debug.Log("------- VREEL-TEST: 3 " + (DateTime.UtcNow-startTime));
        startTime = DateTime.UtcNow;

        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadScanlinesIntoTextureFromWorkingMemory()");
        while (IsLoadingIntoTexture())
        {            
            yield return m_waitForEndOfFrame;
            GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kLoadScanlinesIntoTextureFromWorkingMemory);
            yield return m_waitForSeconds; // These waits need to be longer to ensure that GL.IssuePluginEvent() has gone through!
        }
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished LoadScanlinesIntoTextureFromWorkingMemory()");


        //if (Debug.isDebugBuild) Debug.Log("------- VREEL-TEST: 4 " + (DateTime.UtcNow-startTime));
        startTime = DateTime.UtcNow;

        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling CreateExternalTexture(), size of Texture is Width x Height = " + GetCurrStoredImageWidth() + " x " + GetCurrStoredImageHeight());
        yield return m_waitForEndOfFrame;
        m_lastTextureOperatedOn =
            Texture2D.CreateExternalTexture(
                GetCurrStoredImageWidth(), 
                GetCurrStoredImageHeight(), 
                Helper.kRGB565On ? TextureFormat.RGB565 : TextureFormat.RGB24, // Default textures have a format of ARGB32
                true,
                true,
                GetCurrStoredTexturePtr()
            );
        yield return null;
        m_lastTextureOperatedOn.filterMode = FilterMode.Trilinear;
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished CreateExternalTexture()!");

        //if (Debug.isDebugBuild) Debug.Log("------- VREEL-TEST: 5 " + (DateTime.UtcNow-startTime));
        startTime = DateTime.UtcNow;

        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling SetImageAtIndex()");
        imageSphereController.SetImageAtIndex(sphereIndex, m_lastTextureOperatedOn, imageIdentifier, textureIndex, true);
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished SetImageAtIndex()");

        //if (Debug.isDebugBuild) Debug.Log("------- VREEL-TEST: 6 " + (DateTime.UtcNow-startTime));
        startTime = DateTime.UtcNow;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Completed LoadImageFromStreamIntoImageSphere() with sphereIndex: " + sphereIndex + ", imageIdentifier: " + imageIdentifier + ", with TextureIndex: " + textureIndex);
    }  


    //WIP
    public IEnumerator TestLoad(ImageSphereController imageSphereController, int sphereIndex, string url, string imageIdentifier, int textureIndex)
    {
        var startTime = DateTime.UtcNow;

        WWW www = new WWW(url);
        yield return www;


        if (Debug.isDebugBuild) Debug.Log("------- VREEL-TEST: TestLoad() 1 " + (DateTime.UtcNow-startTime));
        yield return new WaitForSeconds(2);
        startTime = DateTime.UtcNow;


        byte[] newBytes = new byte[4096 * 2048 * 3];

        for (int i = 0; i < (4096 * 2048); i++)
        {
            newBytes[i*3 + 0] = 0;
            newBytes[i*3 + 1] = 255;
            newBytes[i*3 + 2] = 0;
        }

        //DO STUFF
        /*
        GCHandle rawDataHandle = GCHandle.Alloc(myBinary, GCHandleType.Pinned);
        IntPtr rawDataPtr = rawDataHandle.AddrOfPinnedObject();
        yield return m_threadJob.WaitFor();
        ranJobSuccessfully = false;
        m_threadJob.Start( () => 
            ranJobSuccessfully = LoadIntoWorkingMemoryFromImageData(rawDataPtr, myBinary.Length)
        );
        yield return m_threadJob.WaitFor();
        rawDataHandle.Free();
        */


        if (Debug.isDebugBuild) Debug.Log("------- VREEL-TEST: TestLoad() 2 " + (DateTime.UtcNow-startTime));
        yield return new WaitForSeconds(2);
        startTime = DateTime.UtcNow;


        m_lastTextureOperatedOn = new Texture2D(4096, 2048, TextureFormat.RGB24, false);
        m_lastTextureOperatedOn.LoadRawTextureData(newBytes);


        //www.LoadImageIntoTexture(m_lastTextureOperatedOn);


        if (Debug.isDebugBuild) Debug.Log("------- VREEL-TEST: TestLoad() 3 " + (DateTime.UtcNow-startTime));
        yield return new WaitForSeconds(2);
        startTime = DateTime.UtcNow;


        m_lastTextureOperatedOn.Apply();




        if (Debug.isDebugBuild) Debug.Log("------- VREEL-TEST: TestLoad() 4 " + (DateTime.UtcNow-startTime));
        yield return new WaitForSeconds(2);
        startTime = DateTime.UtcNow;


        imageSphereController.SetImageAtIndex(sphereIndex, m_lastTextureOperatedOn, imageIdentifier, textureIndex, true);



        if (Debug.isDebugBuild) Debug.Log("------- VREEL-TEST: TestLoad() 5 " + (DateTime.UtcNow-startTime));
    }



    // **************************
    // Private/Helper functions
    // **************************

    private bool ToByteArray(Stream stream, int contentLength, ref byte[] outBinary)
    {       
        /*
        const int kBlockSize = 1024*1024;
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
        */

        // Now read s into a byte buffer with a little padding.

        Debug.Log("------- VREEL-TEST: ToByteArray() - length " + contentLength);

        int numIterations = 0;

        outBinary = new byte[contentLength];
        int numBytesToRead = contentLength;
        int numBytesRead = 0;
        do
        {
            // Read may return anything from 0 to 10.
            int n = stream.Read(outBinary, numBytesRead, numBytesToRead);
            numBytesRead += n;
            numBytesToRead -= n;

            numIterations++;
        } 
        while (numBytesToRead > 0);

        Debug.Log("------- VREEL-TEST: ToByteArray() iterations " + numIterations);

        return true;
    }
}