using UnityEngine;
using System.Collections;               // IEnumerator
using mixpanel;                         // Mixpanel

public class Analytics : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private ImageSkybox m_imageSphereSkybox;

    private CoroutineQueue m_coroutineQueue;
    private string kEmptyString = "";

    // **************************
    // Public functions
    // **************************

    public void Start()
    {       
        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        m_coroutineQueue.EnqueueAction(IdentifyInternal());
    }

    public void ProfileSelected()
    {     
        Mixpanel.Track("ProfileSelected");
    }

    public void ExploreSelected()
    {
        Mixpanel.Track("ExploreSelected");
    }

    public void FollowingSelected()
    {
        Mixpanel.Track("FollowingSelected");
    }

    public void SearchSelected()
    {
        Mixpanel.Track("FollowingSelected");
    }             

    public void GallerySelected()
    {
        Mixpanel.Track("GallerySelected");
    }

    public void OptionsSelected()
    {
        Mixpanel.Track("OptionsSelected");
    }

    public void LoginSelected()
    {
        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(LoginSelectedInternal());
    }        

    public void LogoutSelected()
    {
        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(LogoutSelectedInternal());
    }

    public void PreSignUpSelected()
    {
        Mixpanel.Track("PreSignUpSelected");
    }

    public void SignUpSelected()
    {
        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(SignUpSelectedInternal());
    }

    public void PublicTimelineSelected()
    {
        Mixpanel.Track("PublicTimelineSelected");
    }

    public void PersonalTimelineSelected()
    {
        Mixpanel.Track("PersonalTimelineSelected");
    }

    public void SearchForProfileSelected()
    {
        Mixpanel.Track("SearchForProfileSelected");
    }

    public void SearchForTagSelected()
    {
        Mixpanel.Track("SearchForTagSelected");
    }       

    public void ImageSphereSelected(int sphereNumber)
    {
        var properties = new Value();
        properties["SphereNumber"] = sphereNumber;
        SetAppState(properties);

        Mixpanel.Track("ProfileSelected", properties);
    }

    public void HandleSelected(int sphereNumber)
    {
        var properties = new Value();
        properties["SphereNumber"] = sphereNumber;
        SetAppState(properties);

        Mixpanel.Track("HandleSelected", properties);
    }

    public void HeartSelected(int sphereNumber)
    {
        var properties = new Value();
        properties["SphereNumber"] = sphereNumber;
        SetAppState(properties);

        Mixpanel.Track("HeartSelected", properties);
    }

    public void LikeSelected(int sphereNumber)
    {
        var properties = new Value();
        properties["SphereNumber"] = sphereNumber;
        SetAppState(properties);

        Mixpanel.Track("LikeSelected", properties);
    }

    public void CaptionSelected(int sphereNumber)
    {
        var properties = new Value();
        properties["SphereNumber"] = sphereNumber;
        SetAppState(properties);

        Mixpanel.Track("CaptionSelected", properties);
    }

    public void FollowSelected()
    {
        var properties = new Value();
        SetAppState(properties);

        Mixpanel.Track("FollowSelected", properties);
    }

    public void CommentUploaded()
    {
        var properties = new Value();
        SetAppState(properties);

        Mixpanel.Track("CommentUploaded", properties);
    }

    public void PreviousArrowSelected()
    {
        var properties = new Value();
        SetAppState(properties);

        Mixpanel.Track("PreviousArrowSelected", properties);
    }

    public void NextArrowSelected()
    {
        var properties = new Value();
        SetAppState(properties);

        Mixpanel.Track("NextArrowSelected", properties);
    }

    public void ImageUploaded()
    {
        var properties = new Value();
        if (m_imageSphereSkybox.IsTextureValid())
        {
            properties["TextureWidth"] = m_imageSphereSkybox.GetTexture().width;
            properties["TextureHeight"] = m_imageSphereSkybox.GetTexture().height;
        }

        Mixpanel.Track("ImageUploaded", properties);
    }
        
    public void ProfileImageUploaded()
    {
        var properties = new Value();
        if (m_imageSphereSkybox.IsTextureValid())
        {
            properties["TextureWidth"] = m_imageSphereSkybox.GetTexture().width;
            properties["TextureHeight"] = m_imageSphereSkybox.GetTexture().height;
        }

        Mixpanel.Track("ProfileImageUploaded", properties);
    }

    public void ImageDeleted()
    {
        Mixpanel.Track("ImageDeleted");
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator IdentifyInternal()
    {
        while (!m_user.IsUserDataStored())
        {
            yield return null;
        }

        Mixpanel.Identify(m_user.m_id);

        Mixpanel.Track("OpenedApp");

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Analytics.IdentifyInternal() Identify: " + m_user.m_id);
    }

    private IEnumerator LoginSelectedInternal()
    {
        while (!m_user.IsUserDataStored())
        {
            yield return null;
        }

        Mixpanel.Identify(m_user.m_id);

        Mixpanel.Track("LoginSelected");

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Analytics.LoginSelectedInternal() Identify: " + m_user.m_id);
    }

    private IEnumerator LogoutSelectedInternal()
    {
        Mixpanel.Track("LogoutSelected"); 

        //Mixpanel.FlushQueue();

        yield return new WaitForSeconds(1);
        while (m_user.IsUserDataStored())
        {
            yield return null;
        }

        //Mixpanel.Reset();

        //Mixpanel.FlushQueue();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Analytics.LogoutSelected(), and Mixpanel.Reset() Called ");
    }

    private IEnumerator SignUpSelectedInternal()
    {
        while (!m_user.IsUserDataStored())
        {
            yield return null;
        }
            
        Mixpanel.Alias(m_user.m_id);
        Mixpanel.Identify(m_user.m_id);

        Mixpanel.people.Name = m_user.m_handle;
        Mixpanel.people.Email = m_user.m_email;

        Mixpanel.Track("SignUpSelected");

        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Analytics.SignUpSelectedInternal() CurrentDistinctID AFTER: " + mixpanel.platform.MixpanelUnityPlatform.get_distinct_id());
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Analytics.SignUpSelectedInternal() Alias and Identify: " + m_user.m_id);
    }      

    /*
    private void Identify()
    {
        if (m_user.m_id != null && m_user.m_id.CompareTo(kEmptyString) != 0)
        {
            Mixpanel.Identify(m_user.m_id);
        }
    }
    */

    private void SetAppState(Value properties)
    {
        if (m_appDirector.GetState() == AppDirector.AppState.kExplore)
        {
            properties["AppState"] = "Explore";
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kFollowing)
        {
            properties["AppState"] = "Following";
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kSearch)
        {
            properties["AppState"] = "Search";
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kGallery)
        {
            properties["AppState"] = "Profile";
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kGallery)
        {
            properties["AppState"] = "Gallery";
        }
    }
}