using UnityEngine;
using System;                         // Exception
using System.IO;                      // Stream
using System.Collections;             // IEnumerator
using System.Net;                     // HttpWebRequest

public class ImageLoader : MonoBehaviour 
{    
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private LoadingIcon m_loadingIcon;

    private const int kMaxNumTextures = 12; // 5 ImageSpheres + 1 Skybox + 1 ProfileImage + 5 spare textures
    private const int kLoadingTextureIndex = -1;
    private int[] m_textureIndexUsage;

    private bool m_isLoading = false;
    private CppPlugin m_cppPlugin;
    private CoroutineQueue m_coroutineQueue;
    private ThreadJob m_threadJob;

    // **************************
    // Public functions
    // *************************

    public void Start() // NOTE: Due to current underlying C++ implementation being single threaded, there can only be one of these
    {
        m_cppPlugin = new CppPlugin(this, kMaxNumTextures);

        m_textureIndexUsage = new int[kMaxNumTextures];
        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();

        m_threadJob = new ThreadJob(this);
    }

    public bool IsLoading()
    {
        return m_isLoading;
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

    public void InvalidateLoading()
    {
        m_coroutineQueue.Clear();
    }

    public void LoadImageFromPathIntoImageSphere(ImageSphereController imageSphereController, int sphereIndex, string filePathAndIdentifier, bool showLoading, int maxImageWidth)
    {
        m_coroutineQueue.EnqueueAction(LoadImageFromPathIntoImageSphereInternal(imageSphereController, sphereIndex, filePathAndIdentifier, showLoading, maxImageWidth));
    }

    public void LoadImageFromURLIntoImageSphere(ImageSphereController imageSphereController, int sphereIndex, string url, string filePathAndIdentifier, bool showLoading)
    {
        m_coroutineQueue.EnqueueAction(LoadImageFromURLIntoImageSphereInternal(imageSphereController, sphereIndex, url, filePathAndIdentifier, showLoading));
    }        

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator LoadImageFromPathIntoImageSphereInternal(ImageSphereController imageSphereController, int sphereIndex, string filePathAndIdentifier, bool showLoading, int maxImageWidth)
    {
        m_isLoading = true;
        if (showLoading)
        {
            m_loadingIcon.Display();
        }

        int textureIndex = GetAvailableTextureIndex();
        yield return m_cppPlugin.LoadImageFromPathIntoImageSphere(imageSphereController, sphereIndex, filePathAndIdentifier, textureIndex, maxImageWidth);

        m_isLoading = false;
        if (showLoading)
        {
            m_loadingIcon.Hide();
        }
    }

    private IEnumerator LoadImageFromURLIntoImageSphereInternal(ImageSphereController imageSphereController, int sphereIndex, string url, string imageIdentifier, bool showLoading)
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_isLoading = true;
        if (showLoading)
        {
            m_loadingIcon.Display();
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Downloading image and getting stream through GetImageStreamFromURL() with url: " + url);
        yield return m_threadJob.WaitFor();
        bool debugOn = Debug.isDebugBuild;
        Stream imageStream = null;
        bool success = false;
        m_threadJob.Start( () => 
            success = GetImageStreamFromURL(url, ref imageStream, debugOn)
        );
        yield return m_threadJob.WaitFor();

        if (success)
        {
            int textureIndex = GetAvailableTextureIndex();
            yield return m_cppPlugin.LoadImageFromStreamIntoImageSphere(imageSphereController, sphereIndex, imageStream, imageIdentifier, textureIndex);

            imageStream.Close();
        }
        else
        {
            imageSphereController.SetImageAtIndexToLoading(sphereIndex);
        }
            
        m_isLoading = false;
        if (showLoading)
        {
            m_loadingIcon.Hide();
        }
    }                   

    private bool GetImageStreamFromURL(string url, ref Stream imageStream, bool debugOn)
    {        
        try
        {
            HttpWebRequest http = (HttpWebRequest)WebRequest.Create(url);
            imageStream = http.GetResponse().GetResponseStream();
        }
        catch(Exception e)
        {
            if (debugOn) Debug.Log("------- VREEL: ERROR - WebRequest got an exception: " + e);
            return false;
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
                + ", " + m_textureIndexUsage[6] + ", " + m_textureIndexUsage[7] + ", " + m_textureIndexUsage[8]
                + ", " + m_textureIndexUsage[9] + ", " + m_textureIndexUsage[10] + ", " + m_textureIndexUsage[11]);
    }
}