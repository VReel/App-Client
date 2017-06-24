using UnityEngine;
using System;                                           //Serializable
using System.Runtime.Serialization.Formatters.Binary;   //BinaryFormatter
using System.IO;                                        //Filestream, File
using System.Collections;                               //IEnumerator
using mixpanel;                                         //Mixpanel

public class Analytics : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private ImageSkybox m_imageSphereSkybox;

    [Serializable]
    public class AnalyticsData
    {
        public string m_uid {get; set;}
    }

    const string m_vreelDevelopmentAnalyticsFile = "vreelDevelopmentAnalytics.dat";
    const string m_vreelStagingAnalyticsFile = "vreelStagingAnalytics.dat";
    const string m_vreelProductionAnalyticsFile = "vreelProductionAnalytics.dat";
    private string m_vreelAnalyticsFile = "";

    private string m_analyticsFilePath;
    private AnalyticsData m_analyticsData;

    private CoroutineQueue m_coroutineQueue;
    private ThreadJob m_threadJob;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {       
        // Version dependent code
        m_vreelAnalyticsFile = GetSaveFile();
        m_analyticsFilePath = Application.persistentDataPath + m_vreelAnalyticsFile;

        m_analyticsData = new AnalyticsData();
        m_analyticsData.m_uid = "";

        m_threadJob = new ThreadJob(this);

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

    public void AccountDeleted()
    {
        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(AccountDeletedInternal());
    }

    // **************************
    // Private/Helper functions
    // **************************

    private string GetSaveFile()
    {
        if (m_user.GetBackEndEnvironment() == User.BackEndEnvironment.kProduction)
        {
            return m_vreelProductionAnalyticsFile;
        }
        else if (m_user.GetBackEndEnvironment() == User.BackEndEnvironment.kStaging)
        {
            return m_vreelStagingAnalyticsFile;
        }
        else
        {
            return m_vreelDevelopmentAnalyticsFile;
        }
    }
        
    private IEnumerator IdentifyInternal()
    {
        if (File.Exists(m_analyticsFilePath))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (FileStream fileStream = File.Open(m_analyticsFilePath, FileMode.Open))
            {
                m_analyticsData = (AnalyticsData) binaryFormatter.Deserialize(fileStream);
            }

            Mixpanel.Identify(m_analyticsData.m_uid);
        }

        Mixpanel.Track("Opened App");

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Analytics.IdentifyInternal() Identify: " + m_analyticsData.m_uid);

        yield break;
    }

    private IEnumerator SetUID(string uid)
    {
        m_analyticsData.m_uid = uid;       
        yield return SaveAnalyticsData();
    }

    private IEnumerator SaveAnalyticsData()
    {
        yield return m_threadJob.WaitFor();
        bool result = false;
        m_threadJob.Start( () => 
            result = SaveAnalyticsDataToFile()
        );
        yield return m_threadJob.WaitFor(); 
    }

    private bool SaveAnalyticsDataToFile()
    {
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        using (FileStream fileStream = File.Create(m_analyticsFilePath)) // We call Create() to ensure we always overwrite the file
        {
            binaryFormatter.Serialize(fileStream, m_analyticsData);
        }

        return true;
    }

    private IEnumerator LoginSelectedInternal()
    {
        while (!m_user.IsLoggedIn())
        {
            yield return null;
        }

        yield return SetUID(m_user.m_id);
        Mixpanel.Identify(m_analyticsData.m_uid);

        Mixpanel.Track("Logged In");

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Analytics.LoginSelectedInternal() Identify: " + m_analyticsData.m_uid);
    }

    private IEnumerator LogoutSelectedInternal()
    {
        Mixpanel.Track("Logged Out"); 

        System.Guid newGUID = System.Guid.NewGuid();
        yield return SetUID(newGUID.ToString());
        Mixpanel.Identify(m_analyticsData.m_uid);

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Analytics.LogoutSelected(), and new Anonymous GUID set");

        yield break;
    }

    private IEnumerator AccountDeletedInternal()
    {
        Mixpanel.Track("Account Deleted"); 

        System.Guid newGUID = System.Guid.NewGuid();
        yield return SetUID(newGUID.ToString());
        Mixpanel.Identify(m_analyticsData.m_uid);

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Analytics.AccountDeleted(), and new Anonymous GUID set");

        yield break;
    }

    private IEnumerator SignUpSelectedInternal()
    {
        while (!m_user.IsLoggedIn())
        {
            yield return null;
        }

        Mixpanel.Track("New User Signed Up!");

        yield return SetUID(m_user.m_id);
        Mixpanel.Alias(m_analyticsData.m_uid);

        Mixpanel.people.Name = m_user.m_handle;
        Mixpanel.people.Email = m_user.m_email;

        Mixpanel.Track("Post Alias Mixpanel Error!"); // Mixpanel always errorneously reports the first event after an Alias to be by an Anonymous user...

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Analytics.SignUpSelectedInternal() Alias and Identify: " + m_analyticsData.m_uid);
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