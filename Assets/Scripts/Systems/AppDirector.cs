using UnityEngine;
using UnityEngine.VR; //VRSettings

// AppDirector keeps track of the current state of the App 
// and is in charge of ensuring that work is correctly delegated to other components
// eg. LoginFlow handles login work, and MenuController enables appropriate menu options

public class AppDirector : MonoBehaviour 
{   
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private GameObject m_imageSpheres;
    [SerializeField] private GameObject m_menuBar;
    [SerializeField] private AWSS3Client m_AWSS3Client;
    [SerializeField] private DeviceGallery m_deviceGallery;
    [SerializeField] private UserLogin m_userLogin;

    private AppState m_appState;

    public enum AppState
    {
        kLogin,         // User is not yet logged in, they are going through the login flow
        kProfile,       // User is viewing their profile, hence accessing their own folder in the S3 Bucket
        kGallery        // User is viewing their 360 photo gallery, they can scroll through all the 360 photos on their phone
    }

    public void Start()
    {
        m_appState = AppState.kLogin;

        SetLoginState();
    }

    public AppState GetState()
    {
        return m_appState;
    }

    public void SetLoginState()
    {        
        DisableAllOptions();
        SetImageSpheres(false);
        SetMenuBar(false);

        m_menuController.SetLoginSubMenuActive(true);
        m_appState = AppState.kLogin;
    }

    public void SetProfileState()
    {
        DisableAllOptions();
        SetImageSpheres(true);
        SetMenuBar(true);

        if (m_appState == AppState.kLogin) // If we are coming from the login screen, set the welcome message
        {
            m_menuController.ShowWelcomeText();
        }

        m_deviceGallery.InvalidateGalleryPictureLoading();

        m_menuController.SetProfileSubMenuActive(true);
        m_AWSS3Client.DownloadAllImages();
        m_appState = AppState.kProfile;
    }

    public void SetGalleryState()
    {
        DisableAllOptions();
        SetImageSpheres(true);
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

    private void SetImageSpheres(bool active)
    {
        m_imageSpheres.SetActive(active);
    }

    private void SetMenuBar(bool active)
    {
        m_menuBar.SetActive(active);
    }

    private void InvertVREnabled() // Unsuable until Oculus update their SDK...
    {        
        VRSettings.enabled = !VRSettings.enabled;
    }
}