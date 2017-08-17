using UnityEngine;
using UnityEngine.UI;               // Text
using System.Collections;           // IEnumerator
using System.Collections.Generic;   // List

public class Posts : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private Profile m_profile;
    [SerializeField] private ImageLoader m_imageLoader;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private LoadingIcon m_loadingIcon;

    public class Post
    {
        public string postId { get; set; }
        public string thumbnailUrl { get; set; }
        public string caption { get; set; }
        public int likeCount { get; set; }
        public int commentCount { get; set; }
        public string createdAt { get; set; }
        public bool edited { get; set; }
        public bool likedByMe { get; set; }
        public string originalUrl { get; set; }
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
    private string m_currUserOrTagId;

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

    /*
    public string GetCurrUserOrTagID()
    {
        return m_currUserOrTagId;
    }
    */
        
    public bool IsProfileType()
    {
        return (m_postsType == PostsType.kUserProfile || m_postsType == PostsType.kOtherProfile);
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

    public bool IsValidRequest(int postImageIndex)
    {
        int numImageSpheres = m_imageSphereController.GetNumSpheres();
        bool imageRequestStillValid = 
            (m_currPostIndex != -1) && 
            (m_currPostIndex <= postImageIndex) &&  
            (postImageIndex < m_currPostIndex + numImageSpheres); // Request no longer valid as user has moved on from this page

        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Checking PostIndex validity returned '{0}' when checking that {1} <= {2} < {1}+{3}", 
            imageRequestStillValid, m_currPostIndex, postImageIndex, numImageSpheres));

        return imageRequestStillValid;
    }

    public void OpenPublicTimeline()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenPublicTimeline() called");

        m_postsType = PostsType.kPublicTimeline;
        m_currUserOrTagId = "";
        OpenPosts();
    }    

    public void OpenPersonalTimeline()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenPersonalTimeline() called");

        m_postsType = PostsType.kPersonalTimeline;
        m_currUserOrTagId = "";
        OpenPosts();
    }    

    public void OpenUserProfile()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenUserProfile() called");

        m_postsType = PostsType.kUserProfile;
        m_currUserOrTagId = m_user.m_id;
        OpenPosts();

        m_profile.OpenProfileDetails(m_currUserOrTagId);
    }    

    public void OpenProfileWithID(string userID, string userHandle)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenProfileWithID() called with Handle: " + userHandle + ", and ID: " + userID);

        m_postsType = PostsType.kOtherProfile;
        m_currUserOrTagId = userID;
        OpenPosts();

        m_profile.OpenProfileDetails(m_currUserOrTagId);
    } 

    public void OpenHashTag(string hashTagID, string hashTag)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenHashTag() called with HashTag: " + hashTag + ", and ID: " + hashTagID);

        m_postsType = PostsType.kHashTag;
        m_currUserOrTagId = hashTagID;
        OpenPosts();
    } 

    /*
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
    */

    public void NextPost(int imageSphereIndex)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: NextPost() called with index: " + imageSphereIndex);

        int numImagesToLoad = 1;
        int numSpheres = m_imageSphereController.GetNumSpheres();
        int numPosts = m_posts.Count;

        m_currPostIndex = Mathf.Clamp(m_currPostIndex + numImagesToLoad, 0, numPosts);

        //m_imageLoader.InvalidateLoading(); // Stop anything we may have already been loading
        //m_coroutineQueue.Clear(); // Ensures we don't repeat operations

        if (m_nextPageOfPosts != null)
        { // By calling this every time a user presses the next button, we ensure he can never miss out on posts and don't overload the API            
            m_coroutineQueue.EnqueueAction(StorePostsFromNextPage());
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: CurrPostIndex: " + m_currPostIndex);

        int postIndex = m_currPostIndex + (numSpheres-1);
        m_coroutineQueue.EnqueueAction(RefreshPostsAtIndex(postIndex));
        m_coroutineQueue.EnqueueAction(DownloadThumbnailAndSetSphere(imageSphereIndex, postIndex));
    }

    /*
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
    */

    public void PreviousPost(int imageSphereIndex)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreviousPost() called with index: " + imageSphereIndex);

        int numImagesToLoad = 1;
        int numPosts = m_posts.Count;

        m_currPostIndex = Mathf.Clamp(m_currPostIndex - numImagesToLoad, 0, numPosts);

        //m_imageLoader.InvalidateLoading(); // Stop anything we may have already been loading
        //m_coroutineQueue.Clear(); // Ensures we don't repeat operations

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: CurrPostIndex: " + m_currPostIndex);

        int postIndex = m_currPostIndex;
        m_coroutineQueue.EnqueueAction(RefreshPostsAtIndex(postIndex));
        m_coroutineQueue.EnqueueAction(DownloadThumbnailAndSetSphere(imageSphereIndex, postIndex));
    }

    public void LikeOrUnlikePost(string postId, bool doLike)
    {
        m_coroutineQueue.EnqueueAction(LikeOrUnlikePostInternal(postId, doLike));
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

    public Post GetPostFromID(string postId)
    {
        int index = ConvertIdToIndex(postId);
        return m_posts[index];
    }

    public void RefreshPostData(string postId)
    {
        m_coroutineQueue.EnqueueAction(RefreshPostDataInternal(postId, true));
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: StoreFirstPostsAndSetSpheres() called for PostType: " + m_postsType.ToString());

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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: StorePostsFromNextPage() for page: " + m_nextPageOfPosts + ", for PostType: " + m_postsType.ToString());       

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
            yield return m_backEndAPI.User_GetUserPosts(m_currUserOrTagId, nextPageOfPosts);
        }
        else if (m_postsType == PostsType.kHashTag)
        {            
            yield return m_backEndAPI.HashTag_GetHashTagPosts(m_currUserOrTagId, nextPageOfPosts);
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
            foreach (VReelJSON.PostData postData in posts.data)
            {   
                Post newPost = new Post();
                newPost.postId = postData.id.ToString();
                newPost.thumbnailUrl = postData.attributes.thumbnail_url.ToString();
                newPost.caption = postData.attributes.caption.ToString();
                newPost.likeCount = postData.attributes.like_count;
                newPost.commentCount = postData.attributes.comment_count;
                newPost.createdAt = postData.attributes.created_at.ToString();
                newPost.edited = postData.attributes.edited;
                newPost.likedByMe = postData.attributes.liked_by_me;
                newPost.userId = postData.relationships.user.data.id.ToString();
                newPost.userHandle = Helper.GetHandleFromIDAndUserData(posts.included, newPost.userId);

                m_posts.Add(newPost);
            }

            m_nextPageOfPosts = null;
            if (posts.meta.next_page)
            {
                m_nextPageOfPosts = posts.meta.next_page_id;
            }
        }
    }

    private IEnumerator LikeOrUnlikePostInternal(string postId, bool doLike)
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (doLike)
        {
            yield return m_backEndAPI.Like_LikePost(postId);
        }
        else
        {
            yield return m_backEndAPI.Like_UnlikePost(postId);
        }
    }
        
    private IEnumerator DownloadThumbnailsAndSetSpheres()
    {
        yield return m_appDirector.VerifyInternetConnection();

        int postIndex = m_currPostIndex;
        int numImages = m_imageSphereController.GetNumSpheres();
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Downloading {0} images beginning at index {1}. We've found {2} posts!", numImages, postIndex, m_posts.Count));

        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, postIndex++)
        {
            if (postIndex < m_posts.Count)
            {                   
                bool showLoading = sphereIndex == 0; // The first one in the profile should do some loading to let the user know things are happening

                LoadImageInternalPlugin(
                    m_posts[postIndex].thumbnailUrl, 
                    sphereIndex,
                    postIndex,
                    m_posts[postIndex].postId, 
                    showLoading
                );

                m_imageSphereController.SetMetadataAtIndex(
                    sphereIndex, 
                    m_posts[postIndex].userId, 
                    m_posts[postIndex].userHandle, 
                    m_posts[postIndex].caption, 
                    m_posts[postIndex].commentCount, 
                    m_posts[postIndex].likeCount,
                    m_posts[postIndex].likedByMe,
                    false
                );
            }
            else
            {
                m_imageSphereController.HideSphereAtIndex(sphereIndex);
            }
        }
    }

    private IEnumerator DownloadThumbnailAndSetSphere(int sphereIndex, int postIndex)
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Downloading 1 image beginning at index {0}. We've found {1} posts!", postIndex, m_posts.Count));

        if (postIndex < m_posts.Count)
        {                   
            bool showLoading = false;

            LoadImageInternalPlugin(
                m_posts[postIndex].thumbnailUrl, 
                sphereIndex,
                postIndex,
                m_posts[postIndex].postId, 
                showLoading
            );

            m_imageSphereController.SetMetadataAtIndex(
                sphereIndex, 
                m_posts[postIndex].userId, 
                m_posts[postIndex].userHandle, 
                m_posts[postIndex].caption, 
                m_posts[postIndex].commentCount, 
                m_posts[postIndex].likeCount,
                m_posts[postIndex].likedByMe,
                false
            );
        }
        else
        {
            m_imageSphereController.HideSphereAtIndex(sphereIndex);
        }
    }

    public IEnumerator DownloadOriginalImageInternal(string postId)
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_loadingIcon.Display();

        yield return RefreshPostDataInternal(postId);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            int index = ConvertIdToIndex(postId); 
            int sphereIndex = Helper.kSkyboxSphereIndex;
            bool showLoading = true;

            LoadImageInternalPlugin(
                m_posts[index].originalUrl, 
                sphereIndex, 
                Helper.kIgnoreImageIndex,
                postId, 
                showLoading
            );
        }

        m_loadingIcon.Hide();
    }

    /*
    private IEnumerator RefreshPostsAtCurrIndex()
    {
        yield return m_appDirector.VerifyInternetConnection();

        int startingPostIndex = m_currPostIndex;
        int numImages = m_imageSphereController.GetNumSpheres();
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Refreshing {0} posts beginning at index {1}. We've found {2} posts!", numImages, startingPostIndex, m_posts.Count));

        int postIndex = startingPostIndex;
        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, postIndex++)
        {
            if (postIndex < m_posts.Count)
            {                   
                yield return RefreshPostDataInternal(m_posts[postIndex].postId);
            }
        }
    }
    */

    private IEnumerator RefreshPostsAtIndex(int postIndex)
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Refreshing post at index {0}!", postIndex));

        if (postIndex < m_posts.Count)
        {                   
            yield return RefreshPostDataInternal(m_posts[postIndex].postId);
        }           
    }

    private IEnumerator RefreshPostDataInternal(string postId, bool updateMetadata = false) // NOTE: Since URL's have a lifetime, we need to refresh the data at certain points...
    {           
        yield return m_appDirector.VerifyInternetConnection();

        yield return m_backEndAPI.Post_GetPost(postId);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            int index = ConvertIdToIndex(postId);
            if (index <= m_posts.Count)
            {
                m_posts[index].thumbnailUrl = m_backEndAPI.GetPostResult().data.attributes.thumbnail_url;
                m_posts[index].caption = m_backEndAPI.GetPostResult().data.attributes.caption.ToString();
                m_posts[index].likeCount = m_backEndAPI.GetPostResult().data.attributes.like_count;
                m_posts[index].commentCount = m_backEndAPI.GetPostResult().data.attributes.comment_count;
                m_posts[index].createdAt = m_backEndAPI.GetPostResult().data.attributes.created_at.ToString();
                m_posts[index].edited = m_backEndAPI.GetPostResult().data.attributes.edited;
                m_posts[index].likedByMe = m_backEndAPI.GetPostResult().data.attributes.liked_by_me;
                m_posts[index].originalUrl = m_backEndAPI.GetPostResult().data.attributes.original_url;

                int sphereIndex = index - m_currPostIndex;
                m_imageSphereController.SetMetadataAtIndex(
                    sphereIndex, 
                    m_posts[index].userId, 
                    m_posts[index].userHandle, 
                    m_posts[index].caption, 
                    m_posts[index].commentCount, 
                    m_posts[index].likeCount,
                    m_posts[index].likedByMe,
                    updateMetadata
                );
            }
        }
    }        

    private void LoadImageInternalPlugin(string url, int sphereIndex, int postIndex, string imageIdentifier, bool showLoading)
    {        
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LoadImageInternal for " + imageIdentifier);

        m_imageLoader.LoadImageFromURLIntoImageSphere(m_imageSphereController, sphereIndex, postIndex, url, imageIdentifier, showLoading);
    }           

    //TODO: To remove this all I need to do is turn m_posts into a Map<ID, PostAttributes>...
    private int ConvertIdToIndex(string postId) 
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