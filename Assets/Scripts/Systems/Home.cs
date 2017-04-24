using UnityEngine;
using UnityEngine.UI;               // Text
using System.Collections;           // IEnumerator
using System.Collections.Generic;   // List

public class Home : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    public enum HomeState
    {
        kNone = -1,
        kPublicTimeline = 0,
        kPersonalTimeline = 1
    }

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private Posts m_posts;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private GameObject[] m_timelineTypes;

    private HomeState m_homeState;
    //private BackEndAPI m_backEndAPI;
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start() 
	{
        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        //m_backEndAPI = new BackEndAPI(this, m_user.GetErrorMessage(), m_user);

        m_homeState = HomeState.kNone;
	}

    public void OpenHome()
    {
        OpenPublicTimeline();
    }

    public void OpenPublicTimeline()
    {
        if (m_homeState == HomeState.kPublicTimeline)
        {
            return;
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenPublicTimeline() called");

        m_homeState = HomeState.kPublicTimeline;
        OnButtonSelected(m_timelineTypes[(int)m_homeState]);  // button 0 = Public timeline button

        m_posts.OpenPublicTimeline();
    }

    public void OpenPersonalTimeline()
    {
        if (m_homeState == HomeState.kPersonalTimeline)
        {
            return;
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenPersonalTimeline() called");

        m_homeState = HomeState.kPersonalTimeline;
        OnButtonSelected(m_timelineTypes[(int)m_homeState]);  // button 1 = Personal timeline button

        m_posts.OpenPersonalTimeline();
    }             

    public void ShowHomeText()
    {
        m_coroutineQueue.EnqueueAction(ShowHomeTextInternal());
    }
        
    public void InvalidateWork() // This function is called in order to stop any ongoing work
    {        
        m_posts.InvalidateWork();
        if (m_coroutineQueue != null)
        {
            m_coroutineQueue.Clear();
        }

        m_homeState = HomeState.kNone;
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void OnButtonSelected(GameObject button)
    {
        foreach(GameObject currButton in m_timelineTypes)
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

    private IEnumerator ShowHomeTextInternal()
    {
        while (!m_user.IsLoggedIn() || !m_user.IsUserDataStored())
        {
            yield return null;
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Setting Home Text!");
        m_user.GetUserMessageButton().SetText("Hi " + m_user.m_handle + "! =D");
    }
}