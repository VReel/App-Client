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
        Mixpanel.Track("User's Own Profile Opened");
    }

    public void ExploreSelected()
    {
        Mixpanel.Track("Explore Tab Opened");
    }

    public void FollowingSelected()
    {
        Mixpanel.Track("Following Tab Opened");
    }

    public void SearchSelected()
    {
        Mixpanel.Track("Search Tab Opened");
    }             

    public void GallerySelected()
    {
        Mixpanel.Track("Gallery Tab Opened");
    }

    public void OptionsSelected()
    {
        Mixpanel.Track("Options Opened");
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
        Mixpanel.Track("New User Began Signup");
    }

    public void SignUpSelected()
    {
        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(SignUpSelectedInternal());
    }

    public void SearchForProfileSelected()
    {
        Mixpanel.Track("Began Searching For User Profile");
    }

    public void SearchForTagSelected()
    {
        Mixpanel.Track("Began Searching For Tag");
    }       

    public void ImageSphereSelected(int sphereNumber)
    {
        var properties = new Value();
        properties["SphereNumber"] = sphereNumber;
        SetAppState(properties);

        Mixpanel.Track("Post Image Selected For Viewing", properties);
    }

    public void HandleSelected(int sphereNumber)
    {
        var properties = new Value();
        properties["SphereNumber"] = sphereNumber;
        SetAppState(properties);

        Mixpanel.Track("Profile Opened From Post", properties);
    }

    public void HeartSelected(int sphereNumber)
    {
        var properties = new Value();
        properties["SphereNumber"] = sphereNumber;
        SetAppState(properties);

        Mixpanel.Track("Post Liked", properties);
    }

    public void LikeSelected(int sphereNumber)
    {
        var properties = new Value();
        properties["SphereNumber"] = sphereNumber;
        SetAppState(properties);

        Mixpanel.Track("Post Likes Opened", properties);
    }

    public void CaptionSelected(int sphereNumber)
    {
        var properties = new Value();
        properties["SphereNumber"] = sphereNumber;
        SetAppState(properties);

        Mixpanel.Track("Caption Selected For Editing", properties);
    }

    public void CommentSelected(int sphereNumber)
    {
        var properties = new Value();
        properties["SphereNumber"] = sphereNumber;
        SetAppState(properties);

        Mixpanel.Track("Post Comments Opened", properties);
    }

    public void FollowSelected()
    {
        var properties = new Value();
        SetAppState(properties);

        Mixpanel.Track("Began Following User", properties);
    }

    public void CommentUploaded()
    {
        var properties = new Value();
        SetAppState(properties);

        Mixpanel.Track("New Comment Added To Post", properties);
    }

    public void PreviousArrowSelected()
    {
        var properties = new Value();
        SetAppState(properties);

        Mixpanel.Track("Previous Posts Selected", properties);
    }

    public void NextArrowSelected()
    {
        var properties = new Value();
        SetAppState(properties);

        Mixpanel.Track("Next Posts Selected", properties);
    }

    public void ImageUploaded()
    {
        var properties = new Value();
        if (m_imageSphereSkybox.IsTextureValid())
        {
            properties["TextureWidth"] = m_imageSphereSkybox.GetTexture().width;
            properties["TextureHeight"] = m_imageSphereSkybox.GetTexture().height;
        }

        Mixpanel.Track("Created New Post", properties);
    }
        
    public void ProfileImageUploaded()
    {
        var properties = new Value();
        if (m_imageSphereSkybox.IsTextureValid())
        {
            properties["TextureWidth"] = m_imageSphereSkybox.GetTexture().width;
            properties["TextureHeight"] = m_imageSphereSkybox.GetTexture().height;
        }

        Mixpanel.Track("Updated Profile Picture", properties);
    }

    public void ImageDeleted()
    {
        Mixpanel.Track("Deleted Post");
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

        Mixpanel.Track("Opened App");

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Analytics.IdentifyInternal() Identify: " + m_user.m_id);
    }

    private IEnumerator LoginSelectedInternal()
    {
        while (!m_user.IsUserDataStored())
        {
            yield return null;
        }

        Mixpanel.Identify(m_user.m_id);

        Mixpanel.Track("Logged In");

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Analytics.LoginSelectedInternal() Identify: " + m_user.m_id);
    }

    private IEnumerator LogoutSelectedInternal()
    {
        Mixpanel.Track("Logged Out"); 

        /*
        Mixpanel.FlushQueue();

        yield return new WaitForSeconds(1);
        while (m_user.IsUserDataStored())
        {
            yield return null;
        }
        */

        Mixpanel.Reset();

        //Mixpanel.FlushQueue();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Analytics.LogoutSelected(), and Mixpanel.Reset() Called ");

        yield break;
    }

    private IEnumerator SignUpSelectedInternal()
    {
        while (!m_user.IsUserDataStored())
        {
            yield return null;
        }
            
        Mixpanel.Alias(m_user.m_id);
        //Mixpanel.Identify(m_user.m_id);

        Mixpanel.people.Name = m_user.m_handle;
        Mixpanel.people.Email = m_user.m_email;

        Mixpanel.Track("New User Signed Up!");

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
        else if (m_appDirector.GetState() == AppDirector.AppState.kProfile)
        {
            properties["AppState"] = "Profile";
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kGallery)
        {
            properties["AppState"] = "Gallery";
        }
    }
}