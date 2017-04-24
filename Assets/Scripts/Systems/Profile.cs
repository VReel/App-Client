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

    private const string kPreDeleteText = "Definitely want to delete this post? =O";
    private const string kCancelDeleteText = "Delete Cancelled =)";
    private const string kSuccessfulDeleteText = "Post Deleted Successfully! =)";
    private const string kFailedDeleteText = "Deleting failed =(\n Please try again!";

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

        m_user.GetUserMessageButton().SetTextAsError(kPreDeleteText);
    }

    public void CancelDelete()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: CancelDelete() called");

        m_confirmDeleteButton.SetActive(false);
        m_cancelDeleteButton.SetActive(false);

        m_user.GetUserMessageButton().SetText(kCancelDeleteText);
    }

    public void Delete()
    {
        m_confirmDeleteButton.SetActive(false);
        m_cancelDeleteButton.SetActive(false);

        m_coroutineQueue.EnqueueAction(DeletePostInternal(m_imageSkybox.GetImageIdentifier()));
    }

    public void ShowProfileText()
    {
        //m_coroutineQueue.EnqueueAction(ShowProfileTextInternal());
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

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {            
            // Report Success in Profile
            m_user.GetUserMessageButton().SetText(kSuccessfulDeleteText);
            m_posts.RequestPostRemoval(id);
        }
        else
        {
            // Report Failure in Profile
            m_user.GetUserMessageButton().SetTextAsError(kFailedDeleteText);
        }

        m_loadingIcon.Hide();
    }
}