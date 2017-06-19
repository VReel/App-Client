using UnityEngine;
using UnityEngine.UI;                 // Text
using System;                         // StringComparer
using System.Collections;             // IEnumerator
using System.Globalization;           // CompareOptions
using System.Text.RegularExpressions; // Regex
using System.Collections.Generic;     // List
using System.Linq;                    // Enumerable

public class Search : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    public enum SearchState
    {
        kNone = -1,           // This should only be the state at the start when opening the Search menu
        kUserSearch =  0,     // Searching for a User
        kTagSearch =  1,      // Searching for a HashTag
        //kUserDisplay =  2,    // Displaying a User
        kTagDisplay =  3      // Displaying a HashTag
    }

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private Profile m_profile;
    [SerializeField] private Posts m_posts;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private KeyBoard m_keyboard;
    [SerializeField] private GameObject m_searchTitle;
    [SerializeField] private GameObject m_resultTopLevel;
    [SerializeField] private GameObject m_buttonsTopLevel;
    [SerializeField] private GameObject m_searchInput;
    [SerializeField] private GameObject[] m_searchTypes;
    [SerializeField] private GameObject[] m_resultObjects;

    public class Result
    {
        public string id { get; set; }
        public string text { get; set; }
    }

    private const string kSearchText = "Search!";

    private List<Result> m_results;
    private Text m_searchInputText;
    private SearchState m_searchState;
    private string m_currSearchString;
    private string m_workingString; //This is an attempt to stop new'ing strings every frame!

    private BackEndAPI m_backEndAPI;
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start() 
	{
        m_results = new List<Result>();
        for (int i = 0; i < m_resultObjects.Length; i++)
        {
            m_results.Add(new Result());
        }

        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();

        m_backEndAPI = new BackEndAPI(this, m_user.GetErrorMessage(), m_user);

        m_searchInputText = m_searchInput.GetComponentInChildren<Text>();
        m_searchState = SearchState.kNone;
        m_currSearchString = "";

        HideAllResults();

        m_menuController.RegisterToUseMenuConfig(this);
        MenuController.MenuConfig menuConfig = m_menuController.GetMenuConfigForOwner(this);
        menuConfig.menuBarVisible = true;
        menuConfig.imageSpheresVisible = true;
        menuConfig.subMenuVisible = true;
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
                    if (m_searchState == SearchState.kUserSearch)
                    {
                        m_coroutineQueue.EnqueueAction(UpdateUserSearch());
                    }
                    else if (m_searchState == SearchState.kTagSearch)
                    {
                        m_coroutineQueue.EnqueueAction(UpdateTagSearch());
                    }
                }
            }
        }
    }

    public int GetNumResultObjects()
    {
        return m_resultObjects.GetLength(0);
    }        

    public SearchState GetSearchState()
    {
        return m_searchState;
    }

    public void InvalidateWork() // This function is called in order to stop any ongoing work
    {        
        //m_currPostIndex = -1;
        if (m_coroutineQueue != null)
        {
            m_coroutineQueue.Clear();
        }

        ResetSearch();
    }

    public void OpenSearch()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenSearch() called");

        OpenUserSearch();
    }       

    public void OpenUserSearch()
    {
        if (m_searchState == SearchState.kUserSearch)
        {
            return;
        }            

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenUserSearch() called");

        m_imageSphereController.HideAllImageSpheres();
        m_searchState = SearchState.kUserSearch;
        OnButtonSelected(m_searchTypes[(int)m_searchState]);  // button 0 = Profile search button
        m_searchInputText.text = "";
        m_buttonsTopLevel.SetActive(true);
        HideAllResults();
    }

    public void OpenTagSearch()
    {
        if (m_searchState == SearchState.kTagSearch)
        {
            return;
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenTagSearch() called");

        m_imageSphereController.HideAllImageSpheres();
        m_searchState = SearchState.kTagSearch;
        OnButtonSelected(m_searchTypes[(int)m_searchState]);  // button 1 = Tag search button
        m_searchInputText.text = "";
        m_buttonsTopLevel.SetActive(true);
        HideAllResults();
    }        

    public void OpenProfileOrTag(int index)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenProfileOrTag() called with Index: " + index);

        HideAllResults();
        if (m_keyboard.ShouldBeShowing())
        {
            m_keyboard.AcceptText();
        }
        else
        {
            m_keyboard.CancelText();
        }
        ClearSearch();

        m_menuController.GetMenuConfigForOwner(this).menuBarVisible = false;
        m_menuController.UpdateMenuConfig(this);

        if (m_searchState == SearchState.kUserSearch)
        {
            m_profile.OpenProfileWithId(m_results[index].id, m_results[index].text);
        }
        else if (m_searchState == SearchState.kTagSearch)
        {
            m_searchTitle.SetActive(true);
            m_searchTitle.GetComponentInChildren<Text>().text = m_results[index].text;

            m_searchState = SearchState.kTagDisplay;
            m_posts.OpenHashTag(m_results[index].id, m_results[index].text);
        }            
    }

    public void CloseResultsDisplay()
    {        
        m_menuController.GetMenuConfigForOwner(this).menuBarVisible = true;
        m_menuController.UpdateMenuConfig(this);
        OpenTagSearch();
    }

    /*
    public void OpenSearchAndProfileWithId(string userId, string userHandle)
    {
        if(m_user.IsCurrentUser(userId))
        {
            m_appDirector.RequestProfileState();
            return;
        }

        m_appDirector.RequestSearchState();
        OpenUserSearch();
        ClearSearch();
        m_searchState = SearchState.kUserDisplay;
        m_posts.OpenProfileWithID(userId, userHandle);
    }
    */

    /*
    public void OpenSearchAndTagWithId(string hashTagId)
    {
        m_appDirector.RequestSearchState();
        OpenTagSearch();
        m_searchState = SearchState.kTagDisplay;
        m_posts.OpenHashTag(hashTagId);
    }
    */

    // **************************
    // Private/Helper functions
    // **************************

    private void ResetSearch()
    {
        m_searchState = SearchState.kNone;
        ClearSearch();
    }

    private void ClearSearch()
    {
        OnButtonSelected(null); // Deselect all buttons
        m_buttonsTopLevel.SetActive(false);
        m_currSearchString = "";
    }

    private void HideAllResults()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling HideAllResults()");

        for (int resultIndex = 0; resultIndex < GetNumResultObjects(); resultIndex++)
        {
            m_resultObjects[resultIndex].SetActive(false);
        }
        m_resultTopLevel.SetActive(false);
        m_searchTitle.SetActive(false);
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

        yield return m_backEndAPI.User_SearchForUsers(
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

        yield return m_backEndAPI.HashTag_SearchForHashTags(
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

        m_searchTitle.SetActive(false);
        m_resultTopLevel.SetActive(true);

        int numUsers = m_backEndAPI.GetUsersResult().data.Count;
        for (int index = 0; index < GetNumResultObjects(); index++)
        {            
            if (index < numUsers)
            {
                m_results[index].id = m_backEndAPI.GetUsersResult().data[index].id;
                m_results[index].text = m_backEndAPI.GetUsersResult().data[index].attributes.handle;

                m_resultObjects[index].GetComponentInChildren<Text>().text = m_results[index].text;
                m_resultObjects[index].SetActive(true);
            }
            else
            {
                m_resultObjects[index].SetActive(false);
            }
        }
    }

    private void SetTagResults()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling SetTagResults()");

        m_searchTitle.SetActive(false);
        m_resultTopLevel.SetActive(true);

        int numTags = m_backEndAPI.GetTagsResult().data.Count;
        for (int index = 0; index < GetNumResultObjects(); index++)
        {
            if (index < numTags)
            {
                m_results[index].id = m_backEndAPI.GetTagsResult().data[index].id;
                m_results[index].text = m_backEndAPI.GetTagsResult().data[index].attributes.tag;

                m_resultObjects[index].GetComponentInChildren<Text>().text = m_results[index].text;
                m_resultObjects[index].SetActive(true);
            }
            else
            {
                m_resultObjects[index].SetActive(false);
            }
        }
    }    

    private void DebugPrintResultList()
    {
        if (Debug.isDebugBuild)
        {
            string debugString = "------- VREEL: ResultsList ";
            for (int index = 0; index < GetNumResultObjects(); index++)
            {
                if (m_resultObjects[index].activeSelf)
                {
                    debugString +=  "Index: " + index + ", ID: " + m_results[index].id + ", Text: " + m_results[index].text + "; ";
                }
            }

            Debug.Log(debugString);
        }
    }
}