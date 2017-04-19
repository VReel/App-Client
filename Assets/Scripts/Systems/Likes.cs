using UnityEngine;
using UnityEngine.UI;               // Text
using System.Collections;           // IEnumerator
using System.Collections.Generic;   // List

public class Likes : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private Search m_search;
    [SerializeField] private GameObject m_displayItemsTopLevel; //Top-level object for results
    [SerializeField] private GameObject[] m_displayItems;
    [SerializeField] private GameObject m_nextButton;
    [SerializeField] private GameObject m_previousButton;

    public class LikeResult
    {
        public string userId { get; set; }
        public string userHandle { get; set; }    
    }

    private string m_currPostID = null;
    private int m_currResultIndex = 0;

    private List<LikeResult> m_likeResults;
    private string m_nextPageOfLikes = null;
    private BackEndAPI m_backEndAPI;
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start() 
	{
        m_likeResults = new List<LikeResult>();

        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();

        m_backEndAPI = new BackEndAPI(this, m_user.GetErrorMessage(), m_user);

        m_displayItemsTopLevel.SetActive(false);
	}            

    public void Update()
    {
        m_nextButton.SetActive(!IsLikeIndexAtEnd());
        m_previousButton.SetActive(!IsLikeIndexAtStart());
    }

    public void LikeOrUnlikePost(string postId, bool doLike)
    {
        m_coroutineQueue.EnqueueAction(LikeOrUnlikePostInternal(postId, doLike));
    }

    public void DisplayLikesResults(string postId)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: DisplayPostLikes() called for post ID: " + postId);

        m_coroutineQueue.EnqueueAction(StoreAndDisplayLikeResultsInternal(postId));
    }

    public void NextLikeResults()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: NextLikeResults() called");

        int numResultsToDisplay = GetNumDisplayItems();
        int numLikeResults = m_likeResults.Count;

        m_currResultIndex = Mathf.Clamp(m_currResultIndex + numResultsToDisplay, 0, numLikeResults);
        m_coroutineQueue.Clear(); // Ensures we don't repeat operations

        if (m_nextPageOfLikes != null)
        { // By calling this every time a user presses the next button, we ensure he can never miss out on posts and don't overload the API            
            m_coroutineQueue.EnqueueAction(StoreLikeResultsFromNextPage());
        }

        DisplayLikeResultsOnItems();
    }

    public void PreviousLikeResults()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreviousLikeResults() called");

        int numResultsToDisplay = GetNumDisplayItems();
        int numLikeResults = m_likeResults.Count;

        m_currResultIndex = Mathf.Clamp(m_currResultIndex - numResultsToDisplay, 0, numLikeResults);
        m_coroutineQueue.Clear(); // Ensures we don't repeat operations

        DisplayLikeResultsOnItems();
    }

    public void HandleSelected(int likeResultItemIndex)
    {
        m_displayItemsTopLevel.SetActive(false);
        m_search.OpenSearchAndProfileWithId(m_likeResults[m_currResultIndex + likeResultItemIndex].userId);
    }
     
    // **************************
    // Private/Helper functions
    // **************************

    private int GetNumDisplayItems()
    {
        return m_displayItems.GetLength(0);
    }

    private bool IsLikeIndexAtStart()
    {
        return m_currResultIndex <= 0;
    }

    private bool IsLikeIndexAtEnd()
    {
        int numDisplayItems = GetNumDisplayItems();
        int numLikeResults = m_likeResults.Count;
        return m_currResultIndex >= (numLikeResults - numDisplayItems);       
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

    public IEnumerator StoreAndDisplayLikeResultsInternal(string postId)
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_likeResults.Clear();
        m_currPostID = postId;

        yield return m_backEndAPI.Post_GetPostLikes(m_currPostID);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {            
            StoreNewLikeResults();

            DisplayLikeResultsOnItems();
        }

        m_displayItemsTopLevel.SetActive(m_likeResults.Count > 0);
    }

    private IEnumerator StoreLikeResultsFromNextPage()
    {
        yield return m_appDirector.VerifyInternetConnection();

        yield return m_backEndAPI.Post_GetPostLikes(m_currPostID, m_nextPageOfLikes);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            StoreNewLikeResults();
        }
    }

    private void StoreNewLikeResults()
    {
        VReelJSON.Model_Users users = m_backEndAPI.GetUsersResult();
        if (users != null)
        {
            foreach (VReelJSON.UserData userData in users.data)
            {   
                LikeResult newLikeResult = new LikeResult();
                newLikeResult.userId = userData.id.ToString();
                newLikeResult.userHandle = userData.attributes.handle.ToString();

                m_likeResults.Add(newLikeResult);
            }

            m_nextPageOfLikes = null;
            if (users.meta.next_page)
            {
                m_nextPageOfLikes = users.meta.next_page_id;
            }
        }
    }

    private void DisplayLikeResultsOnItems()
    {
        int startingPostIndex = m_currResultIndex;
        int numResultsToDisplay = GetNumDisplayItems();
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Displaying {0} like results beginning at index {1}. We've found {2} likes for the post!", numResultsToDisplay, startingPostIndex, m_likeResults.Count));

        int likeResultIndex = startingPostIndex;
        for (int itemIndex = 0; itemIndex < numResultsToDisplay; likeResultIndex++, itemIndex++)
        {
            if (likeResultIndex < m_likeResults.Count)
            {                                   
                m_displayItems[itemIndex].SetActive(true);
                m_displayItems[itemIndex].GetComponentInChildren<Text>().text = m_likeResults[likeResultIndex].userHandle;
            }
            else
            {
                m_displayItems[itemIndex].SetActive(false);
            }
        }
    }
}