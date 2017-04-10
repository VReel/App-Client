using UnityEngine;
using UnityEngine.UI;                 // Text
using System;                         // StringComparer
using System.Collections;             // IEnumerator
using System.Globalization;           // CompareOptions
using System.Text.RegularExpressions; // Regex

//using System;                       // Datetime
//using System.Collections.Generic;   // List
//using System.IO;                    // Stream

//using System.Net;                   // HttpWebRequest -- only used in old function LoadImageInternalUnity()

public class Search : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    public enum SearchState
    {
        kNone = -1,     // This should only be the state at the start when opening the Search menu
        kUser =  0,     // Searching for a User
        kTag =  1       // Searching for a HashTag
    }

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private ImageLoader m_imageLoader;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private LoadingIcon m_loadingIcon;
    [SerializeField] private GameObject[] m_searchTypes;
    [SerializeField] private GameObject[] m_results;
    [SerializeField] private GameObject m_searchInput;

    private Text m_searchInputText;
    private SearchState m_searchState;
    private string m_currSearchString;
    private string m_workingString; //This is an attempt to stop new'ing strings every frame!

    /*
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
    */

    private BackEndAPI m_backEndAPI;
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start() 
	{
        //m_posts = new List<Post>();

        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        m_backEndAPI = new BackEndAPI(this, m_user.GetErrorMessage(), m_user);

        m_searchInputText = m_searchInput.GetComponentInChildren<Text>();
        m_searchState = SearchState.kNone;
        m_currSearchString = "";

        HideAllResults();
	}      

    public void Update()
    {
        if (m_searchState != SearchState.kNone)
        {
            Text searchTextComponent = m_searchInput.GetComponentInChildren<Text>();
            m_workingString = Regex.Replace(searchTextComponent.text, @"\s+[|]", String.Empty);
            bool stringChanged = !String.Equals(m_currSearchString, m_workingString, StringComparison.OrdinalIgnoreCase);
            if (stringChanged)
            {
                m_currSearchString = searchTextComponent.text;
                if (m_currSearchString.Length > 0)
                {
                    if (m_searchState == SearchState.kUser)
                    {
                        m_coroutineQueue.EnqueueAction(UpdateUserSearch());
                    }
                    else if (m_searchState == SearchState.kTag)
                    {
                        m_coroutineQueue.EnqueueAction(UpdateTagSearch());
                    }
                }
            }
        }
    }

    public int GetNumResults()
    {
        return m_results.GetLength(0);
    }        

    /*
    public void PreDelete()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreDelete() called on post: " + m_imageSkybox.GetImageIdentifier());

        m_confirmDeleteButton.SetActive(true);
        m_cancelDeleteButton.SetActive(true);

        Text userTextComponent = m_userMessage.GetComponentInChildren<Text>();
        userTextComponent.text = "Definitely want to delete this post? =(";
        userTextComponent.color = Color.red;
    }

    public void CancelDelete()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: CancelDelete() called");

        m_confirmDeleteButton.SetActive(false);
        m_cancelDeleteButton.SetActive(false);

        Text userTextComponent = m_userMessage.GetComponentInChildren<Text>();
        userTextComponent.text = "Delete Cancelled =)";
        userTextComponent.color = Color.black;
    }

    public void Delete()
    {
        m_confirmDeleteButton.SetActive(false);
        m_cancelDeleteButton.SetActive(false);

        m_coroutineQueue.EnqueueAction(DeletePostInternal(m_imageSkybox.GetImageIdentifier()));
    }
    */

    public void ShowSearchText()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Setting Search Text!");
        Text userTextComponent = m_user.GetUserMessage().GetComponentInChildren<Text>();
        userTextComponent.text = "Search!";
        userTextComponent.color = Color.black;
    }
        
    public void InvalidateWork() // This function is called in order to stop any ongoing work
    {        
        //m_currPostIndex = -1;
        if (m_coroutineQueue != null)
        {
            m_coroutineQueue.Clear();
        }

        ClearSearch();
    }
        
    /*
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
    */

    public void OpenSearch()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenSearch() called");

        ClearSearch();
    }       

    public void OpenUserSearch()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenProfileSearch() called");

        m_searchState = SearchState.kUser;
        OnButtonSelected(m_searchTypes[(int)m_searchState]);  // button 0 = Profile search button
        m_searchInputText.text = "";
        m_searchInput.SetActive(true);
    }

    public void OpenTagSearch()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenTagSearch() called");

        m_searchState = SearchState.kTag;
        OnButtonSelected(m_searchTypes[(int)m_searchState]);  // button 1 = Tag search button
        m_searchInputText.text = "";
        m_searchInput.SetActive(true);
    }

    /*
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
    */

    // **************************
    // Private/Helper functions
    // **************************

    private void ClearSearch()
    {
        m_searchState = SearchState.kNone;
        OnButtonSelected(null); // Deselect all buttons
        m_searchInput.SetActive(false);
        m_currSearchString = "";
    }

    private void HideAllResults()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling HideAllResults()");

        for (int resultIndex = 0; resultIndex < GetNumResults(); resultIndex++)
        {
            m_results[resultIndex].SetActive(false);
        }
    }

    private void OnButtonSelected(GameObject button)
    {
        foreach(GameObject currButton in m_searchTypes)
        {       
            var searchTypeButton = currButton.GetComponent<SelectedButton>();
            if (button == currButton)
            {
                searchTypeButton.OnButtonSelected();
            }
            else 
            {
                searchTypeButton.OnButtonDeselected();
            }
        }
    }

    private IEnumerator UpdateUserSearch()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: UpdateUserSearch() called");

        yield return m_backEndAPI.Search_SearchForUsers(
            m_currSearchString
        );

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            SetUserResults();
        }
    }

    private IEnumerator UpdateTagSearch()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: UpdateTagSearch() called");

        yield return m_backEndAPI.Search_SearchForHashTags(
            m_currSearchString
        );

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            SetTagResults();
        }
    }

    private void SetUserResults()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling SetUserResults()");

        int numUsers = m_backEndAPI.GetUsersResult().data.Count;
        for (int resultIndex = 0, userIndex = 0; resultIndex < GetNumResults(); resultIndex++, userIndex++)
        {            
            if (userIndex < numUsers)
            {
                m_results[resultIndex].GetComponentInChildren<Text>().text = m_backEndAPI.GetUsersResult().data[userIndex].attributes.handle;
                m_results[resultIndex].SetActive(true);
            }
            else
            {
                m_results[resultIndex].SetActive(false);
            }
        }
    }

    private void SetTagResults()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling SetTagResults()");

        int numTags = m_backEndAPI.GetTagsResult().data.Count;
        for (int resultIndex = 0, tagIndex = 0; resultIndex < GetNumResults(); resultIndex++, tagIndex++)
        {
            if (tagIndex < numTags)
            {
                m_results[resultIndex].GetComponentInChildren<Text>().text = m_backEndAPI.GetTagsResult().data[tagIndex].attributes.tag;
                m_results[resultIndex].SetActive(true);
            }
            else
            {
                m_results[resultIndex].SetActive(false);
            }
        }
    }

    /*
    private IEnumerator DeletePostInternal(string id)
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Delete() called on post: " + id);

        m_loadingIcon.Display();

        yield return m_backEndAPI.Posts_DeletePost(id);

        Text userTextComponent = m_userMessage.GetComponentInChildren<Text>();
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
    */

    /*
    private IEnumerator StoreFirstPostsAndSetSpheres()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Getting First set of Posts for Logged in User");

        m_loadingIcon.Display(); //NOTE: This should stop the following operation from ever being cut half-way through

        m_posts.Clear();

        yield return m_backEndAPI.Posts_GetPage();

        VReelJSON.Model_Posts posts = m_backEndAPI.GetAllPostsResult();
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

        yield return m_backEndAPI.Posts_GetPage(m_nextPageOfPosts);

        VReelJSON.Model_Posts posts = m_backEndAPI.GetAllPostsResult();
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
    */

    /*
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
    */

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
    */
}