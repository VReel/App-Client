using UnityEngine;
using UnityEngine.UI;               // Text
using System;                       // Datetime
using System.Collections;           // IEnumerator
using System.Collections.Generic;   // List
using System.IO;                    // Stream

//using System.Net;                   // HttpWebRequest -- only used in old function LoadImageInternalUnity()

public class Profile : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private ImageLoader m_imageLoader;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private LoadingIcon m_loadingIcon;
    [SerializeField] private GameObject m_newUserText;   
    [SerializeField] private GameObject m_confirmDeleteButton;
    [SerializeField] private GameObject m_cancelDeleteButton;

    public class Post
    {
        public string id { get; set; }
        public string thumbnailUrl { get; set; }
        public string originalUrl { get; set; }
        public string caption { get; set; }
    }

    private List<Post> m_posts;
    private string m_nextPageOfPosts = null;
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

        m_backEndAPI = new BackEndAPI(this, m_user.GetErrorMessage(), m_user);
	}              

    public void PreDelete()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreDelete() called on post: " + m_imageSkybox.GetImageIdentifier());

        m_confirmDeleteButton.SetActive(true);
        m_cancelDeleteButton.SetActive(true);

        Text userTextComponent = m_user.GetUserMessage().GetComponentInChildren<Text>();
        userTextComponent.text = "Definitely want to delete this post? =(";
        userTextComponent.color = Color.red;
    }

    public void CancelDelete()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: CancelDelete() called");

        m_confirmDeleteButton.SetActive(false);
        m_cancelDeleteButton.SetActive(false);

        Text userTextComponent = m_user.GetUserMessage().GetComponentInChildren<Text>();
        userTextComponent.text = "Delete Cancelled =)";
        userTextComponent.color = Color.black;
    }

    public void Delete()
    {
        m_confirmDeleteButton.SetActive(false);
        m_cancelDeleteButton.SetActive(false);

        m_coroutineQueue.EnqueueAction(DeletePostInternal(m_imageSkybox.GetImageIdentifier()));
    }

    public void ShowProfileText()
    {
        m_coroutineQueue.EnqueueAction(ShowProfileTextInternal());
    }
        
    public void InvalidateWork() // This function is called in order to stop any ongoing work
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
        m_coroutineQueue.EnqueueAction(StoreFirstPostsAndSetSpheres());
    }       

    public void NextImages()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: NextImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numPosts = m_posts.Count;

        m_currPostIndex = Mathf.Clamp(m_currPostIndex + numImagesToLoad, 0, numPosts);

        m_imageLoader.InvalidateLoading(); // Stop anything we may have already been loading
        m_coroutineQueue.Clear(); // Ensures we don't repeat operations

        if (m_nextPageOfPosts != null)
        { // By calling this every time a user presses the next button, we ensure he can never miss out on posts and don't overload the API            
            m_coroutineQueue.EnqueueAction(StorePostsFromNextPage());
        }

        m_coroutineQueue.EnqueueAction(RefreshPostsAtCurrIndex());
        m_coroutineQueue.EnqueueAction(DownloadThumbnailsAndSetSpheres());
    }

    public void PreviousImages()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreviousImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numPosts = m_posts.Count;

        m_currPostIndex = Mathf.Clamp(m_currPostIndex - numImagesToLoad, 0, numPosts);

        m_imageLoader.InvalidateLoading(); // Stop anything we may have already been loading
        m_coroutineQueue.Clear(); // Ensures we don't repeat operations
        m_coroutineQueue.EnqueueAction(RefreshPostsAtCurrIndex());
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

    private IEnumerator DeletePostInternal(string id)
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Delete() called on post: " + id);

        m_loadingIcon.Display();

        yield return m_backEndAPI.Posts_DeletePost(id);

        Text userTextComponent = m_user.GetUserMessage().GetComponentInChildren<Text>();
        if (m_backEndAPI.IsLastAPICallSuccessful())
        {            
            // Report Success in Profile
            userTextComponent.text = "Post Deleted Successfully! =)";
            userTextComponent.color = Color.black;

            m_posts.RemoveAt(ConvertIdToIndex(id));
        }
        else
        {
            // Report Failure in Profile
            userTextComponent.text = "Deleting failed =(\n Please try again!";
            userTextComponent.color = Color.red;
        }

        m_loadingIcon.Hide();
    }

    private IEnumerator ShowProfileTextInternal()
    {
        while (!m_user.IsLoggedIn() || !m_user.IsUserDataStored())
        {
            yield return null;
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Setting Profile Text!");
        Text userTextComponent = m_user.GetUserMessage().GetComponentInChildren<Text>();
        userTextComponent.text = "Hi " + m_user.m_handle + "! =D";
        userTextComponent.color = Color.black;
    }
      
    private IEnumerator StoreFirstPostsAndSetSpheres()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Getting First set of Posts for Logged in User");

        m_loadingIcon.Display(); //NOTE: This should stop the following operation from ever being cut half-way through

        m_posts.Clear();

        yield return m_backEndAPI.Posts_GetMyPage();

        VReelJSON.Model_Posts posts = m_backEndAPI.GetPostsResult();
        if (posts != null)
        {
            foreach (VReelJSON.PostsData postData in posts.data)
            {   
                Post newPost = new Post();
                newPost.id = postData.id.ToString();
                newPost.thumbnailUrl = postData.attributes.thumbnail_url.ToString();
                newPost.caption = postData.attributes.caption.ToString();
                m_posts.Add(newPost);
            }         

            m_nextPageOfPosts = null;
            if (posts.meta.next_page) // Handle users with over 20 posts - if we have another page, then loop back around... 
            {
                m_nextPageOfPosts = posts.meta.next_page_id;
            }
        }

        m_currPostIndex = 0; // set to a valid Index
        m_coroutineQueue.EnqueueAction(DownloadThumbnailsAndSetSpheres());

        bool noImagesUploaded = m_posts.Count <= 0;
        m_newUserText.SetActive(noImagesUploaded); // If the user has yet to upload any images then show them the New User Text!

        m_loadingIcon.Hide();
    }

    private IEnumerator StorePostsFromNextPage()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Getting Posts for Logged in User from page: " + m_nextPageOfPosts);       

        m_loadingIcon.Display(); //NOTE: This should stop the following operation from ever being cut half-way through

        yield return m_backEndAPI.Posts_GetMyPage(m_nextPageOfPosts);

        VReelJSON.Model_Posts posts = m_backEndAPI.GetPostsResult();
        if (posts != null)
        {
            foreach (VReelJSON.PostsData postData in posts.data)
            {   
                Post newPost = new Post();
                newPost.id = postData.id.ToString();
                newPost.thumbnailUrl = postData.attributes.thumbnail_url.ToString();
                newPost.caption = postData.attributes.caption.ToString();
                m_posts.Add(newPost);
            }

            m_nextPageOfPosts = null;
            if (posts.meta.next_page) // Handle users with over 20 posts - if we have another page, then loop back around... 
            {
                m_nextPageOfPosts = posts.meta.next_page_id;
            }
        }

        m_loadingIcon.Hide();
    }

    private IEnumerator DownloadThumbnailsAndSetSpheres()
    {
        yield return m_appDirector.VerifyInternetConnection();

        int startingPostIndex = m_currPostIndex;
        int numImages = m_imageSphereController.GetNumSpheres();
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Downloading {0} images beginning at index {1}. We've found {2} posts for the user!", numImages, startingPostIndex, m_posts.Count));

        int postIndex = startingPostIndex;
        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, postIndex++)
        {
            if (postIndex < m_posts.Count)
            {                   
                string id = m_posts[postIndex].id;
                string thumbnailURL = m_posts[postIndex].thumbnailUrl;
                string captionText = m_posts[postIndex].caption;

                bool showLoading = sphereIndex == 0; // The first one in the profile should do some loading to let the user know things are happening
                LoadImageInternalPlugin(thumbnailURL, sphereIndex, id, showLoading);
                m_imageSphereController.SetMetadataAtIndex(sphereIndex, "", captionText, -1);
            }
            else
            {
                m_imageSphereController.HideSphereAtIndex(sphereIndex);
            }
        }
    }

    public IEnumerator DownloadOriginalImageInternal(string id)
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_loadingIcon.Display();

        yield return RefreshPostData(id);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            int sphereIndex = ConvertIdToIndex(id);
            string originalURL = m_posts[sphereIndex].originalUrl;
            string captionText = m_posts[sphereIndex].caption;

            bool showLoading = true;
            LoadImageInternalPlugin(originalURL, -1, id, showLoading); // a -1 sphereIndex maps to the SkyBox
            m_imageSphereController.SetMetadataAtIndex(sphereIndex, "", captionText, -1);
        }

        m_loadingIcon.Hide();
    }

    private IEnumerator RefreshPostsAtCurrIndex()
    {
        yield return m_appDirector.VerifyInternetConnection();

        int startingPostIndex = m_currPostIndex;
        int numImages = m_imageSphereController.GetNumSpheres();
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Refreshing {0} posts beginning at index {1}. We've found {2} posts for the user!", numImages, startingPostIndex, m_posts.Count));

        int postIndex = startingPostIndex;
        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, postIndex++)
        {
            if (postIndex < m_posts.Count)
            {                   
                yield return RefreshPostData(m_posts[postIndex].id);
            }
        }
    }

    private IEnumerator RefreshPostData(string id) // NOTE: Since URL's have a lifetime, we need to refresh the data at certain points...
    {            
        yield return m_backEndAPI.Posts_GetPost(id);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            int index = ConvertIdToIndex(id);
            if (index <= m_posts.Count)
            {
                m_posts[index].thumbnailUrl = m_backEndAPI.GetPostResult().data.attributes.thumbnail_url;
                m_posts[index].originalUrl = m_backEndAPI.GetPostResult().data.attributes.original_url;
                m_posts[index].caption = m_backEndAPI.GetPostResult().data.attributes.caption.ToString();
            }
        }
    }

    private void LoadImageInternalPlugin(string url, int sphereIndex, string imageIdentifier, bool showLoading)
    {        
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LoadImageInternal for " + imageIdentifier);

        m_imageLoader.LoadImageFromURLIntoImageSphere(m_imageSphereController, sphereIndex, url, imageIdentifier, showLoading);

        /*
        using (var stream = response.GetResponseStream())
        {
            yield return m_imageSphereController.LoadImageFromStreamIntoImageSphere(stream, sphereIndex, imageIdentifier);
        }
        */
    }
        
    /*
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
                        yield return null;
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
        m_imageSphereController.SetImageAtIndex(sphereIndex, newImage, imageIdentifier, -1 , true);
        yield return null;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished Setting Image!");

        Resources.UnloadUnusedAssets();
    }
    */

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