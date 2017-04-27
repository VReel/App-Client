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
    //[SerializeField] private Search m_search;
    [SerializeField] private ProfileDetails m_profileDetails;
    [SerializeField] private GameObject m_displayItemsTopLevel; //Top-level object for results
    [SerializeField] private GameObject[] m_displayItems;
    [SerializeField] private GameObject m_nextButton;
    [SerializeField] private GameObject m_previousButton;

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

    public void DisplayCommentResults(string postId)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: DisplayCommentResults() called for post ID: " + postId);

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
        
    public IEnumerator StoreAndDisplayUserResultsInternal(string postId)
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_profileDetails.CloseProfileDetails();

        m_commentResults.Clear();
        m_postId = postId;

        yield return GetResults();

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {            
            StoreNewUserResults();

            DisplayUserResultsOnItems();
        }

        m_displayItemsTopLevel.SetActive(m_commentResults.Count > 0);
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

    private void StoreNewUserResults()
    {
        VReelJSON.Model_Comments comments = m_backEndAPI.GetCommentsResult();
        if (comments != null)
        {
            foreach (VReelJSON.CommentData commentData in comments.data)
            {   
                CommentResult newUserResult = new CommentResult();
                newUserResult.commentId = commentData.id.ToString();
                newUserResult.text = commentData.attributes.text.ToString();
                newUserResult.edited = commentData.attributes.edited;
                newUserResult.userId = commentData.relationships.user.data.id;
                newUserResult.userHandle = Helper.GetHandleFromIDAndUserData(comments.included, newUserResult.userId);

                m_commentResults.Add(newUserResult);
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
                m_displayItems[itemIndex].GetComponentInChildren<Text>().text = m_commentResults[userResultIndex].text;
            }
            else
            {
                m_displayItems[itemIndex].SetActive(false);
            }
        }
    }
}