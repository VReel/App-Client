using UnityEngine;
using System.Collections;               // IEnumerator
using mixpanel;                         // Mixpanel

//TODO: Add Mixpanel.Identify() and Mixpanel.Alias()
public class Analytics : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private ImageSkybox m_imageSphereSkybox;

    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {       
        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();
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
        m_coroutineQueue.EnqueueAction(LoginSelectedInternal());
    }

    public void LogoutSelected()
    {
        Mixpanel.Track("LogoutSelected");
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

    private IEnumerator LoginSelectedInternal()
    {
        while (!m_user.IsLoggedIn())
        {
            yield return null;
        }

        // If this does not have any properties, then it should not be in a coroutine!

        Mixpanel.Track("LoginSelected");
    }        

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