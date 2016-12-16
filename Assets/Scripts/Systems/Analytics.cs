using UnityEngine;
using Amazon;
using Amazon.CognitoIdentity;           // CognitoAWSCredentials
using Amazon.MobileAnalytics.MobileAnalyticsManager; // MobileAnalyticsManager

public class Analytics : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private UserLogin m_userLogin;
    [SerializeField] private ImageSkybox m_imageSphereSkybox;

    private MobileAnalyticsManager m_analyticsManager;

    // **************************
    // Public functions
    // **************************

    public void Start() 
    {                
        //TODO: Move this pool over to EUWest1!
        var credentials = new CognitoAWSCredentials(
            "us-east-1:76e86965-28da-4906-bf7d-ed48c4e50477", // Amazon Cognito Identity Pool ID
            RegionEndpoint.USEast1 // Cognito Identity Region
        ); 
        
        m_analyticsManager = MobileAnalyticsManager.GetOrCreateInstance(
            "410c9ef3e9d94a74afaa2d5bb96426f9", // Amazon Mobile Analytics App ID
            credentials,
            RegionEndpoint.USEast1 // Cognito Identity Region
        ); 
    }

    public void ProfileSelected()
    {
        CustomEvent customEvent = new CustomEvent("ProfileSelected");
        customEvent.AddAttribute("CognitoID", m_userLogin.GetCognitoUserID());

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void GallerySelected()
    {
        CustomEvent customEvent = new CustomEvent("GallerySelected");
        customEvent.AddAttribute("CognitoID", m_userLogin.GetCognitoUserID());

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void LoginSelected()
    {
        CustomEvent customEvent = new CustomEvent("LoginSelected");
        customEvent.AddAttribute("CognitoID", m_userLogin.GetCognitoUserID());

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void LogoutSelected()
    {
        CustomEvent customEvent = new CustomEvent("LogoutSelected");
        customEvent.AddAttribute("CognitoID", m_userLogin.GetCognitoUserID());

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void ImageSphereSelected(int sphereNumber)
    {
        CustomEvent customEvent = new CustomEvent("ImageSphereSelected");
        customEvent.AddAttribute("CognitoID", m_userLogin.GetCognitoUserID());

        if (m_appDirector.GetState() == AppDirector.AppState.kProfile)
        {
            customEvent.AddAttribute("AppState", "Profile");
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kGallery)
        {
            customEvent.AddAttribute("AppState", "Gallery");
        }

        customEvent.AddMetric("SphereNumber", sphereNumber);

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void PreviousArrowSelected()
    {
        CustomEvent customEvent = new CustomEvent("PreviousArrowSelected");
        customEvent.AddAttribute("CognitoID", m_userLogin.GetCognitoUserID());

        if (m_appDirector.GetState() == AppDirector.AppState.kProfile)
        {
            customEvent.AddAttribute("AppState", "Profile");
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kGallery)
        {
            customEvent.AddAttribute("AppState", "Gallery");
        }

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void NextArrowSelected()
    {
        CustomEvent customEvent = new CustomEvent("NextArrowSelected");
        customEvent.AddAttribute("CognitoID", m_userLogin.GetCognitoUserID());

        if (m_appDirector.GetState() == AppDirector.AppState.kProfile)
        {
            customEvent.AddAttribute("AppState", "Profile");
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kGallery)
        {
            customEvent.AddAttribute("AppState", "Gallery");
        }

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void ImageUploaded()
    {
        //TODO: Split this into request and success/failure
        
        CustomEvent customEvent = new CustomEvent("ImageUploaded");
        customEvent.AddAttribute("CognitoID", m_userLogin.GetCognitoUserID());

        if (m_imageSphereSkybox.IsTextureValid())
        {
            customEvent.AddMetric("TextureWidth", m_imageSphereSkybox.GetTexture().width);
            customEvent.AddMetric("TextureHeight", m_imageSphereSkybox.GetTexture().height);
        }

        m_analyticsManager.RecordEvent(customEvent);
    }

    // **************************
    // Private/Helper functions
    // **************************

    // You want this session management code only in one game object
    // that persists through the game life cycles using “DontDestroyOnLoad (transform.gameObject);”
    private void OnApplicationFocus(bool focus) 
    {
        if (m_analyticsManager != null) 
        {
            if (focus) 
            {
                m_analyticsManager.ResumeSession();
            } 
            else 
            {    
                m_analyticsManager.PauseSession();
            }
        }
    }
}