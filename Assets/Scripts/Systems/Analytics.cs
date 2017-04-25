using UnityEngine;
using Amazon;
using Amazon.CognitoIdentity;           // CognitoAWSCredentials
using Amazon.MobileAnalytics.MobileAnalyticsManager; // MobileAnalyticsManager
using System.Collections;               // IEnumerator

public class Analytics : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private ImageSkybox m_imageSphereSkybox;

    private MobileAnalyticsManager m_analyticsManager;
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {                
        UnityInitializer.AttachToGameObject(this.gameObject);

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

        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();
    }

    public void HomeSelected()
    {
        CustomEvent customEvent = new CustomEvent("HomeSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void SearchSelected()
    {
        CustomEvent customEvent = new CustomEvent("SearchSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void ProfileSelected()
    {
        CustomEvent customEvent = new CustomEvent("ProfileSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        m_analyticsManager.RecordEvent(customEvent);
    }        

    public void GallerySelected()
    {
        CustomEvent customEvent = new CustomEvent("GallerySelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void OptionsSelected()
    {
        CustomEvent customEvent = new CustomEvent("OptionsSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void LoginSelected()
    {
        m_coroutineQueue.EnqueueAction(LoginSelectedInternal());
    }

    public void LogoutSelected()
    {
        CustomEvent customEvent = new CustomEvent("LogoutSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void PublicTimelineSelected()
    {
        CustomEvent customEvent = new CustomEvent("PublicTimelineSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void PersonalTimelineSelected()
    {
        CustomEvent customEvent = new CustomEvent("PersonalTimelineSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void SearchForProfileSelected()
    {
        CustomEvent customEvent = new CustomEvent("SearchForProfileSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void SearchForTagSelected()
    {
        CustomEvent customEvent = new CustomEvent("SearchForTagSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        m_analyticsManager.RecordEvent(customEvent);
    }       

    public void ImageSphereSelected(int sphereNumber)
    {
        CustomEvent customEvent = new CustomEvent("ImageSphereSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        SetAppState(customEvent);

        customEvent.AddAttribute("SphereNumber", sphereNumber.ToString());

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void HandleSelected(int sphereNumber)
    {
        CustomEvent customEvent = new CustomEvent("HandleSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        SetAppState(customEvent);

        customEvent.AddAttribute("SphereNumber", sphereNumber.ToString());

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void HeartSelected(int sphereNumber)
    {
        CustomEvent customEvent = new CustomEvent("HeartSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        SetAppState(customEvent);

        customEvent.AddAttribute("SphereNumber", sphereNumber.ToString());

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void LikeSelected(int sphereNumber)
    {
        CustomEvent customEvent = new CustomEvent("LikeSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        SetAppState(customEvent);

        customEvent.AddAttribute("SphereNumber", sphereNumber.ToString());

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void FollowSelected()
    {
        CustomEvent customEvent = new CustomEvent("FollowSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        SetAppState(customEvent);

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void PreviousArrowSelected()
    {
        CustomEvent customEvent = new CustomEvent("PreviousArrowSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        SetAppState(customEvent);

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void NextArrowSelected()
    {
        CustomEvent customEvent = new CustomEvent("NextArrowSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        SetAppState(customEvent);

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void ImageUploaded()
    {
        CustomEvent customEvent = new CustomEvent("ImageUploaded");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        if (m_imageSphereSkybox.IsTextureValid())
        {
            customEvent.AddMetric("TextureWidth", m_imageSphereSkybox.GetTexture().width);
            customEvent.AddMetric("TextureHeight", m_imageSphereSkybox.GetTexture().height);
        }

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void ProfileImageUploaded()
    {
        CustomEvent customEvent = new CustomEvent("ProfileImageUploaded");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        if (m_imageSphereSkybox.IsTextureValid())
        {
            customEvent.AddMetric("TextureWidth", m_imageSphereSkybox.GetTexture().width);
            customEvent.AddMetric("TextureHeight", m_imageSphereSkybox.GetTexture().height);
        }

        m_analyticsManager.RecordEvent(customEvent);
    }

    public void ImageDeleted()
    {
        CustomEvent customEvent = new CustomEvent("ImageDeleted");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        m_analyticsManager.RecordEvent(customEvent);
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator LoginSelectedInternal()
    {
        while (!m_user.IsLoggedIn())
        {
            yield return null;
        }

        CustomEvent customEvent = new CustomEvent("LoginSelected");
        customEvent.AddAttribute("UserEmail", m_user.m_email);

        m_analyticsManager.RecordEvent(customEvent);
    }

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

    private void SetAppState(CustomEvent customEvent)
    {
        if (m_appDirector.GetState() == AppDirector.AppState.kHome)
        {
            customEvent.AddAttribute("AppState", "Home");
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kSearch)
        {
            customEvent.AddAttribute("AppState", "Search");
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kGallery)
        {
            customEvent.AddAttribute("AppState", "Profile");
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kGallery)
        {
            customEvent.AddAttribute("AppState", "Gallery");
        }
    }
}