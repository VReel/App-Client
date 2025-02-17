﻿using UnityEngine;
using UnityEngine.VR;               //VRSettings
using UnityEngine.UI;
using System.Collections;           //IEnumerator

// AppDirector keeps track of the current state of the App 
// and is in charge of ensuring that work is correctly delegated to other components
// eg. LoginFlow handles login work, and MenuController enables appropriate menu options

using System.Net;

public class AppDirector : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    public enum AppState
    {
        kInit,          // This should only be the state at the very start and never again!
        kExplore,       // User is viewing the public timeline (ie. public or personal timeline)
        kFollowing,     // User is viewing the personal timeline (ie. public or personal timeline)
        kProfile,       // User is viewing the pictures in their own profile
        kSearch,        // User is searching profiles or tags
        kGallery        // User is viewing their 360 photo gallery, they can scroll through all the 360 photos on their phone
    }
        
    [SerializeField] private User m_user;
    [SerializeField] private GameObject m_menuBar;
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private Carousel m_carousel;
    [SerializeField] private Posts m_posts;
    [SerializeField] private Search m_search;
    [SerializeField] private Profile m_profile;
    [SerializeField] private Gallery m_gallery;
    [SerializeField] private LoginFlow m_loginFlow;
    [SerializeField] private ImageLoader m_imageLoader;
    [SerializeField] private KeyBoard m_keyboard;
    [SerializeField] private InternetReachabilityVerifier m_internetReachabilityVerifier;
    [SerializeField] private GameObject m_lostConnectionIcon;

    private AppState m_appState;
    private bool m_overlayShowing; // This is true when for example the Options menu comes on, as we don't want to change AppState entirely
    //private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        m_keyboard.gameObject.SetActive(true); // this is to ensure the keyboard is always switched on even if I switch it off in the editor...
        m_imageSphereController.gameObject.SetActive(true);  // this is to ensure that its always switched on even if I switch it off in the editor...

        //m_coroutineQueue = new CoroutineQueue(this);
        //m_coroutineQueue.StartLoop();       

        m_appState = AppState.kInit;

        m_lostConnectionIcon.SetActive(false);

        m_menuController.RegisterToUseMenuConfig(this);
        m_menuController.GetMenuConfigForOwner(this).imageSpheresVisible = true;
        //m_menuController.UpdateMenuConfig(this); - too early in the execution
    }

    public AppState GetState()
    {
        return m_appState;
    }

    public bool GetOverlayShowing()
    {
        return m_overlayShowing;
    }

    public void SetOverlayShowing(bool overlayShowing)
    {
        m_overlayShowing = overlayShowing;
    }

    public void Update()
    {    
        if (m_appState == AppDirector.AppState.kInit && !m_user.IsLoadingLoginData())
        {
            RequestExploreState();
            m_profile.SetMenuBarProfileDetails();
        }

        //AppDirector changes certain settings based on whether in VR or not!
        if (!VRSettings.enabled)
        {
            Camera.current.fieldOfView = 78;
            m_keyboard.SetInVRMode(false);
        }
        else
        {
            Camera.current.fieldOfView = 90;
            m_keyboard.SetInVRMode(true);
        }
    }
       
    public void RequestExploreState()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: RequestExploreState() called");
        if (m_appState != AppState.kExplore)
        {
            SetExploreState();
        }
    }

    public void RequestFollowingState()
    {
        if (!m_user.IsLoggedIn())
        {
            m_loginFlow.OpenCloseSwitch();
            return;
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: RequestFollowingState() called");
        if (m_appState != AppState.kFollowing)
        {
            SetFollowingState();
        }
    }

    public void RequestSearchState()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: RequestSearchState() called");
        if (m_appState != AppState.kSearch)
        {
            SetSearchState();
        }
    }

    public void RequestProfileState()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: RequestProfileState() called");
        if (m_appState != AppState.kProfile)
        {
            SetProfileState();
        }
    }       

    public void RequestGalleryState()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: RequestGalleryState() called");
        if (m_appState != AppState.kGallery)
        {
            SetGalleryState();
        }
    }

    public void RefreshCurrentState()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: RefreshCurrentState() called");

        if (m_appState == AppDirector.AppState.kExplore)
        {
            SetExploreState();
        }
        else if (m_appState == AppDirector.AppState.kFollowing)
        {
            if (!m_user.IsLoggedIn())
            {
                SetExploreState();
                return;
            }

            SetFollowingState();
        }
        else if (m_appState == AppDirector.AppState.kProfile)
        {
            if (!m_user.IsLoggedIn())
            {
                SetExploreState();
                return;
            }

            SetProfileState();
        }
        else if (m_appState == AppDirector.AppState.kSearch)
        {
            SetSearchState();
        }
        else if (m_appState == AppDirector.AppState.kGallery)
        {
            SetGalleryState();
        }
    }

    public IEnumerator VerifyInternetConnection()
    {
        if (m_internetReachabilityVerifier.status == InternetReachabilityVerifier.Status.NetVerified)
        {
            yield break;
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Lost Internet Connection");

        m_lostConnectionIcon.SetActive(true);

        yield return m_internetReachabilityVerifier.waitForNetVerifiedStatus();

        m_lostConnectionIcon.SetActive(false);

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Got the Internet Connection back!");
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void SetExploreState()
    {
        Resources.UnloadUnusedAssets();
        m_imageSphereController.HideAllImageSpheres();
        m_carousel.ResetPositions();
        m_keyboard.CancelText();

        m_imageLoader.InvalidateLoading();
        m_posts.InvalidateWork();
        m_search.InvalidateWork();
        m_profile.InvalidateWork();
        m_gallery.InvalidateWork();

        m_posts.OpenPublicTimeline();
        m_appState = AppState.kExplore;

        MenuController.MenuConfig menuConfig = m_menuController.GetMenuConfigForOwner(this);
        menuConfig.menuBarVisible = true;
        menuConfig.subMenuVisible = true;
        m_menuController.UpdateMenuConfig(this);
    }

    private void SetFollowingState()
    {
        Resources.UnloadUnusedAssets();
        m_imageSphereController.HideAllImageSpheres();
        m_carousel.ResetPositions();
        m_keyboard.CancelText();

        m_imageLoader.InvalidateLoading();
        m_posts.InvalidateWork();
        m_search.InvalidateWork();
        m_profile.InvalidateWork();
        m_gallery.InvalidateWork();

        m_posts.OpenPersonalTimeline();
        m_appState = AppState.kFollowing;

        MenuController.MenuConfig menuConfig = m_menuController.GetMenuConfigForOwner(this);
        menuConfig.menuBarVisible = true;
        menuConfig.subMenuVisible = true;
        m_menuController.UpdateMenuConfig(this);
    }

    private void SetSearchState()
    {
        Resources.UnloadUnusedAssets();
        m_imageSphereController.HideAllImageSpheres();
        m_carousel.ResetPositions();
        m_keyboard.CancelText();

        m_imageLoader.InvalidateLoading();
        m_posts.InvalidateWork();
        m_search.InvalidateWork();
        m_profile.InvalidateWork();
        m_gallery.InvalidateWork();

        m_search.OpenSearch();
        m_appState = AppState.kSearch;

        MenuController.MenuConfig menuConfig = m_menuController.GetMenuConfigForOwner(this);
        menuConfig.menuBarVisible = true;
        menuConfig.subMenuVisible = true;
        m_menuController.UpdateMenuConfig(this);
    }

    private void SetProfileState()
    {
        Resources.UnloadUnusedAssets();
        m_imageSphereController.HideAllImageSpheres();
        m_carousel.ResetPositions();
        m_keyboard.CancelText();

        m_imageLoader.InvalidateLoading();
        m_posts.InvalidateWork();
        m_search.InvalidateWork();
        m_profile.InvalidateWork();
        m_gallery.InvalidateWork();

        //m_profile.OpenUserProfile(); - ProfileState can either be User or Other's so its not opened through RequestProfileState();
        m_appState = AppState.kProfile;

        MenuController.MenuConfig menuConfig = m_menuController.GetMenuConfigForOwner(this);
        menuConfig.menuBarVisible = false;
        menuConfig.subMenuVisible = true;
        m_menuController.UpdateMenuConfig(this);
    }        

    private void SetGalleryState()
    {
        Resources.UnloadUnusedAssets();
        m_imageSphereController.HideAllImageSpheres();
        m_carousel.ResetPositions();
        m_keyboard.CancelText();

        m_imageLoader.InvalidateLoading();
        m_posts.InvalidateWork();
        m_search.InvalidateWork();
        m_profile.InvalidateWork();
        m_gallery.InvalidateWork();

        m_gallery.OpenAndroidGallery();
        m_appState = AppState.kGallery;

        MenuController.MenuConfig menuConfig = m_menuController.GetMenuConfigForOwner(this);
        menuConfig.menuBarVisible = true;
        menuConfig.subMenuVisible = true;
        m_menuController.UpdateMenuConfig(this);
    }

    private void InvertVREnabled() // Unavailable until Oculus update their SDK...
    {        
        VRSettings.enabled = !VRSettings.enabled;
    }
}