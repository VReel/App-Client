using UnityEngine;
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
        kLogin,         // User is not yet logged in, they are going through the login flow
        //kHome,          // User is viewing their home (ie. public or personal timeline)
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
    [SerializeField] private Home m_home;
    [SerializeField] private Search m_search;
    [SerializeField] private Profile m_profile;
    [SerializeField] private Gallery m_gallery;
    [SerializeField] private LoginFlow m_loginFlow;
    [SerializeField] private ProfileDetails m_profileDetails;
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
        m_menuController.SetAllSubMenusActive(false);

        m_lostConnectionIcon.SetActive(false);
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
        if (m_appState != AppDirector.AppState.kLogin && !m_user.IsLoggedIn())
        {
            RequestLoginState();
        }
        else if ( (m_appState == AppDirector.AppState.kLogin || m_appState == AppDirector.AppState.kInit) && m_user.IsLoggedIn() && m_user.m_handle.Length > 0)
        {
            RequestExploreState();
            m_profileDetails.SetMenuBarProfileDetails();
        }
    }
        
    public void RequestLoginState()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: RequestLoginState() called");
        if (m_appState != AppState.kLogin)
        {
            SetLoginState();
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

    private void SetLoginState()
    {        
        Resources.UnloadUnusedAssets();
        DisableAllOptions();
        m_imageSphereController.HideAllImageSpheres();
        m_keyboard.CancelText();
        SetMenuBar(false);

        m_imageLoader.InvalidateLoading();
        m_gallery.InvalidateWork();
        m_profile.InvalidateWork();
        m_search.InvalidateWork();
        m_loginFlow.SetLoginFlowPage(0);

        m_appState = AppState.kLogin;
        m_menuController.SetCurrentSubMenuActive(true); // m_menuController.SetLoginSubMenuActive(true);
    }

    private void SetExploreState()
    {
        Resources.UnloadUnusedAssets();
        DisableAllOptions();
        m_imageSphereController.HideAllImageSpheres();
        m_keyboard.CancelText();
        SetMenuBar(true);

        m_imageLoader.InvalidateLoading();
        m_home.InvalidateWork();
        m_search.InvalidateWork();
        m_profile.InvalidateWork();
        m_gallery.InvalidateWork();

        m_home.ShowHomeText();

        m_home.OpenPublicTimeline();
        m_appState = AppState.kExplore;
        m_menuController.SetCurrentSubMenuActive(true); //m_menuController.SetExploreSubMenuActive(true);
    }

    private void SetFollowingState()
    {
        Resources.UnloadUnusedAssets();
        DisableAllOptions();
        m_imageSphereController.HideAllImageSpheres();
        m_keyboard.CancelText();
        SetMenuBar(true);

        m_imageLoader.InvalidateLoading();
        m_home.InvalidateWork();
        m_search.InvalidateWork();
        m_profile.InvalidateWork();
        m_gallery.InvalidateWork();

        m_home.ShowHomeText();

        m_home.OpenPersonalTimeline();
        m_appState = AppState.kFollowing;
        m_menuController.SetCurrentSubMenuActive(true); //m_menuController.SetFollowingSubMenuActive(true);
    }

    private void SetSearchState()
    {
        Resources.UnloadUnusedAssets();
        DisableAllOptions();
        m_imageSphereController.HideAllImageSpheres();
        m_keyboard.CancelText();
        SetMenuBar(true);

        m_imageLoader.InvalidateLoading();
        m_home.InvalidateWork();
        m_search.InvalidateWork();
        m_profile.InvalidateWork();
        m_gallery.InvalidateWork();

        m_search.ShowSearchText();

        m_search.OpenSearch();
        m_appState = AppState.kSearch;
        m_menuController.SetCurrentSubMenuActive(true); //m_menuController.SetSearchSubMenuActive(true);
    }

    private void SetProfileState()
    {
        Resources.UnloadUnusedAssets();
        DisableAllOptions();
        m_imageSphereController.HideAllImageSpheres();
        m_keyboard.CancelText();
        SetMenuBar(true);

        m_imageLoader.InvalidateLoading();
        m_home.InvalidateWork();
        m_search.InvalidateWork();
        m_profile.InvalidateWork();
        m_gallery.InvalidateWork();

        m_profile.ShowProfileText();

        m_profile.OpenProfile();
        m_appState = AppState.kProfile;
        m_menuController.SetCurrentSubMenuActive(true); //m_menuController.SetProfileSubMenuActive(true);
    }        

    private void SetGalleryState()
    {
        Resources.UnloadUnusedAssets();
        DisableAllOptions();
        m_imageSphereController.HideAllImageSpheres();
        m_keyboard.CancelText();
        SetMenuBar(true);

        m_user.GetUserMessageButton().SetIsActiveButton(false);

        m_imageLoader.InvalidateLoading();
        m_home.InvalidateWork();
        m_search.InvalidateWork();
        m_profile.InvalidateWork();
        m_gallery.InvalidateWork();

        m_gallery.ShowGalleryText();

        m_gallery.OpenAndroidGallery();
        m_appState = AppState.kGallery;
        m_menuController.SetCurrentSubMenuActive(true); //m_menuController.SetGallerySubMenuActive(true);
    }

    private void DisableAllOptions()
    {
        m_menuController.SetAllSubMenusActive(false);
    }

    private void SetMenuBar(bool active)
    {
        m_menuBar.SetActive(active);
    }       

    private void InvertVREnabled() // Unavailable until Oculus update their SDK...
    {        
        VRSettings.enabled = !VRSettings.enabled;
    }
}