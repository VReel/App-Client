using UnityEngine;
using System.IO;                      // Stream
using System.Collections;             // IEnumerator
using System.Net;                     // HttpWebRequest

public class ImageLoader : MonoBehaviour 
{    
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private GameObject m_staticLoadingIcon;

    private const int kMaxNumTextures = 10; // 5 ImageSpheres + 1 Skybox + 4 spare textures
    private const int kLoadingTextureIndex = -1;
    private int[] m_textureIndexUsage;

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

    public void LoadImageFromPathIntoImageSphere(ImageSphereController imageSphereController, int sphereIndex, string filePathAndIdentifier, bool showLoading)
    {
        m_coroutineQueue.EnqueueAction(LoadImageFromPathIntoImageSphereInternal(imageSphereController, sphereIndex, filePathAndIdentifier, showLoading));
    }

    public void LoadImageFromURLIntoImageSphere(ImageSphereController imageSphereController, int sphereIndex, string url, string filePathAndIdentifier, bool showLoading)
    {
        m_coroutineQueue.EnqueueAction(LoadImageFromURLIntoImageSphereInternal(imageSphereController, sphereIndex, url, filePathAndIdentifier, showLoading));
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator LoadImageFromPathIntoImageSphereInternal(ImageSphereController imageSphereController, int sphereIndex, string filePathAndIdentifier, bool showLoading)
    {
        m_staticLoadingIcon.SetActive(showLoading);

        int textureIndex = GetAvailableTextureIndex();
        yield return m_cppPlugin.LoadImageFromPathIntoImageSphere(imageSphereController, sphereIndex, filePathAndIdentifier, textureIndex);

        m_staticLoadingIcon.SetActive(false);
    }

    private IEnumerator LoadImageFromURLIntoImageSphereInternal(ImageSphereController imageSphereController, int sphereIndex, string url, string imageIdentifier, bool showLoading)
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_staticLoadingIcon.SetActive(showLoading);

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Downloading image and getting stream through GetImageStreamFromURL() with url: " + url);
        yield return m_threadJob.WaitFor();
        Stream imageStream = null;
        m_threadJob.Start( () => 
            imageStream = GetImageStreamFromURL(url)
        );
        yield return m_threadJob.WaitFor();

        if (imageStream != null)
        {
            int textureIndex = GetAvailableTextureIndex();
            yield return m_cppPlugin.LoadImageFromStreamIntoImageSphere(imageSphereController, sphereIndex, imageStream, imageIdentifier, textureIndex);

            imageStream.Close();
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - WebRequest go a null stream for url: " + url);
        }
            
        m_staticLoadingIcon.SetActive(false);
    }        

    private Stream GetImageStreamFromURL(string url)
    {
        HttpWebRequest http = (HttpWebRequest)WebRequest.Create(url);
        Stream imageStream = http.GetResponse().GetResponseStream();
        return imageStream;
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
                + ", " + m_textureIndexUsage[9]); // + ", " + m_textureIndexUsage[10]);
    }
}