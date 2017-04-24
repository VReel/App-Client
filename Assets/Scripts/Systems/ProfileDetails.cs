using UnityEngine;
using UnityEngine.UI;           //Text
using System.Collections;       // IEnumerator

//TODO: Improve this class name and the way this communicates with everything else...
public class ProfileDetails : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private Posts m_posts;
    [SerializeField] private ListUsers m_listUsers;
    [SerializeField] private GameObject m_profileDetailsTopLevel;
    [SerializeField] private GameObject m_followButtonObject;
    [SerializeField] private GameObject m_handleObject;
    [SerializeField] private GameObject m_followerCountObject;
    [SerializeField] private GameObject m_followingCountObject;
    [SerializeField] private GameObject m_profileDescriptionObject;

    private string m_userId;
    private string m_handle;
    private string m_name;
    private int m_followerCount;
    private int m_followingCount;
    private int m_postCount;
    private string m_email;
    private string m_profileDescription;
    private bool m_followedByMe;
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

        CloseProfileDetails();
    }

    public string GetUserId()
    {
        return m_userId;
    }

    public void OpenProfileDetails()
    {
        if( !m_posts.IsProfileType() )
        {
            return;
        }

        m_profileDetailsTopLevel.SetActive(true);

        bool isCurrentUser = m_user.m_id.CompareTo(m_posts.GetCurrUserOrTagID()) == 0;
        m_followButtonObject.SetActive(!isCurrentUser);

        m_handleObject.GetComponentInChildren<Text>().text = "";
        m_followerCountObject.GetComponentInChildren<Text>().text = "";
        m_followingCountObject.GetComponentInChildren<Text>().text = "";
        m_profileDescriptionObject.GetComponentInChildren<Text>().text = "";

        m_coroutineQueue.EnqueueAction(GetUserDetails());

        m_listUsers.CloseListUsers();
    }

    public void DisplayFollowers()
    {
        m_listUsers.DisplayFollowersResults(m_posts.GetCurrUserOrTagID());
    }

    public void DisplayFollowing()
    {
        m_listUsers.DisplayFollowingResults(m_posts.GetCurrUserOrTagID());
    }

    public void CloseProfileDetails()
    {
        m_profileDetailsTopLevel.SetActive(false);
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

    public void FollowOrUnfollowUser(string userId, bool doFollow)
    {
        m_coroutineQueue.EnqueueAction(FollowOrUnfollowUserInternal(userId, doFollow));
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator GetUserDetails()
    {
        yield return m_appDirector.VerifyInternetConnection();

        yield return m_backEndAPI.User_GetUser(m_posts.GetCurrUserOrTagID());

        m_userId = m_backEndAPI.GetUserResult().data.id;
        m_handle = m_backEndAPI.GetUserResult().data.attributes.handle;
        m_name = m_backEndAPI.GetUserResult().data.attributes.name;
        m_followerCount = m_backEndAPI.GetUserResult().data.attributes.follower_count;
        m_followingCount = m_backEndAPI.GetUserResult().data.attributes.following_count;
        m_postCount = m_backEndAPI.GetUserResult().data.attributes.post_count;
        m_email = m_backEndAPI.GetUserResult().data.attributes.email;
        m_profileDescription = m_backEndAPI.GetUserResult().data.attributes.profile;
        m_followedByMe = m_backEndAPI.GetUserResult().data.attributes.followed_by_me;

        m_handleObject.GetComponentInChildren<Text>().text = m_handle; 
        m_followerCountObject.GetComponentInChildren<Text>().text = m_followerCount.ToString(); 
        m_followingCountObject.GetComponentInChildren<Text>().text = m_followingCount.ToString(); 
        m_profileDescriptionObject.GetComponentInChildren<Text>().text = m_profileDescription; 

        m_followButtonObject.GetComponentInChildren<FollowButton>().FollowOnOffSwitch(m_followedByMe);
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
}