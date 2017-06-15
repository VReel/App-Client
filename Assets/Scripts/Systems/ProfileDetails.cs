using UnityEngine;
using UnityEngine.UI;           //Text
using System.Collections;       //IEnumerator

//TODO: Merge this into Profile.cs...
public class ProfileDetails : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private Posts m_posts;
    [SerializeField] private ListUsers m_listUsers;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private ImageLoader m_imageLoader;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private LoadingIcon m_loadingIcon;
    [SerializeField] private GameObject m_menuBarProfileButtonObject;
    [SerializeField] private GameObject m_profileDetailsTopLevel;
    [SerializeField] private GameObject m_editProfileButtonObject;
    [SerializeField] private GameObject m_followButtonObject;
    [SerializeField] private GameObject m_handleObject;
    [SerializeField] private GameObject m_followerCountObject;
    [SerializeField] private GameObject m_followingCountObject;
    [SerializeField] private GameObject m_profileDescriptionObject;
    [SerializeField] private GameObject m_profileDescriptionUpdateTopLevel;
    [SerializeField] private GameObject m_profileHandleNewText;
    [SerializeField] private GameObject m_profileDescriptionNewText;
    [SerializeField] private GameObject m_newUserText;

    private string m_userId;
    private string m_handle;
    private string m_name;
    private int m_followerCount;
    private int m_followingCount;
    private int m_postCount;
    private string m_email;
    private string m_profileDescription;
    private bool m_followedByMe;
    private string m_thumbnailUrl;
    private string m_originalUrl;

    private string m_loggedUserThumbnailUrl;
    private string m_loggedUserOriginalUrl;

    private BackEndAPI m_backEndAPI;
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();

        m_backEndAPI = new BackEndAPI(this, m_user.GetErrorMessage(), m_user);

        m_menuController.RegisterToUseMenuConfig(this);
        MenuController.MenuConfig menuConfig = m_menuController.GetMenuConfigForOwner(this);
        menuConfig.menuBarVisible = true;
        menuConfig.subMenuVisible = true;
        m_menuController.UpdateMenuConfig(this);
    }

    public void Update() //TODO Make this event based instead...
    {
        bool noImagesUploaded = m_posts.GetNumPosts() <= 0 && !m_loadingIcon.IsDisplaying() && m_user.IsCurrentUser(m_userId);
        m_newUserText.SetActive(noImagesUploaded); // If the user has yet to upload any images then show them the New User Text!
    }

    public string GetUserId()
    {
        return m_userId;
    }

    public bool IsUser(string userId)
    {
        return (m_userId != null && userId != null && m_userId.CompareTo(userId) == 0);
    }

    public bool IsLoggedUser(string userId)
    {
        return (userId != null && m_user.m_id.CompareTo(userId) == 0);
    }

    public void SetMenuBarProfileDetails()
    {
        m_menuBarProfileButtonObject.GetComponentInChildren<Text>().text = m_user.m_handle;
        m_coroutineQueue.EnqueueAction(SetMenuBarProfileDetailsInternal());
    }

    public void OpenProfile() // TODO: Improve the fact that there's OpenProfile()/ OpenUserProfile() / OpenProfileWithId()
    {
        if (m_user.IsCurrentUser(m_userId))
        {
            m_posts.OpenUserProfile();
        }
        else
        {
            m_posts.OpenProfileWithID(m_userId, m_handle);
        }
    }

    public void OpenUserProfile()
    {
        m_userId = m_user.m_id;
        m_handle = m_user.m_handle;

        m_appDirector.RequestProfileState();
    }

    public void OpenProfileWithId(string userId, string handle)
    {
        m_userId = userId;
        m_handle = handle;

        m_appDirector.RequestProfileState();
    }

    public void OpenProfileDetails(string userId)
    {
        m_userId = userId;

        m_profileDetailsTopLevel.SetActive(true);
        m_profileDescriptionUpdateTopLevel.SetActive(false);

        bool isCurrentUser = m_user.IsCurrentUser(m_userId);
        m_followButtonObject.SetActive(!isCurrentUser);
        m_editProfileButtonObject.SetActive(isCurrentUser);

        m_handleObject.GetComponentInChildren<Text>().text = "";
        m_followerCountObject.GetComponentInChildren<Text>().text = "";
        m_followingCountObject.GetComponentInChildren<Text>().text = "";
        m_profileDescriptionObject.GetComponentInChildren<Text>().text = "";

        m_coroutineQueue.EnqueueAction(GetUserDetails());

        m_listUsers.CloseListUsers();
    }

    public void DisplayFollowers()
    {
        if (m_followerCount > 0)
        {
            m_listUsers.DisplayFollowersResults(m_userId);
        }
    }

    public void DisplayFollowing()
    {
        if (m_followingCount > 0)
        {
            m_listUsers.DisplayFollowingResults(m_userId);
        }
    }

    public void FollowSelected()
    {
        m_followedByMe = !m_followedByMe;
        m_followButtonObject.GetComponentInChildren<FollowButton>().FollowOnOffSwitch(m_followedByMe);
        FollowOrUnfollowUser(m_userId, m_followedByMe);

        Text textObject = m_followerCountObject.GetComponentInChildren<Text>();
        int followers = System.Convert.ToInt32(textObject.text);
        followers = m_followedByMe ? followers+1 : followers-1;
        textObject.text = followers.ToString();
    }       

    public void PreUpdateProfileDescription()
    {
        bool isCurrentUser = m_user.IsCurrentUser(m_userId);
        if (isCurrentUser)
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreUpdateProfileDescription() called");

            //TODO: Improve this - such that there's less need to set so many things to active/inactive
            m_profileDetailsTopLevel.SetActive(false);
            m_profileDescriptionUpdateTopLevel.SetActive(true);
            m_profileHandleNewText.GetComponentInChildren<Text>().text = m_handle;
            m_profileDescriptionNewText.GetComponentInChildren<Text>().text = m_profileDescription;

            MenuController.MenuConfig menuConfig = m_menuController.GetMenuConfigForOwner(this);
            menuConfig.menuBarVisible = false;
            menuConfig.imageSpheresVisible = false;
            m_menuController.UpdateMenuConfig(this);
            m_appDirector.SetOverlayShowing(true);
        }            
    }

    public void CancelUpdateProfileDescription()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: CancelUpdateProfileDescription() called");

        //TODO: Improve this - such that there's less need to set so many things to active/inactive
        m_profileDetailsTopLevel.SetActive(true);
        m_profileDescriptionUpdateTopLevel.SetActive(false);

        MenuController.MenuConfig menuConfig = m_menuController.GetMenuConfigForOwner(this);
        menuConfig.menuBarVisible = false;
        menuConfig.imageSpheresVisible = true;
        m_menuController.UpdateMenuConfig(this);
        m_appDirector.SetOverlayShowing(false);
    }

    public void AcceptUpdateProfileDescription()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: AcceptUpdateProfileDescription() called");

        m_coroutineQueue.EnqueueAction(UpdateProfileDescriptionInternal());
    }

    public void DownloadOriginalImage()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: DownloadOriginalImage() called");

        m_coroutineQueue.EnqueueAction(DownloadOriginalImageInternal());
    }

    public void DownloadMenuBarOriginalImage()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: DownloadMenuBarOriginalImage() called");

        m_coroutineQueue.EnqueueAction(DownloadMenuBarOriginalImageInternal());
    }

    public void CloseProfile()
    {
        //TODO: Do something more intelligent in order to not lose the state you were in...
        m_imageSphereController.HideSphereAtIndex(Helper.kProfilePageSphereIndex, true); // True tells it to ForceHide
        m_appDirector.RequestExploreState();
    }
        
    public void InvalidateWork() // This function is called in order to stop any ongoing work
    {        
        m_posts.InvalidateWork();
        if (m_coroutineQueue != null)
        {
            m_coroutineQueue.Clear();
        }
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator SetMenuBarProfileDetailsInternal()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: SetMenuBarProfileDetailsInternal() called");

        yield return m_appDirector.VerifyInternetConnection();

        yield return RefreshLoggedProfileData();

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            DownloadMenuBarThumbnailImage();
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - Unable to load logged user Profile Details!");
        }
    }

    private IEnumerator GetUserDetails()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: GetUserDetails() called");

        yield return m_appDirector.VerifyInternetConnection();

        yield return RefreshProfileData();

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            m_handleObject.GetComponentInChildren<Text>().text = m_handle; 
            m_followerCountObject.GetComponentInChildren<Text>().text = m_followerCount.ToString(); 
            m_followingCountObject.GetComponentInChildren<Text>().text = m_followingCount.ToString(); 
            m_profileDescriptionObject.GetComponentInChildren<Text>().text = m_profileDescription; 

            m_followButtonObject.GetComponentInChildren<FollowButton>().FollowOnOffSwitch(m_followedByMe);

            DownloadThumbnailImage();
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - Unable to load User Profile Details!");
        }
    }

    private void DownloadThumbnailImage()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: DownloadThumbnailImage() loading User Thumbnail Image for: " + m_thumbnailUrl);

        if (m_thumbnailUrl != null && m_thumbnailUrl.Length > 0)
        {
            m_imageLoader.LoadImageFromURLIntoImageSphere(m_imageSphereController, Helper.kProfilePageSphereIndex, m_thumbnailUrl, m_userId, false);
        }
    }
        
    private IEnumerator DownloadOriginalImageInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_loadingIcon.Display();

        yield return RefreshProfileData();

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {            
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: DownloadOriginalImageInternal() loading User Original Image for: " + m_originalUrl);

            m_imageLoader.LoadImageFromURLIntoImageSphere(m_imageSphereController, Helper.kSkyboxSphereIndex, m_originalUrl, m_userId, true);
        }

        m_loadingIcon.Hide();
    }
        
    private IEnumerator RefreshProfileData() // NOTE: Since URL's have a lifetime, we need to refresh the data at certain points...
    {            
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: RefreshProfileData() for user: " + m_userId);

        yield return m_backEndAPI.User_GetUser(m_userId);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            m_userId = m_backEndAPI.GetUserResult().data.id;
            m_handle = m_backEndAPI.GetUserResult().data.attributes.handle;
            m_name = m_backEndAPI.GetUserResult().data.attributes.name;
            m_followerCount = m_backEndAPI.GetUserResult().data.attributes.follower_count;
            m_followingCount = m_backEndAPI.GetUserResult().data.attributes.following_count;
            m_postCount = m_backEndAPI.GetUserResult().data.attributes.post_count;
            m_email = m_backEndAPI.GetUserResult().data.attributes.email;
            m_profileDescription = m_backEndAPI.GetUserResult().data.attributes.profile;
            m_followedByMe = m_backEndAPI.GetUserResult().data.attributes.followed_by_me;
            m_thumbnailUrl = m_backEndAPI.GetUserResult().data.attributes.thumbnail_url;
            m_originalUrl = m_backEndAPI.GetUserResult().data.attributes.original_url;
        }
    }

    //TODO: Remove the duplication of DownloadThumbnailImage()
    private void DownloadMenuBarThumbnailImage()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: DownloadMenuBarThumbnailImage() loading User Thumbnail Image for: " + m_loggedUserThumbnailUrl);

        if (m_loggedUserThumbnailUrl != null && m_loggedUserThumbnailUrl.Length > 0)
        {
            m_imageLoader.LoadImageFromURLIntoImageSphere(m_imageSphereController, Helper.kMenuBarProfileSphereIndex, m_loggedUserThumbnailUrl, m_user.m_id, false);
        }
    }

    //TODO: Remove the duplication of DownloadOriginalImage()
    private IEnumerator DownloadMenuBarOriginalImageInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_loadingIcon.Display();

        yield return RefreshLoggedProfileData();

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {            
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: DownloadMenuBarOriginalImageInternal() loading User Original Image for: " + m_loggedUserOriginalUrl);

            m_imageLoader.LoadImageFromURLIntoImageSphere(m_imageSphereController, Helper.kSkyboxSphereIndex, m_loggedUserOriginalUrl, m_user.m_id, true);
        }

        m_loadingIcon.Hide();
    }

    private IEnumerator RefreshLoggedProfileData() // NOTE: Since URL's have a lifetime, we need to refresh the data at certain points...
    {            
        yield return m_backEndAPI.User_GetUser(m_user.m_id);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            m_loggedUserThumbnailUrl = m_backEndAPI.GetUserResult().data.attributes.thumbnail_url;
            m_loggedUserOriginalUrl = m_backEndAPI.GetUserResult().data.attributes.original_url;
        }
    }

    //TODO: Should this be somewhere else and as a public function...?
    private void FollowOrUnfollowUser(string userId, bool doFollow)
    {
        m_coroutineQueue.EnqueueAction(FollowOrUnfollowUserInternal(userId, doFollow));
    }
        
    private IEnumerator FollowOrUnfollowUserInternal(string userId, bool doFollow)
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (doFollow)
        {
            yield return m_backEndAPI.Follow_FollowUser(userId);
        }
        else
        {
            yield return m_backEndAPI.Follow_UnfollowUser(userId);
        }
    }

    private IEnumerator UpdateProfileDescriptionInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_loadingIcon.Display();

        m_handle = m_profileHandleNewText.GetComponentInChildren<Text>().text;
        Helper.TruncateString(ref m_handle, Helper.kMaxCaptionOrDescriptionLength);
        m_profileDescriptionObject.GetComponentInChildren<Text>().text = m_handle;
        yield return m_backEndAPI.Register_UpdateHandle(m_handle);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            m_handleObject.GetComponentInChildren<Text>().text = m_handle;
            m_menuBarProfileButtonObject.GetComponentInChildren<Text>().text = m_handle;
        }

        m_profileDescription = m_profileDescriptionNewText.GetComponentInChildren<Text>().text;
        Helper.TruncateString(ref m_profileDescription, Helper.kMaxCaptionOrDescriptionLength);
        m_profileDescriptionObject.GetComponentInChildren<Text>().text = m_profileDescription;
        yield return m_backEndAPI.Register_UpdateProfileDescription(m_profileDescription);

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            m_profileDescriptionObject.GetComponentInChildren<Text>().text = m_profileDescription;
        }
            
        //TODO: Improve this - such that there's less need to set so many things to active/inactive
        m_profileDetailsTopLevel.SetActive(true);
        m_profileDescriptionUpdateTopLevel.SetActive(false);

        MenuController.MenuConfig menuConfig = m_menuController.GetMenuConfigForOwner(this);
        menuConfig.menuBarVisible = false;
        menuConfig.imageSpheresVisible = true;
        m_menuController.UpdateMenuConfig(this);
        m_appDirector.SetOverlayShowing(false);

        m_loadingIcon.Hide();
    }
}