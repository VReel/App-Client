using UnityEngine;
using UnityEngine.VR; //VRSettings

// AppDirector keeps track of the current state of the App 
// and is in charge of ensuring that work is correctly delegated to other components
// eg. LoginFlow handles login work, and MenuController enables appropriate menu options

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

    private AppState m_appState;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        m_appState = AppState.kLogin;

        SetLoginState();
    }

    public AppState GetState()
    {
        return m_appState;
    }

    public void RequestLoginState()
    {
        if (m_appState != AppState.kLogin)
        {
            SetLoginState();
        }
    }

    public void RequestProfileState()
    {
        if (m_appState != AppState.kProfile)
        {
            SetProfileState();
        }
    }

    public void RequestGalleryState()
    {
        if (m_appState != AppState.kGallery)
        {
            SetGalleryState();
        }
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void SetLoginState()
    {        
        DisableAllOptions();
        m_imageSphereController.HideAllImageSpheres();
        SetMenuBar(false);

        m_menuController.SetLoginSubMenuActive(true);
        m_appState = AppState.kLogin;
    }

    private void SetProfileState()
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
    }

    private void SetGalleryState()
    {
        DisableAllOptions();
        m_imageSphereController.HideAllImageSpheres();
        SetMenuBar(true);

        m_AWSS3Client.InvalidateS3ImageLoading();

        m_menuController.SetGallerySubMenuActive(true);
        m_deviceGallery.OpenAndroidGallery();
        m_appState = AppState.kGallery;
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