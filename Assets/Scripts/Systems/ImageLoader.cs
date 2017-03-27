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

    // **************************
    // Public functions
    // *************************

    public void Start() // NOTE: Due to current underlying C++ implementation being single threaded, there can only be one of these
    {
        m_cppPlugin = new CppPlugin(this, kMaxNumTextures);

        m_textureIndexUsage = new int[kMaxNumTextures];
        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();
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

        /*
        using (WebClient webClient = new WebClient()) 
        {
            byte [] data = webClient
            using (var stream = new MemoryStream(data)) 
            {
                yield return LoadImageInternalPlugin(stream, sphereIndex, imageIdentifier);
                //m_coroutineQueue.EnqueueAction(LoadImageInternalPlugin(stream, sphereIndex, imageIdentifier));
            }
        }
        */

        m_staticLoadingIcon.SetActive(showLoading);

        HttpWebRequest http = (HttpWebRequest)WebRequest.Create(url);
        using (var imageStream = http.GetResponse().GetResponseStream())
        {
            int textureIndex = GetAvailableTextureIndex();
            yield return m_cppPlugin.LoadImageFromStreamIntoImageSphere(imageSphereController, sphereIndex, imageStream, imageIdentifier, textureIndex);
        }

        m_staticLoadingIcon.SetActive(false);
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