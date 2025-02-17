﻿using UnityEngine;
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
    [SerializeField] private Profile m_profile;
    [SerializeField] private Search m_search;
    [SerializeField] private LoginFlow m_loginFlow;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private MenuHider m_menuHider;
    [SerializeField] private ListComments m_listComments;
    [SerializeField] private LoadingIcon m_loadingIcon;
    [SerializeField] private KeyBoard m_keyboard;
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
    [SerializeField] private GameObject m_profileImagePage;
    [SerializeField] private GameObject m_galleryImagePage;
    [SerializeField] private GameObject m_galleryCreatePostPage;

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

        m_menuController.RegisterToUseMenuConfig(this);
        MenuController.MenuConfig menuConfig = m_menuController.GetMenuConfigForOwner(this);
        menuConfig.menuBarVisible = false;
        menuConfig.imageSpheresVisible = false;
        menuConfig.subMenuVisible = false;
    }

    public void OpenWithImageSphere(ImageSphere imageSphere)
    {
        m_currImageSphere = imageSphere;

        if (imageSphere.IsSmallImageSphere())
        {
            SetImageFlowPage(9);
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kGallery)
        {
            SetImageFlowPage(10);
        }
        else
        {
            SetImageFlowPage(0);
            SetImageSummary();

            m_listComments.DisplayCommentResults(m_currImageSphere.GetImageIdentifier(), m_currImageSphere);
        }

        m_menuController.UpdateMenuConfig(this);
        m_appDirector.SetOverlayShowing(true);

        m_menuHider.SetMenuVisibility(false);
    }

    public void Close()
    {
        m_imageSubMenu.SetActive(false);

        m_menuController.UpdateMenuConfig(m_appDirector);
        m_appDirector.SetOverlayShowing(false);

        if (m_appDirector.GetState() == AppDirector.AppState.kSearch)
        {
            m_menuController.UpdateMenuConfig(m_search);
        }
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
            m_profileImagePage.SetActive(false);
            m_galleryImagePage.SetActive(false);
            m_galleryCreatePostPage.SetActive(false);
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
            m_profileImagePage.SetActive(false);
            m_galleryImagePage.SetActive(false);
            m_galleryCreatePostPage.SetActive(false);
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
            m_profileImagePage.SetActive(false);
            m_galleryImagePage.SetActive(false);
            m_galleryCreatePostPage.SetActive(false);
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
            m_profileImagePage.SetActive(false);
            m_galleryImagePage.SetActive(false);
            m_galleryCreatePostPage.SetActive(false);
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
            m_profileImagePage.SetActive(false);
            m_galleryImagePage.SetActive(false);
            m_galleryCreatePostPage.SetActive(false);
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
            m_profileImagePage.SetActive(false);
            m_galleryImagePage.SetActive(false);
            m_galleryCreatePostPage.SetActive(false);
        }
        else if (pageNumber == 6) // Options for others's Profile
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
            m_profileImagePage.SetActive(false);
            m_galleryImagePage.SetActive(false);
            m_galleryCreatePostPage.SetActive(false);
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
            m_profileImagePage.SetActive(false);
            m_galleryImagePage.SetActive(false);
            m_galleryCreatePostPage.SetActive(false);
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
            m_profileImagePage.SetActive(false);
            m_galleryImagePage.SetActive(false);
            m_galleryCreatePostPage.SetActive(false);
        }
        else if (pageNumber == 9) // Profile Image
        {
            m_imageSummaryPage.SetActive(false);
            m_commentsPage.SetActive(false);
            m_editCaptionPage.SetActive(false);
            m_newCommentPage.SetActive(false);
            m_updateCommentPage.SetActive(false);
            m_imageOptionsPersonalPage.SetActive(false);
            m_imageOptionsOthersPage.SetActive(false);
            m_deleteImagePage.SetActive(false);
            m_reportImagePage.SetActive(false);
            m_profileImagePage.SetActive(true);
            m_galleryImagePage.SetActive(false);
            m_galleryCreatePostPage.SetActive(false);
        }
        else if (pageNumber == 10) // Gallery Image
        {
            m_imageSummaryPage.SetActive(false);
            m_commentsPage.SetActive(false);
            m_editCaptionPage.SetActive(false);
            m_newCommentPage.SetActive(false);
            m_updateCommentPage.SetActive(false);
            m_imageOptionsPersonalPage.SetActive(false);
            m_imageOptionsOthersPage.SetActive(false);
            m_deleteImagePage.SetActive(false);
            m_reportImagePage.SetActive(false);
            m_profileImagePage.SetActive(false);
            m_galleryImagePage.SetActive(true);
            m_galleryCreatePostPage.SetActive(false);
        }
        else if (pageNumber == 11) // Gallery Create Post
        {
            m_imageSummaryPage.SetActive(false);
            m_commentsPage.SetActive(false);
            m_editCaptionPage.SetActive(false);
            m_newCommentPage.SetActive(false);
            m_updateCommentPage.SetActive(false);
            m_imageOptionsPersonalPage.SetActive(false);
            m_imageOptionsOthersPage.SetActive(false);
            m_deleteImagePage.SetActive(false);
            m_reportImagePage.SetActive(false);
            m_profileImagePage.SetActive(false);
            m_galleryImagePage.SetActive(false);
            m_galleryCreatePostPage.SetActive(true);
        }
    }

    public void OptionsSelected()
    {        
        m_keyboard.AcceptText();
        if (!m_user.IsLoggedIn())
        {
            Close();
            m_loginFlow.OpenCloseSwitch();
            return;
        }

        if (m_currImageSphere.IsLoggedUserImage())
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
        if (!m_user.IsLoggedIn())
        {
            Close();
            m_loginFlow.OpenCloseSwitch();
            return;
        }

        m_currImageSphere.HeartSelectedWithObject(m_heartObject, m_likeCountObject);
    }

    public void LikesSelected()
    {
        if (m_currImageSphere.IsLikesGreaterThanZero())
        {
            SetImageFlowPage(-1);
            m_currImageSphere.LikesSelected();
        }
    }

    public void CommentsSelected()
    {
        SetImageFlowPage(1);
        m_listComments.DisplayCommentResults(m_currImageSphere.GetImageIdentifier(), m_currImageSphere);
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
        if (!m_user.IsLoggedIn())
        {
            Close();
            m_loginFlow.OpenCloseSwitch();
            return;
        }

        SetImageFlowPage(3);
    }
        
    public void UpdateCommentSelected(int commentIndex)
    {
        if ( m_listComments.PreUpdateComment(commentIndex) )
        {
            if (m_listComments.IsCaptionSelected(commentIndex))
            {
                SetImageFlowPage(2);
            }
            else
            {
                SetImageFlowPage(4);
            }
        }
    }

    public void CommentHandleSelected(int commentIndex)
    {
        Close();
        ListComments.CommentResult comment = m_listComments.GetCommentResult(commentIndex);
        m_profile.OpenProfileWithId(comment.userId, comment.userHandle);
    }        
        
    public void DeletePost()
    {
        m_coroutineQueue.EnqueueAction(DeletePostInternal(m_imageSkybox.GetImageIdentifier()));
    }

    public void FlagPost(string flagReason)
    {           
        m_coroutineQueue.EnqueueAction(FlagPostInternal(flagReason));
    }

    public void PreCreatePost()
    {           
        if (!m_user.IsLoggedIn())
        {
            Close();
            m_loginFlow.OpenCloseSwitch();
            return;
        }

        SetImageFlowPage(11);
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
            // TODO: Report Success!
            m_posts.RequestPostRemoval(id);
        }
        else
        {
            // TODO: Report Failure!
        }

        Close();

        m_profile.OpenUserProfile();

        m_loadingIcon.Hide();
    }

    private IEnumerator FlagPostInternal(string flagReason)
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_loadingIcon.Display();

        yield return m_backEndAPI.Flag_FlagPost(m_currImageSphere.GetImageIdentifier(), flagReason);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            //TODO: REPORT ON SUCCESS
        } 
        else
        {
            //TODO: REPORT ON FAILURE
        }

        SetImageFlowPage(0);

        m_loadingIcon.Hide();
    }
}