using UnityEngine;
using UnityEngine.UI;               // Text
using System.Collections;           // IEnumerator
using System.Collections.Generic;   // List

//TODO: If this doesn't change much, then it should be merged into ListUsers somehow...
public class ListComments : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private Posts m_posts;
    //[SerializeField] private Search m_search;
    [SerializeField] private ProfileDetails m_profileDetails;
    [SerializeField] private ListUsers m_listUsers;
    [SerializeField] private GameObject m_displayItemsTopLevel; //Top-level object for results
    [SerializeField] private GameObject[] m_displayItems;
    [SerializeField] private GameObject m_nextButton;
    [SerializeField] private GameObject m_previousButton;
    [SerializeField] private GameObject m_addCommentConfirmation;
    [SerializeField] private GameObject m_commentNewText;
    [SerializeField] private GameObject m_updateCommentConfirmation;
    [SerializeField] private GameObject m_commentUpdateText;
    [SerializeField] private GameObject m_deleteCommentOption;
    [SerializeField] private GameObject m_deleteCommentConfirmation;

    public class CommentResult
    {
        public string commentId { get; set; }
        public string text { get; set; }
        public bool edited { get; set; }
        public string userId { get; set; }
        public string userHandle { get; set; }
    }        

    private string m_postId = null;
    private int m_currResultIndex = 0;
    private int m_currSelectedCommentIndex = 0;
    private ImageSphere m_currImageSphere = null;

    private List<CommentResult> m_commentResults;
    private string m_nextPageOfResults = null;
    private BackEndAPI m_backEndAPI;
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start() 
	{
        m_commentResults = new List<CommentResult>();

        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();

        m_backEndAPI = new BackEndAPI(this, m_user.GetErrorMessage(), m_user);

        CloseListComments();
	}            

    public void Update()
    {
        m_nextButton.SetActive(!IsIndexAtEnd());
        m_previousButton.SetActive(!IsIndexAtStart());
    }        

    public void DisplayCommentResults(string postId, ImageSphere imageSphere)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: DisplayCommentResults() called for post ID: " + postId);

        m_currImageSphere = imageSphere;
        m_coroutineQueue.EnqueueAction(StoreAndDisplayUserResultsInternal(postId));
    }

    public void NextCommentResults()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: NextUserResults() called");

        int numResultsToDisplay = GetNumDisplayItems();
        int numCommentResults = m_commentResults.Count;

        m_currResultIndex = Mathf.Clamp(m_currResultIndex + numResultsToDisplay, 0, numCommentResults);
        m_coroutineQueue.Clear(); // Ensures we don't repeat operations

        if (m_nextPageOfResults != null)
        {   // By calling this every time a user presses the next button, we ensure he can never miss out on posts and don't overload the API            
            m_coroutineQueue.EnqueueAction(StoreUserResultsFromNextPage());
        }

        DisplayUserResultsOnItems();
    }

    public void PreviousCommentResults()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreviousUserResults() called");

        int numResultsToDisplay = GetNumDisplayItems();
        int numCommentResults = m_commentResults.Count;

        m_currResultIndex = Mathf.Clamp(m_currResultIndex - numResultsToDisplay, 0, numCommentResults);
        m_coroutineQueue.Clear(); // Ensures we don't repeat operations

        DisplayUserResultsOnItems();
    }

    public void CloseListComments()
    {
        m_displayItemsTopLevel.SetActive(false);
        CloseSubMenus();
    }
        
    public void PreAddComment()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreAddComment() called on post: " + m_postId);

        CloseSubMenus();
        m_addCommentConfirmation.SetActive(true);
    }

    public void CancelAddComment()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: CancelUpload() called");

        m_addCommentConfirmation.SetActive(true);
    }

    public void ConfirmAddComment()
    {
        m_coroutineQueue.EnqueueAction(ConfirmAddCommentInternal());
    }

    public void PreUpdateComment(int commentBoxIndex)
    {
        m_currSelectedCommentIndex = commentBoxIndex;
        int commentIndex = m_currResultIndex + m_currSelectedCommentIndex;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreUpdateComment() called on comment: " + m_commentResults[commentIndex].commentId);

        bool updateCommentAvailable = m_user.IsCurrentUser(m_commentResults[commentIndex].userId);
        if (updateCommentAvailable)
        {
            CloseSubMenus();
            m_deleteCommentOption.SetActive(commentIndex > 0);
            m_updateCommentConfirmation.SetActive(true);
        }
    }

    public void CancelUpdateComment()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: CancelUpdateComment() called");

        m_updateCommentConfirmation.SetActive(false);
    }

    public void ConfirmUpdateComment()
    {     
        m_coroutineQueue.EnqueueAction(ConfirmUpdateCommentInternal());
    }

    public void PreDeleteComment()
    {
        int commentIndex = m_currResultIndex + m_currSelectedCommentIndex;
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreDeleteComment() called on comment: " + m_commentResults[commentIndex].commentId);

        CloseSubMenus();
        m_deleteCommentConfirmation.SetActive(true);
    }

    public void CancelDeleteComment()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: CancelDeleteComment() called");

        m_deleteCommentConfirmation.SetActive(false);
    }

    public void ConfirmDeleteComment()
    {     
        m_coroutineQueue.EnqueueAction(ConfirmDeleteCommentInternal());
    }

    /*
    public void HandleSelected(int userResultItemIndex)
    {
        CloseListComments();
        int actualResultIndex = m_currResultIndex + userResultItemIndex;
        m_search.OpenSearchAndProfileWithId(m_userResults[actualResultIndex].userId, m_userResults[actualResultIndex].userHandle);
    }
    */
     
    // **************************
    // Private/Helper functions
    // **************************

    private int GetNumDisplayItems()
    {
        return m_displayItems.GetLength(0);
    }

    private bool IsIndexAtStart()
    {
        return m_currResultIndex <= 0;
    }

    private bool IsIndexAtEnd()
    {
        int numDisplayItems = GetNumDisplayItems();
        int numLikeResults = m_commentResults.Count;
        return m_currResultIndex >= (numLikeResults - numDisplayItems);       
    }        

    public void CloseSubMenus()
    {
        m_addCommentConfirmation.SetActive(false);
        m_updateCommentConfirmation.SetActive(false);
        m_deleteCommentConfirmation.SetActive(false);
    }
        
    public IEnumerator StoreAndDisplayUserResultsInternal(string postId)
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: A");

        m_profileDetails.CloseProfileDetails();
        m_listUsers.CloseListUsers();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: B");

        m_currResultIndex = 0;
        m_commentResults.Clear();
        m_postId = postId;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: C");

        yield return GetResults();

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {            
            StoreCaption();

            StoreNewUserResults();

            DisplayUserResultsOnItems();

            if (Debug.isDebugBuild) Debug.Log("------- VREEL: D");
        }

        m_displayItemsTopLevel.SetActive(true);
        m_addCommentConfirmation.SetActive(false);
        m_updateCommentConfirmation.SetActive(false);

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: E");
    }

    private IEnumerator StoreUserResultsFromNextPage()
    {
        yield return m_appDirector.VerifyInternetConnection();

        yield return GetResults(m_nextPageOfResults);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            StoreNewUserResults();
        }
    }

    private IEnumerator GetResults(string nextPageOfResults = "")
    {
        yield return m_backEndAPI.Post_GetPostComments(m_postId, nextPageOfResults);
    }

    private void StoreCaption()
    {
        Posts.Post postViewed = m_posts.GetPostFromID(m_postId);

        CommentResult newCommentResult = new CommentResult();
        newCommentResult.commentId = m_postId;
        newCommentResult.text = postViewed.caption;
        newCommentResult.edited = postViewed.edited;
        newCommentResult.userId = postViewed.userId;
        newCommentResult.userHandle = postViewed.userHandle;

        m_commentResults.Add(newCommentResult);
    }

    private void StoreNewUserResults()
    {
        VReelJSON.Model_Comments comments = m_backEndAPI.GetCommentsResult();
        if (comments != null)
        {
            foreach (VReelJSON.CommentData commentData in comments.data)
            {   
                CommentResult newCommentResult = new CommentResult();
                newCommentResult.commentId = commentData.id.ToString();
                newCommentResult.text = commentData.attributes.text.ToString();
                newCommentResult.edited = commentData.attributes.edited;
                newCommentResult.userId = commentData.relationships.user.data.id;
                newCommentResult.userHandle = Helper.GetHandleFromIDAndUserData(comments.included, newCommentResult.userId);

                m_commentResults.Add(newCommentResult);
            }

            m_nextPageOfResults = null;
            if (comments.meta.next_page)
            {
                m_nextPageOfResults = comments.meta.next_page_id;
            }
        }
    }

    private void DisplayUserResultsOnItems()
    {
        int startingPostIndex = m_currResultIndex;
        int numResultsToDisplay = GetNumDisplayItems();
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Displaying {0} results beginning at index {1}. We've found {2} comments for the post!", numResultsToDisplay, startingPostIndex, m_commentResults.Count));

        int userResultIndex = startingPostIndex;
        for (int itemIndex = 0; itemIndex < numResultsToDisplay; userResultIndex++, itemIndex++)
        {
            if (userResultIndex < m_commentResults.Count)
            {                                   
                m_displayItems[itemIndex].SetActive(true);
                m_displayItems[itemIndex].GetComponentInChildren<Text>().text = m_commentResults[userResultIndex].userHandle + ":\t" + m_commentResults[userResultIndex].text;
            }
            else
            {
                m_displayItems[itemIndex].SetActive(false);
            }
        }
    }

    private IEnumerator ConfirmAddCommentInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        string commentText = m_commentNewText.GetComponentInChildren<Text>().text;
        yield return m_backEndAPI.Comment_CreateComment(m_postId, commentText);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            CommentResult newCommentResult = new CommentResult();
            newCommentResult.commentId = m_backEndAPI.GetCommentResult().data.id.ToString();
            newCommentResult.text = m_backEndAPI.GetCommentResult().data.attributes.text.ToString();
            newCommentResult.edited = m_backEndAPI.GetCommentResult().data.attributes.edited;
            newCommentResult.userId = m_user.m_id;
            newCommentResult.userHandle = m_user.m_handle;

            m_commentResults.Add(newCommentResult);

            DisplayUserResultsOnItems();
        }

        m_currImageSphere.AddToCommentCount(1);
        CloseSubMenus();
    }

    private IEnumerator ConfirmUpdateCommentInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        string commentText = m_commentUpdateText.GetComponentInChildren<Text>().text;
        int commentIndex = m_currResultIndex + m_currSelectedCommentIndex;
        if (commentIndex == 0) // CommentIndex = 0 is the Caption
        {
            yield return m_backEndAPI.Post_UpdatePost(m_postId, commentText);
        }
        else //Any other CommentIndex represents an actual Comment
        {
            yield return m_backEndAPI.Comment_UpdateComment(m_commentResults[commentIndex].commentId, commentText);
        }

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            m_commentResults[commentIndex].text = commentText;

            DisplayUserResultsOnItems();
        }

        CloseSubMenus();
    }

    private IEnumerator ConfirmDeleteCommentInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        int commentIndex = m_currResultIndex + m_currSelectedCommentIndex;
        yield return m_backEndAPI.Comment_DeleteComment(m_commentResults[commentIndex].commentId);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            m_commentResults.RemoveAt(commentIndex);

            DisplayUserResultsOnItems();
        }

        m_currImageSphere.AddToCommentCount(-1);
        CloseSubMenus();
    }
}