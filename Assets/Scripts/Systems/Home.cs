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
        
    public void OpenPublicTimeline()
    {
        if (m_homeState == HomeState.kPublicTimeline)
        {
            return;
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenPublicTimeline() called");

        m_homeState = HomeState.kPublicTimeline; // TODO: Change to Explore   

        m_posts.OpenPublicTimeline();
    }

    public void OpenPersonalTimeline()
    {
        if (m_homeState == HomeState.kPersonalTimeline)
        {
            return;
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenPersonalTimeline() called");

        m_homeState = HomeState.kPersonalTimeline; // TODO: Change to Following

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