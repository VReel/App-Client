using UnityEngine;
using System.Collections;           // IEnumerator
using System.Collections.Generic;   // List

//using System.Net;                   // HttpWebRequest -- only used in old function LoadImageInternalUnity()

public class Posts : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private ImageLoader m_imageLoader;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private LoadingIcon m_loadingIcon;

    public class Post
    {
        public string postId { get; set; }
        public string thumbnailUrl { get; set; }
        public string originalUrl { get; set; }
        public int likeCount { get; set; }
        public string caption { get; set; }
        public bool edited { get; set; }
        public string createdAt { get; set; }
        public string userId { get; set; }
        public string userHandle { get; set; }
    }

    public enum PostsType
    {
        kPublicTimeline,
        kPersonalTimeline,
        kUserProfile,
        kOtherProfile,
        kHashTag
    };

    private PostsType m_postsType;
    private string m_currIdOrTag;

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
        
    public int GetNumPosts()
    {
        return m_posts.Count;
    }

    public void InvalidateWork() // This function is called in order to stop any ongoing work
    {    
        m_imageLoader.InvalidateLoading();    
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

    public void OpenPublicTimeline()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenPublicTimeline() called");

        m_postsType = PostsType.kPublicTimeline;
        m_currIdOrTag = "";
        OpenPosts();
    }    

    public void OpenPersonalTimeline()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenPersonalTimeline() called");

        m_postsType = PostsType.kPersonalTimeline;
        m_currIdOrTag = "";
        OpenPosts();
    }    

    public void OpenUserProfile()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenUserProfile() called");

        m_postsType = PostsType.kUserProfile;
        m_currIdOrTag = "";
        OpenPosts();
    }    

    public void OpenProfileWithID(string userID)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenProfileWithID() called with ID: " + userID);

        m_postsType = PostsType.kOtherProfile;
        m_currIdOrTag = userID;
        OpenPosts();
    } 

    public void OpenHashTag(string hashTag)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenHashTag() called with ID: " + hashTag);

        m_postsType = PostsType.kHashTag;
        m_currIdOrTag = hashTag;
        OpenPosts();
    } 

    public void NextPosts()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: NextPosts() called");

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

    public void PreviousPosts()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreviousPosts() called");

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

    public void RequestPostRemoval(string postId) 
    {
        // Lets Posts decide whether it should remove the post 
        //  -> for now we always remove the post, but really we should be checking that post no longer exists...
        m_posts.RemoveAt(ConvertIdToIndex(postId));
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void OpenPosts()
    {
        m_imageSphereController.SetAllImageSpheresToLoading();
        m_coroutineQueue.EnqueueAction(StoreFirstPostsAndSetSpheres());
    }

    private IEnumerator StoreFirstPostsAndSetSpheres()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Getting First set of Posts for Logged in User");

        m_loadingIcon.Display(); //NOTE: This should stop the following operation from ever being cut half-way through

        m_posts.Clear();

        yield return GetPosts();

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            StoreNewPosts();
        }

        m_currPostIndex = 0; // set to a valid Index
        m_coroutineQueue.EnqueueAction(DownloadThumbnailsAndSetSpheres());

        m_loadingIcon.Hide();
    }

    private IEnumerator StorePostsFromNextPage()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Getting Posts for Logged in User from page: " + m_nextPageOfPosts);       

        m_loadingIcon.Display(); //NOTE: This should stop the following operation from ever being cut half-way through

        yield return GetPosts(m_nextPageOfPosts);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            StoreNewPosts();
        }

        m_loadingIcon.Hide();
    }

    private IEnumerator GetPosts(string nextPageOfPosts = "")
    {
        if (m_postsType == PostsType.kPublicTimeline)
        {
            yield return m_backEndAPI.Timeline_GetPublicTimeline(nextPageOfPosts);
        }
        else if (m_postsType == PostsType.kPersonalTimeline)
        {
            yield return m_backEndAPI.Timeline_GetPersonalTimeline(nextPageOfPosts);
        }
        else if (m_postsType == PostsType.kOtherProfile)
        {
            yield return m_backEndAPI.User_GetUserPosts(m_currIdOrTag, nextPageOfPosts);
        }
        else if (m_postsType == PostsType.kHashTag)
        {            
            yield return m_backEndAPI.HashTag_GetHashTagPosts(m_currIdOrTag, nextPageOfPosts);
        }
        else if (m_postsType == PostsType.kUserProfile)
        {
            yield return m_backEndAPI.Post_GetPosts(nextPageOfPosts);
        }
    }

    private void StoreNewPosts()
    {
        VReelJSON.Model_Posts posts = m_backEndAPI.GetPostsResult();
        if (posts != null)
        {
            foreach (VReelJSON.PostsData postData in posts.data)
            {   
                Post newPost = new Post();
                newPost.postId = postData.id.ToString();
                newPost.thumbnailUrl = postData.attributes.thumbnail_url.ToString();
                newPost.likeCount = postData.attributes.like_count;
                newPost.caption = postData.attributes.caption.ToString();
                newPost.edited = postData.attributes.edited;
                newPost.createdAt = postData.attributes.created_at.ToString();
                newPost.userId = postData.relationships.user.data.id.ToString();
                newPost.userHandle = GetHandleFromIDAndPostData(ref posts, newPost.userId);

                m_posts.Add(newPost);
            }

            m_nextPageOfPosts = null;
            if (posts.meta.next_page) // Handle users with over 20 posts - if we have another page, then loop back around... 
            {
                m_nextPageOfPosts = posts.meta.next_page_id;
            }
        }
    }

    private string GetHandleFromIDAndPostData(ref VReelJSON.Model_Posts posts, string userId)
    {
        for (int i = 0; i < (posts.included).Count; i++)
        {
            if ((posts.included[i]).id.CompareTo(userId) == 0)
            {
                return (posts.included[i]).attributes.handle;
            }
        }

        return "HANDLE_ERROR";
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
                bool showLoading = sphereIndex == 0; // The first one in the profile should do some loading to let the user know things are happening

                LoadImageInternalPlugin(
                    m_posts[postIndex].thumbnailUrl, 
                    sphereIndex, 
                    m_posts[postIndex].postId, 
                    showLoading
                );

                m_imageSphereController.SetMetadataAtIndex(
                    sphereIndex, 
                    (m_postsType == PostsType.kUserProfile) ? "" : m_posts[postIndex].userHandle, 
                    m_posts[postIndex].caption, 
                    m_posts[postIndex].likeCount
                );
            }
            else
            {
                m_imageSphereController.HideSphereAtIndex(sphereIndex);
            }
        }
    }

    public IEnumerator DownloadOriginalImageInternal(string postId)
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_loadingIcon.Display();

        yield return RefreshPostData(postId);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            int index = ConvertIdToIndex(postId); 
            int sphereIndex = -1; // Set it on the skybox
            bool showLoading = true;

            LoadImageInternalPlugin(
                m_posts[index].originalUrl, 
                sphereIndex, 
                postId, 
                showLoading
            );
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
                yield return RefreshPostData(m_posts[postIndex].postId);
            }
        }
    }

    private IEnumerator RefreshPostData(string postId) // NOTE: Since URL's have a lifetime, we need to refresh the data at certain points...
    {            
        yield return m_backEndAPI.Post_GetPost(postId);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            int index = ConvertIdToIndex(postId);
            if (index <= m_posts.Count)
            {
                m_posts[index].thumbnailUrl = m_backEndAPI.GetPostResult().data.attributes.thumbnail_url;
                m_posts[index].caption = m_backEndAPI.GetPostResult().data.attributes.caption.ToString();
                m_posts[index].likeCount = m_backEndAPI.GetPostResult().data.attributes.like_count;
                m_posts[index].createdAt = m_backEndAPI.GetPostResult().data.attributes.created_at.ToString();
                m_posts[index].edited = m_backEndAPI.GetPostResult().data.attributes.edited;
                m_posts[index].originalUrl = m_backEndAPI.GetPostResult().data.attributes.original_url;
            }
        }
    }

    private void LoadImageInternalPlugin(string url, int sphereIndex, string imageIdentifier, bool showLoading)
    {        
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LoadImageInternal for " + imageIdentifier);

        m_imageLoader.LoadImageFromURLIntoImageSphere(m_imageSphereController, sphereIndex, url, imageIdentifier, showLoading);
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

    private int ConvertIdToIndex(string postId) //TODO: To remove this all I need to do is turn m_posts into a Map<ID, PostAttributes>...
    {
        int index = 0;    
        for (; index < m_posts.Count; index++)
        {   
            if ((m_posts[index].postId).CompareTo(postId) == 0)
            {
                break;
            }
        }

        if (index >= m_posts.Count)
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - Invalid ID being converted to Post Index!! -> " + postId);
            index = 0;
        }

        return index;
    }
}