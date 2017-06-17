using UnityEngine;
using UnityEngine.UI;               // Text
using System.Collections;           // IEnumerator
using System.Collections.Generic;   // List

public class ListComments : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private Posts m_posts;
    [SerializeField] private LoadingIcon m_loadingIcon;
    [SerializeField] private GameObject m_commentCountInList;
    [SerializeField] private GameObject m_commentCountInSummary;
    [SerializeField] private GameObject[] m_displayItems;
    [SerializeField] private GameObject m_nextButton;
    [SerializeField] private GameObject m_previousButton;

    [SerializeField] private GameObject m_captionUpdateText;
    [SerializeField] private GameObject m_commentNewText;
    [SerializeField] private GameObject m_commentUpdateText;
    [SerializeField] private GameObject m_imageSummaryCaption;

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
	}            

    public CommentResult GetCommentResult(int commentIndex)
    {
        int actualResultIndex = m_currResultIndex + commentIndex;
        return m_commentResults[actualResultIndex];
    }

    public bool IsCaptionSelected(int commentIndex)
    {
        int actualCommentIndex = m_currResultIndex + m_currSelectedCommentIndex;
        return actualCommentIndex == 0;
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
        string commentCountText = (m_currImageSphere.GetCommentCount() + 1).ToString() + (m_currImageSphere.GetCommentCount() == 1 ? " comment" : " comments");
        m_commentCountInList.GetComponentInChildren<Text>().text = commentCountText;
        m_commentCountInSummary.GetComponentInChildren<Text>().text = commentCountText;
        m_coroutineQueue.EnqueueAction(StoreAndDisplayCommentResultsInternal(postId));
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
            m_coroutineQueue.EnqueueAction(StoreCommentResultsFromNextPage());
        }

        DisplayCommentResultsOnItems();
    }

    public void PreviousCommentResults()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreviousUserResults() called");

        int numResultsToDisplay = GetNumDisplayItems();
        int numCommentResults = m_commentResults.Count;

        m_currResultIndex = Mathf.Clamp(m_currResultIndex - numResultsToDisplay, 0, numCommentResults);
        m_coroutineQueue.Clear(); // Ensures we don't repeat operations

        DisplayCommentResultsOnItems();
    }
        
    public bool PreUpdateComment(int commentIndex)
    {
        m_currSelectedCommentIndex = commentIndex;
        int actualCommentIndex = m_currResultIndex + m_currSelectedCommentIndex;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreUpdateComment() called on comment: " + m_commentResults[actualCommentIndex].commentId);

        bool updateCommentAvailable = m_user.IsCurrentUser(m_commentResults[actualCommentIndex].userId);
        if (updateCommentAvailable)
        {
            if (actualCommentIndex == 0) //if its the caption
            {
                m_captionUpdateText.GetComponentInChildren<Text>().text = m_commentResults[actualCommentIndex].text;
            }
            else
            {
                m_commentUpdateText.GetComponentInChildren<Text>().text = m_commentResults[actualCommentIndex].text;
            }
        }

        return updateCommentAvailable;
    }

    public void ConfirmUpdateComment()
    {     
        m_coroutineQueue.EnqueueAction(ConfirmUpdateCommentInternal());
    }        

    public void ConfirmAddComment()
    {
        m_coroutineQueue.EnqueueAction(ConfirmAddCommentInternal());
    }
        
    public void ConfirmDeleteComment()
    {     
        m_coroutineQueue.EnqueueAction(ConfirmDeleteCommentInternal());
    }        
     
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
        
    private IEnumerator StoreAndDisplayCommentResultsInternal(string postId)
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_currResultIndex = 0;
        m_commentResults.Clear();
        m_postId = postId;

        yield return GetResults();

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {            
            StoreCaption();

            StoreNewCommentResults();

            DisplayCommentResultsOnItems();
        }
    }

    private IEnumerator StoreCommentResultsFromNextPage()
    {
        yield return m_appDirector.VerifyInternetConnection();

        yield return GetResults(m_nextPageOfResults);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            StoreNewCommentResults();
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

    private void StoreNewCommentResults()
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

    private void DisplayCommentResultsOnItems()
    {
        int startingPostIndex = m_currResultIndex;
        int numResultsToDisplay = GetNumDisplayItems();
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Displaying {0} results beginning at index {1}. We've found {2} comments for the post!", numResultsToDisplay, startingPostIndex, m_commentResults.Count));

        int userResultIndex = startingPostIndex;
        for (int itemIndex = 0; itemIndex < numResultsToDisplay; userResultIndex++, itemIndex++)
        {
            if (userResultIndex < m_commentResults.Count)
            {                                   
                // NOTE: The reliance on an expected order for Text components is not the best way to do this...
                m_displayItems[itemIndex].SetActive(true);
                Text[] textItems = m_displayItems[itemIndex].GetComponentsInChildren<Text>(); 
                textItems[0].text = m_commentResults[userResultIndex].userHandle;
                textItems[1].text = m_commentResults[userResultIndex].text;
            }
            else
            {
                m_displayItems[itemIndex].SetActive(false);
            }
        }
    }

    private IEnumerator ConfirmUpdateCommentInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        int actualCommentIndex = m_currResultIndex + m_currSelectedCommentIndex;
        string commentText = (actualCommentIndex == 0 ? m_captionUpdateText : m_commentUpdateText).GetComponentInChildren<Text>().text;
        if (actualCommentIndex == 0) // actualCommentIndex = 0 is the Caption
        {
            yield return m_backEndAPI.Post_UpdatePost(m_postId, commentText);
        }
        else //Any other CommentIndex represents an actual Comment
        {
            yield return m_backEndAPI.Comment_UpdateComment(m_commentResults[actualCommentIndex].commentId, commentText);
        }

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {            
            m_commentResults[actualCommentIndex].text = commentText;
            if (actualCommentIndex == 0) //if its the caption
            {
                m_posts.RefreshPostData(m_postId);
                m_imageSummaryCaption.GetComponentInChildren<Text>().text = commentText;
            }

            DisplayCommentResultsOnItems();
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

            DisplayCommentResultsOnItems();

            m_currImageSphere.AddToCommentCount(1);
            string commentCountText = (m_currImageSphere.GetCommentCount() + 1).ToString() + (m_currImageSphere.GetCommentCount() == 1 ? " comment" : " comments");
            m_commentCountInList.GetComponentInChildren<Text>().text = commentCountText;
            m_commentCountInSummary.GetComponentInChildren<Text>().text = commentCountText;
        }
    }        

    private IEnumerator ConfirmDeleteCommentInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        int actualCommentIndex = m_currResultIndex + m_currSelectedCommentIndex;
        yield return m_backEndAPI.Comment_DeleteComment(m_commentResults[actualCommentIndex].commentId);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            m_commentResults.RemoveAt(actualCommentIndex);

            DisplayCommentResultsOnItems();

            m_currImageSphere.AddToCommentCount(-1);
            string commentCountText = (m_currImageSphere.GetCommentCount() + 1).ToString() + (m_currImageSphere.GetCommentCount() == 1 ? " comment" : " comments");
            m_commentCountInList.GetComponentInChildren<Text>().text = commentCountText;
            m_commentCountInSummary.GetComponentInChildren<Text>().text = commentCountText;
        }
    }
}