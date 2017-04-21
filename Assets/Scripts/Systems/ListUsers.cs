using UnityEngine;
using UnityEngine.UI;               // Text
using System.Collections;           // IEnumerator
using System.Collections.Generic;   // List

public class ListUsers : MonoBehaviour 
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

    public class UserResult
    {
        public string userId { get; set; }
        public string userHandle { get; set; }    
    }

    public enum ResultType
    {
        kLikes,
        kFollowers,
        kFollowing
    };

    private ResultType m_resultType;
    private string m_currPostOrUserID = null;
    private int m_currResultIndex = 0;

    private List<UserResult> m_userResults;
    private string m_nextPageOfResults = null;
    private BackEndAPI m_backEndAPI;
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start() 
	{
        m_userResults = new List<UserResult>();

        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();

        m_backEndAPI = new BackEndAPI(this, m_user.GetErrorMessage(), m_user);

        m_displayItemsTopLevel.SetActive(false);
	}            

    public void Update()
    {
        m_nextButton.SetActive(!IsIndexAtEnd());
        m_previousButton.SetActive(!IsIndexAtStart());
    }        

    public void DisplayLikeResults(string postId)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: DisplayUserResults() called for post ID: " + postId);

        m_resultType = ResultType.kLikes;
        m_coroutineQueue.EnqueueAction(StoreAndDisplayUserResultsInternal(postId));
    }

    public void DisplayFollowersResults(string userId)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: DisplayFollowersResults() called for user ID: " + userId);

        m_resultType = ResultType.kFollowers;
        m_coroutineQueue.EnqueueAction(StoreAndDisplayUserResultsInternal(userId));
    }

    public void DisplayFollowingResults(string userId)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: DisplayFollowingResults() called for user ID: " + userId);

        m_resultType = ResultType.kFollowing;
        m_coroutineQueue.EnqueueAction(StoreAndDisplayUserResultsInternal(userId));
    }

    public void NextUserResults()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: NextUserResults() called");

        int numResultsToDisplay = GetNumDisplayItems();
        int numUserResults = m_userResults.Count;

        m_currResultIndex = Mathf.Clamp(m_currResultIndex + numResultsToDisplay, 0, numUserResults);
        m_coroutineQueue.Clear(); // Ensures we don't repeat operations

        if (m_nextPageOfResults != null)
        {   // By calling this every time a user presses the next button, we ensure he can never miss out on posts and don't overload the API            
            m_coroutineQueue.EnqueueAction(StoreUserResultsFromNextPage());
        }

        DisplayUserResultsOnItems();
    }

    public void PreviousUserResults()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreviousUserResults() called");

        int numResultsToDisplay = GetNumDisplayItems();
        int numUserResults = m_userResults.Count;

        m_currResultIndex = Mathf.Clamp(m_currResultIndex - numResultsToDisplay, 0, numUserResults);
        m_coroutineQueue.Clear(); // Ensures we don't repeat operations

        DisplayUserResultsOnItems();
    }

    public void HandleSelected(int userResultItemIndex)
    {
        m_displayItemsTopLevel.SetActive(false);
        int actualResultIndex = m_currResultIndex + userResultItemIndex;
        m_search.OpenSearchAndProfileWithId(m_userResults[actualResultIndex].userId, m_userResults[actualResultIndex].userHandle);
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
        int numLikeResults = m_userResults.Count;
        return m_currResultIndex >= (numLikeResults - numDisplayItems);       
    }        
        
    public IEnumerator StoreAndDisplayUserResultsInternal(string postOrUserId)
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_userResults.Clear();
        m_currPostOrUserID = postOrUserId;

        yield return GetResults();

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {            
            StoreNewUserResults();

            DisplayUserResultsOnItems();
        }

        m_displayItemsTopLevel.SetActive(m_userResults.Count > 0);
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
        if (m_resultType == ResultType.kLikes)
        {
            yield return m_backEndAPI.Post_GetPostLikes(m_currPostOrUserID, nextPageOfResults);
        }
        else if (m_resultType == ResultType.kFollowers)
        {
            yield return m_backEndAPI.User_GetUserFollowers(m_currPostOrUserID, nextPageOfResults);
        }
        else if (m_resultType == ResultType.kFollowing)
        {
            yield return m_backEndAPI.User_GetUserFollowing(m_currPostOrUserID, nextPageOfResults);
        }
    }

    private void StoreNewUserResults()
    {
        VReelJSON.Model_Users users = m_backEndAPI.GetUsersResult();
        if (users != null)
        {
            foreach (VReelJSON.UserData userData in users.data)
            {   
                UserResult newUserResult = new UserResult();
                newUserResult.userId = userData.id.ToString();
                newUserResult.userHandle = userData.attributes.handle.ToString();

                m_userResults.Add(newUserResult);
            }

            m_nextPageOfResults = null;
            if (users.meta.next_page)
            {
                m_nextPageOfResults = users.meta.next_page_id;
            }
        }
    }

    private void DisplayUserResultsOnItems()
    {
        int startingPostIndex = m_currResultIndex;
        int numResultsToDisplay = GetNumDisplayItems();
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Displaying {0} like results beginning at index {1}. We've found {2} likes for the post!", numResultsToDisplay, startingPostIndex, m_userResults.Count));

        int userResultIndex = startingPostIndex;
        for (int itemIndex = 0; itemIndex < numResultsToDisplay; userResultIndex++, itemIndex++)
        {
            if (userResultIndex < m_userResults.Count)
            {                                   
                m_displayItems[itemIndex].SetActive(true);
                m_displayItems[itemIndex].GetComponentInChildren<Text>().text = m_userResults[userResultIndex].userHandle;
            }
            else
            {
                m_displayItems[itemIndex].SetActive(false);
            }
        }
    }
}