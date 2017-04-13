using UnityEngine;
using UnityEngine.UI;               // Text
using System.Collections;           // IEnumerator

public class Profile : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private Posts m_posts;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private LoadingIcon m_loadingIcon;
    [SerializeField] private GameObject m_newUserText;   
    [SerializeField] private GameObject m_confirmDeleteButton;
    [SerializeField] private GameObject m_cancelDeleteButton;

    private BackEndAPI m_backEndAPI;
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start() 
	{
        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        m_backEndAPI = new BackEndAPI(this, m_user.GetErrorMessage(), m_user);
	}          

    public void Update() //TODO Make this event based instead...
    {
        bool noImagesUploaded = m_posts.GetNumPosts() <= 0;
        m_newUserText.SetActive(noImagesUploaded); // If the user has yet to upload any images then show them the New User Text!
    }

    public void OpenProfile()
    {
        m_posts.OpenUserProfile();
    }

    public void PreDelete()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreDelete() called on post: " + m_imageSkybox.GetImageIdentifier());

        m_confirmDeleteButton.SetActive(true);
        m_cancelDeleteButton.SetActive(true);

        Text userTextComponent = m_user.GetUserMessage().GetComponentInChildren<Text>();
        userTextComponent.text = "Definitely want to delete this post? =(";
        userTextComponent.color = Color.red;
    }

    public void CancelDelete()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: CancelDelete() called");

        m_confirmDeleteButton.SetActive(false);
        m_cancelDeleteButton.SetActive(false);

        Text userTextComponent = m_user.GetUserMessage().GetComponentInChildren<Text>();
        userTextComponent.text = "Delete Cancelled =)";
        userTextComponent.color = Color.black;
    }

    public void Delete()
    {
        m_confirmDeleteButton.SetActive(false);
        m_cancelDeleteButton.SetActive(false);

        m_coroutineQueue.EnqueueAction(DeletePostInternal(m_imageSkybox.GetImageIdentifier()));
    }

    public void ShowProfileText()
    {
        m_coroutineQueue.EnqueueAction(ShowProfileTextInternal());
    }
        
    public void InvalidateWork() // This function is called in order to stop any ongoing work
    {        
        m_posts.InvalidateWork();
        if (m_coroutineQueue != null)
        {
            m_coroutineQueue.Clear();
        }
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator DeletePostInternal(string id)
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Delete() called on post: " + id);

        m_loadingIcon.Display();

        yield return m_backEndAPI.Post_DeletePost(id);

        Text userTextComponent = m_user.GetUserMessage().GetComponentInChildren<Text>();
        if (m_backEndAPI.IsLastAPICallSuccessful())
        {            
            // Report Success in Profile
            userTextComponent.text = "Post Deleted Successfully! =)";
            userTextComponent.color = Color.black;

            m_posts.RequestPostRemoval(id);
        }
        else
        {
            // Report Failure in Profile
            userTextComponent.text = "Deleting failed =(\n Please try again!";
            userTextComponent.color = Color.red;
        }

        m_loadingIcon.Hide();
    }

    private IEnumerator ShowProfileTextInternal()
    {
        while (!m_user.IsLoggedIn() || !m_user.IsUserDataStored())
        {
            yield return null;
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Setting Profile Text!");
        Text userTextComponent = m_user.GetUserMessage().GetComponentInChildren<Text>();
        userTextComponent.text = m_user.m_handle + "'s Profile";
        userTextComponent.color = Color.black;
    }
}