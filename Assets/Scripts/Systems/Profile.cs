using UnityEngine;
using UnityEngine.UI;               // Text
using System;                       // Datetime
using System.Collections;           // IEnumerator
using System.Collections.Generic;   // List
using System.IO;                    // Stream
using System.Net;                   // HttpWebRequest

public class Profile : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private GameObject m_errorMessage;
    [SerializeField] private GameObject m_staticLoadingIcon;
    [SerializeField] private GameObject m_newUserText;   

    public class Post
    {
        public string id { get; set; }
        public string thumbnailUrl { get; set; }
        public string originalUrl { get; set; }       
    }

    private List<Post> m_posts;
    private BackEndAPI m_backEndAPI;
    private int m_currPostIndex = -1;
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start() 
	{
        m_posts = new List<Post>();

        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        m_backEndAPI = new BackEndAPI(this, m_errorMessage, m_user);

        m_staticLoadingIcon.SetActive(false);
	}

    public void Update()
    {        
        if (m_appDirector.GetState() == AppDirector.AppState.kLogin)
        {
            if (m_user.IsLoggedIn())
            {
                m_appDirector.RequestProfileState();
            }
        }
        else if (m_appDirector.GetState() != AppDirector.AppState.kLogin)
        {
            if (!m_user.IsLoggedIn())
            {
                m_appDirector.RequestLoginState();
            }
        }
    }

    public void LogOut()
    {
        m_coroutineQueue.EnqueueAction(LogoutInternal());
    }
        
    public void InvalidatePostsLoading() // This function is called in order to stop any ongoing image loading 
    {        
        m_currPostIndex = -1;
        if (m_coroutineQueue != null)
        {
            m_coroutineQueue.Clear();
        }
    }
        
    public bool IsPostIndexAtStart()
    {
        return m_currPostIndex <= 0;
    }

    public bool IsPostIndexAtEnd()
    {
        int numImageSpheres = m_imageSphereController.GetNumSpheres();
        int numPosts = m_posts.Count; 
        return m_currPostIndex >= (numPosts - numImageSpheres);       
    }        

    public void OpenProfile()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenProfile() called");

        m_imageSphereController.SetAllImageSpheresToLoading();
        m_coroutineQueue.EnqueueAction(StoreAllPostsAndSetSpheres());
    }       

    public void NextImages()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: NextImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numPosts = m_posts.Count;

        m_currPostIndex = Mathf.Clamp(m_currPostIndex + numImagesToLoad, 0, numPosts);

        m_coroutineQueue.Clear(); // Ensure we stop loading something that we may be loading
        m_coroutineQueue.EnqueueAction(DownloadThumbnailsAndSetSpheres());
    }

    public void PreviousImages()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreviousImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numPosts = m_posts.Count;

        m_currPostIndex = Mathf.Clamp(m_currPostIndex - numImagesToLoad, 0, numPosts);

        m_coroutineQueue.Clear(); // Ensure we stop loading something that we may be loading
        m_coroutineQueue.EnqueueAction(DownloadThumbnailsAndSetSpheres());
    }

    public void DownloadOriginalImage(string imageIdentifier)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: DownloadOriginalImage() called");

        m_coroutineQueue.EnqueueAction(DownloadOriginalImageInternal(imageIdentifier));
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator LogoutInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LogOut() called");

        m_staticLoadingIcon.SetActive(true);

        yield return m_backEndAPI.Session_SignOut();

        m_staticLoadingIcon.SetActive(false);
    }

    // TODO: Handle users who have more than 20 posts!
    private IEnumerator StoreAllPostsAndSetSpheres()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Getting all Posts for Logged in User");

        m_posts.Clear();

        yield return m_backEndAPI.Posts_GetAll();

        VReelJSON.Model_Posts posts = m_backEndAPI.GetAllPostsResult();
        if (posts != null)
        {
            foreach (VReelJSON.PostsData postData in posts.data)
            {   
                Post newPost = new Post();
                newPost.id = postData.id.ToString();
                newPost.thumbnailUrl = postData.attributes.thumbnail_url.ToString();
                m_posts.Add(newPost);
            }
        }           

        m_currPostIndex = 0; // set to a valid Index
        m_coroutineQueue.EnqueueAction(DownloadThumbnailsAndSetSpheres());

        bool noImagesUploaded = m_posts.Count <= 0;
        m_newUserText.SetActive(noImagesUploaded); // If the user has yet to upload any images then show them the New User Text!
    }

    private IEnumerator DownloadThumbnailsAndSetSpheres()
    {
        yield return m_appDirector.VerifyInternetConnection();

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        DownloadThumbnailsAndSetSpheresInternal(m_currPostIndex, numImagesToLoad);
    }

    private void DownloadThumbnailsAndSetSpheresInternal(int startingPostIndex, int numImages)
    {
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Downloading {0} images beginning at index {1}. We've found {2} posts for the user!", numImages, startingPostIndex, m_posts.Count));

        Resources.UnloadUnusedAssets();

        int currPostIndex = startingPostIndex;
        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, currPostIndex++)
        {
            if (currPostIndex < m_posts.Count)
            {                   
                string id = m_posts[currPostIndex].id;
                string thumbnailURL = m_posts[currPostIndex].thumbnailUrl;
                m_coroutineQueue.EnqueueAction(DownloadImageInternal(id, thumbnailURL, sphereIndex)); //, currPostIndex, numImages));
            }
            else
            {
                m_imageSphereController.HideSphereAtIndex(sphereIndex);
            }
        }

        Resources.UnloadUnusedAssets();
    }

    public IEnumerator DownloadOriginalImageInternal(string id)
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_staticLoadingIcon.SetActive(true);

        yield return RefreshPostData(id);

        yield return DownloadImageInternal(id, m_posts[ConvertIdToIndex(id)].originalUrl, -1); // a -1 sphereIndex maps to the SkyBox

        m_staticLoadingIcon.SetActive(false);   
    }

    private IEnumerator DownloadImageInternal(string imageIdentifier, string url, int sphereIndex) // int thisThumbnailURLIndex, int numImages) - these were once used for validity checks
    {
        yield return m_appDirector.VerifyInternetConnection();
                   
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Downloading: " + imageIdentifier));

        HttpWebRequest http = (HttpWebRequest)WebRequest.Create(url);
        m_coroutineQueue.EnqueueAction(LoadImageInternalPlugin(http.GetResponse(), sphereIndex, imageIdentifier));

        /*
        using (WebClient webClient = new WebClient()) 
        {
            byte [] data = webClient.DownloadData(url);
            using (MemoryStream mem = new MemoryStream(data)) 
            {
                using (var yourImage = Image.FromStream(mem)) 
                { 
                }
            }
        }
        */
    }

    private IEnumerator RefreshPostData(string id) // NOTE: Since URL's have a lifetime, we need to refresh the data at certain points...
    {            
        yield return m_backEndAPI.Posts_Get(id);

        int index = ConvertIdToIndex(id);
        if (index <= m_posts.Count)
        {
            m_posts[index].thumbnailUrl = m_backEndAPI.GetPostResult().data.attributes.thumbnail_url;
            m_posts[index].originalUrl = m_backEndAPI.GetPostResult().data.attributes.original_url;
        }
    }

    private IEnumerator LoadImageInternalPlugin(WebResponse response, int sphereIndex, string imageIdentifier)
    {        
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LoadImageInternal for " + imageIdentifier);

        using (var stream = response.GetResponseStream())
        {
            yield return m_imageSphereController.LoadImageFromStreamIntoImageSphere(stream, sphereIndex, imageIdentifier);
        }
    }
        
    private IEnumerator LoadImageInternalUnity(WebResponse response, int sphereIndex, string imageIdentifier)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: ConvertStreamAndSetImage for " + imageIdentifier);

        const int kNumIterationsPerFrame = 150;
        byte[] myBinary = null;
        using (var stream = response.GetResponseStream())
        {            
            using( MemoryStream ms = new MemoryStream() )
            {
                int iterations = 0;
                int byteCount = 0;
                do
                {
                    byte[] buf = new byte[1024];
                    byteCount = stream.Read(buf, 0, 1024);
                    ms.Write(buf, 0, byteCount);
                    iterations++;
                    if (iterations % kNumIterationsPerFrame == 0)
                    {                        
                        yield return new WaitForEndOfFrame();
                    }
                } 
                while(stream.CanRead && byteCount > 0);

                myBinary = ms.ToArray();
            }
        }

        // The following is generally coming out to around 6-7MB in size...
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished iterating, length of byte[] is " + myBinary.Length);

        Texture2D newImage = new Texture2D(2,2); 
        newImage.LoadImage(myBinary);
        m_imageSphereController.SetImageAtIndex(sphereIndex, newImage, imageIdentifier, -1);
        yield return new WaitForEndOfFrame();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished Setting Image!");

        Resources.UnloadUnusedAssets();
    }

    private int ConvertIdToIndex(string id) //TODO: To remove this all I need to do is turn m_posts into a Map<ID, PostAttributes>...
    {
        int index = 0;    
        for (; index < m_posts.Count; index++)
        {   
            if (m_posts[index].id.CompareTo(id) == 0)
            {
                break;
            }
        }

        if (index >= m_posts.Count)
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - Invalid ID being converted to Post Index!! -> " + id);
            index = 0;
        }

        return index;
    }
}