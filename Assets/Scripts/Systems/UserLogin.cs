using UnityEngine;
using Facebook.Unity;

public class UserLogin : MonoBehaviour 
{   
    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private AWSS3Client m_AWSS3Client;
    [SerializeField] private GameObject m_fbInvalidLoginError;

    void Start()
    {
        Facebook.Unity.FB.Init();
    }

    public void LoginWithFacebook()
    {
        if (Application.isEditor)
        {
            m_appDirector.SetProfileState();
            return;
        }

        if (FB.IsInitialized)
        {
            FB.ActivateApp();

            if (FB.IsLoggedIn) 
            {   //User already logged in from a previous session                
                Debug.Log("------- VREEL: User already logged in from a previous session");
                AddFacebookTokenToCognito();
            } 
            else 
            {
                Debug.Log("------- VREEL: FB.LogInWithReadPermissions");
                FB.LogInWithReadPermissions (null, FacebookLoginCallback);
            }
        }
        else
        {
            Debug.Log("------- VREEL: Serious error: FB failed to Initialise!");
        }
    }

    public string GetUserID()
    {
        string userID = "Error";
        if (FB.IsInitialized && FB.IsLoggedIn)
        {
            userID = AccessToken.CurrentAccessToken.UserId.ToString();
        }
        else if (Application.isEditor)
        {
            userID = "Editor";
        }
        else 
        {
            Debug.Log("------- VREEL: Serious error: UserID queried but FB is not LoggedIn and we're not in the Editor!");
        }

        return userID;
    }

    private void FacebookLoginCallback(ILoginResult result)
    {
        Debug.Log("------- VREEL: FacebookLoginCallback");
        Debug.Log("------- VREEL: FB.IsInitialized: " + FB.IsInitialized + ", FB.IsLoggedIn: " + FB.IsLoggedIn);
        if (FB.IsLoggedIn)
        {
            AddFacebookTokenToCognito();
        }
        else
        {
            m_fbInvalidLoginError.SetActive(true);
        }
    }

    private void AddFacebookTokenToCognito()
    {
        Debug.Log("------- VREEL: AddFacebookTokenToCognito");
        Debug.Log("------- VREEL: FB.IsInitialized: " + FB.IsInitialized + ", FB.IsLoggedIn: " + FB.IsLoggedIn);
        Debug.Log("------- VREEL: AccessToken.UserId: " + AccessToken.CurrentAccessToken.UserId.ToString());
        Debug.Log("------- VREEL: AccessToken.CurrentAccessToken: " + AccessToken.CurrentAccessToken.TokenString);
        Debug.Log("------- VREEL: LoginsCount Before AddLogin(): " + m_AWSS3Client.GetCredentials().LoginsCount);
        m_AWSS3Client.GetCredentials().AddLogin ("graph.facebook.com", AccessToken.CurrentAccessToken.TokenString);
        Debug.Log("------- VREEL: LoginsCount After AddLogin(): " + m_AWSS3Client.GetCredentials().LoginsCount);

        m_appDirector.SetProfileState();
    }
}