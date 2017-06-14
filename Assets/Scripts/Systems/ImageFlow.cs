using UnityEngine;
using System.Collections;   // IEnumerator
using UnityEngine.UI;       // Text

public class ImageFlow : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;  
    [SerializeField] private User m_user;
    [SerializeField] private Posts m_posts;
    [SerializeField] private ProfileDetails m_profile;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private ListComments m_listComments;
    [SerializeField] private LoadingIcon m_loadingIcon;
    [SerializeField] private GameObject m_handleObject;
    [SerializeField] private GameObject m_captionObject;
    [SerializeField] private GameObject m_likeCountObject;
    [SerializeField] private GameObject m_commentCountObject;
    [SerializeField] private GameObject m_heartObject;

    [SerializeField] private GameObject m_imageSubMenu;
    [SerializeField] private GameObject m_imageSummaryPage;
    [SerializeField] private GameObject m_commentsPage;
    [SerializeField] private GameObject m_editCaptionPage;
    [SerializeField] private GameObject m_newCommentPage;
    [SerializeField] private GameObject m_updateCommentPage;
    [SerializeField] private GameObject m_imageOptionsPersonalPage;
    [SerializeField] private GameObject m_imageOptionsOthersPage;
    [SerializeField] private GameObject m_deleteImagePage;
    [SerializeField] private GameObject m_reportImagePage;

    private ImageSphere m_currImageSphere;
    private CoroutineQueue m_coroutineQueue;
    private BackEndAPI m_backEndAPI;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {        
        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();

        m_backEndAPI = new BackEndAPI(this, m_user.GetErrorMessage(), m_user);

        m_imageSubMenu.SetActive(false);
    }

    public void OpenWithImageSphere(ImageSphere imageSphere)
    {
        m_currImageSphere = imageSphere;

        SetImageFlowPage(0);
        SetImageSummary();

        m_menuController.SetCurrentSubMenuActive(false);
        m_menuController.SetImagesAndMenuBarActive(false);

        m_listComments.PreUpdateUserResults( m_currImageSphere.GetImageIdentifier() );

        m_appDirector.SetOverlayShowing(true);
    }

    public void Close()
    {
        m_imageSubMenu.SetActive(false);

        m_menuController.SetImagesAndMenuBarActive(true);

        m_appDirector.SetOverlayShowing(false);
    }

    public void SetImageFlowPage(int pageNumber)
    {
        m_imageSubMenu.SetActive(true);

        if (pageNumber == -1)
        {
            m_imageSubMenu.SetActive(false);
        }
        else if (pageNumber == 0) // Summary 
        {            
            m_imageSummaryPage.SetActive(true);
            m_commentsPage.SetActive(false);
            m_editCaptionPage.SetActive(false);
            m_newCommentPage.SetActive(false);
            m_updateCommentPage.SetActive(false);
            m_imageOptionsPersonalPage.SetActive(false);
            m_imageOptionsOthersPage.SetActive(false);
            m_deleteImagePage.SetActive(false);
            m_reportImagePage.SetActive(false);
        }
        else if (pageNumber == 1) // Comments
        {
            m_imageSummaryPage.SetActive(false);
            m_commentsPage.SetActive(true);
            m_editCaptionPage.SetActive(false);
            m_newCommentPage.SetActive(false);
            m_updateCommentPage.SetActive(false);
            m_imageOptionsPersonalPage.SetActive(false);
            m_imageOptionsOthersPage.SetActive(false);
            m_deleteImagePage.SetActive(false);
            m_reportImagePage.SetActive(false);
        }
        else if (pageNumber == 2) // Edit Caption
        {
            m_imageSummaryPage.SetActive(false);
            m_commentsPage.SetActive(false);
            m_editCaptionPage.SetActive(true);
            m_newCommentPage.SetActive(false);
            m_updateCommentPage.SetActive(false);
            m_imageOptionsPersonalPage.SetActive(false);
            m_imageOptionsOthersPage.SetActive(false);
            m_deleteImagePage.SetActive(false);
            m_reportImagePage.SetActive(false);
        }
        else if (pageNumber == 3) // New Comment
        {
            m_imageSummaryPage.SetActive(false);
            m_commentsPage.SetActive(false);
            m_editCaptionPage.SetActive(false);
            m_newCommentPage.SetActive(true);
            m_updateCommentPage.SetActive(false);
            m_imageOptionsPersonalPage.SetActive(false);
            m_imageOptionsOthersPage.SetActive(false);
            m_deleteImagePage.SetActive(false);
            m_reportImagePage.SetActive(false);
        }
        else if (pageNumber == 4) // New Comment
        {
            m_imageSummaryPage.SetActive(false);
            m_commentsPage.SetActive(false);
            m_editCaptionPage.SetActive(false);
            m_newCommentPage.SetActive(false);
            m_updateCommentPage.SetActive(true);
            m_imageOptionsPersonalPage.SetActive(false);
            m_imageOptionsOthersPage.SetActive(false);
            m_deleteImagePage.SetActive(false);
            m_reportImagePage.SetActive(false);
        }
        else if (pageNumber == 5) // Options for my own Profile
        {
            m_imageSummaryPage.SetActive(false);
            m_commentsPage.SetActive(false);
            m_editCaptionPage.SetActive(false);
            m_newCommentPage.SetActive(false);
            m_updateCommentPage.SetActive(false);
            m_imageOptionsPersonalPage.SetActive(true);
            m_imageOptionsOthersPage.SetActive(false);
            m_deleteImagePage.SetActive(false);
            m_reportImagePage.SetActive(false);
        }
        else if (pageNumber == 6) // Options for other's Profile
        {
            m_imageSummaryPage.SetActive(false);
            m_commentsPage.SetActive(false);
            m_editCaptionPage.SetActive(false);
            m_newCommentPage.SetActive(false);
            m_updateCommentPage.SetActive(false);
            m_imageOptionsPersonalPage.SetActive(false);
            m_imageOptionsOthersPage.SetActive(true);
            m_deleteImagePage.SetActive(false);
            m_reportImagePage.SetActive(false);
        }
        else if (pageNumber == 7) // Delete Image
        {
            m_imageSummaryPage.SetActive(false);
            m_commentsPage.SetActive(false);
            m_editCaptionPage.SetActive(false);
            m_newCommentPage.SetActive(false);
            m_updateCommentPage.SetActive(false);
            m_imageOptionsPersonalPage.SetActive(false);
            m_imageOptionsOthersPage.SetActive(false);
            m_deleteImagePage.SetActive(true);
            m_reportImagePage.SetActive(false);
        }
        else if (pageNumber == 8) // Report Image
        {
            m_imageSummaryPage.SetActive(false);
            m_commentsPage.SetActive(false);
            m_editCaptionPage.SetActive(false);
            m_newCommentPage.SetActive(false);
            m_updateCommentPage.SetActive(false);
            m_imageOptionsPersonalPage.SetActive(false);
            m_imageOptionsOthersPage.SetActive(false);
            m_deleteImagePage.SetActive(false);
            m_reportImagePage.SetActive(true);
        }
    }

    public void OptionsSelected()
    {        
        if (m_currImageSphere.IsLoggedUserProfileImage())
        {
            SetImageFlowPage(5);
        }
        else
        {
            SetImageFlowPage(6);
        }
    }

    public void HandleSelected()
    {
        Close();
        m_currImageSphere.HandleSelected();
    }

    public void HeartSelected()
    {
        m_currImageSphere.HeartSelectedWithObject(m_heartObject, m_likeCountObject);
    }

    public void LikesSelected()
    {
        SetImageFlowPage(-1);
        m_currImageSphere.LikesSelected();
    }

    public void CommentsSelected()
    {
        SetImageFlowPage(1);
        m_currImageSphere.CommentsSelected();
    }

    public void CaptionSelected()
    {
        if ( m_listComments.PreUpdateComment(0))
        {
            SetImageFlowPage(2);
        }
    }

    public void NewCommentSelected()
    {
        SetImageFlowPage(3);
    }
        
    public void UpdateCommentSelected(int commentIndex)
    {
        if ( m_listComments.PreUpdateComment(commentIndex) )
        {
            SetImageFlowPage(4);
        }
    }

    public void CommentHandleSelected(int commentIndex)
    {
        //TODO: Call a new function in ListComments...
    }
        
    public void DeletePost()
    {
        m_coroutineQueue.EnqueueAction(DeletePostInternal(m_imageSkybox.GetImageIdentifier()));
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void SetImageSummary()
    {
        m_currImageSphere.SetImageSummary(m_handleObject, m_captionObject, m_likeCountObject, m_commentCountObject, m_heartObject);
    }

    private IEnumerator DeletePostInternal(string id)
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Delete() called on post: " + id);

        m_loadingIcon.Display();

        yield return m_backEndAPI.Post_DeletePost(id);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {            
            // Report Success in Profile
            // TODO: Report Success!
            //m_user.GetUserMessageButton().SetText(kSuccessfulDeleteText);
            m_posts.RequestPostRemoval(id);
        }
        else
        {
            // Report Failure in Profile
            // TODO: Report Failure!
            //m_user.GetUserMessageButton().SetTextAsError(kFailedDeleteText);
        }

        Close();

        if (m_appDirector.GetState() != AppDirector.AppState.kProfile)
        {            
            m_appDirector.RequestProfileState();
        }
        else
        {
            m_profile.OpenProfile();
        }

        m_loadingIcon.Hide();
    }
}