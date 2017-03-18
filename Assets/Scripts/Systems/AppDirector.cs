using UnityEngine;
using UnityEngine.VR;               //VRSettings
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
        kLogin,         // User is not yet logged in, they are going through the login flow
        kProfile,       // User is viewing their profile, hence accessing their own folder in the S3 Bucket
        kGallery        // User is viewing their 360 photo gallery, they can scroll through all the 360 photos on their phone
    }

    [SerializeField] private GameObject m_menuBar;
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private AWSS3Client m_AWSS3Client;
    [SerializeField] private DeviceGallery m_deviceGallery;
    [SerializeField] private UserLogin m_userLogin;
    [SerializeField] private InternetReachabilityVerifier m_internetReachabilityVerifier;
    [SerializeField] private GameObject m_lostConnectionIcon;

    private AppState m_appState;
    private CoroutineQueue m_coroutineQueue;

    /*
     * 
     * (need to check if ".NET 2.0" is enough for all the functionality we want to do!)
     * HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create("http://www.contoso.com/");
     * 
     */

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        m_coroutineQueue.EnqueueAction(SetLoginState());

        m_lostConnectionIcon.SetActive(false);
    }

    public AppState GetState()
    {
        return m_appState;
    }

    public void RequestLoginState()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: RequestLoginState() called");
        if (m_appState != AppState.kLogin)
        {
            m_coroutineQueue.EnqueueAction(SetLoginState());
        }
    }

    public void RequestProfileState()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: RequestProfileState() called");
        if (m_appState != AppState.kProfile)
        {
            m_coroutineQueue.EnqueueAction(SetProfileState());
        }
    }

    public void RequestGalleryState()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: RequestGalleryState() called");
        if (m_appState != AppState.kGallery)
        {
            m_coroutineQueue.EnqueueAction(SetGalleryState());
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

    private IEnumerator SetLoginState()
    {        
        DisableAllOptions();
        m_imageSphereController.HideAllImageSpheres();
        SetMenuBar(false);

        m_deviceGallery.InvalidateGalleryImageLoading();
        m_AWSS3Client.InvalidateS3ImageLoading();

        m_menuController.SetLoginSubMenuActive(true);
        m_appState = AppState.kLogin;

        yield break;
    }

    private IEnumerator SetProfileState()
    {
        DisableAllOptions();
        m_imageSphereController.HideAllImageSpheres();
        SetMenuBar(true);

        if (m_appState == AppState.kLogin) // If we are coming from the login screen, set the welcome message
        {
            m_menuController.ShowWelcomeText();
        }

        m_deviceGallery.InvalidateGalleryImageLoading();

        m_menuController.SetProfileSubMenuActive(true);
        m_AWSS3Client.OpenProfile();
        m_appState = AppState.kProfile;

        yield break;
    }

    private IEnumerator SetGalleryState()
    {
        DisableAllOptions();
        m_imageSphereController.HideAllImageSpheres();
        SetMenuBar(true);

        m_AWSS3Client.InvalidateS3ImageLoading();

        m_menuController.SetGallerySubMenuActive(true);
        m_deviceGallery.OpenAndroidGallery();
        m_appState = AppState.kGallery;

        yield break;
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