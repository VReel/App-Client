using UnityEngine;
using System.Collections;   // IDictionary
using Facebook.Unity;       // FB, AccessToken, HttpMethod, IGraphResult
using Facebook.MiniJSON;    // Json

public class UserLogin : MonoBehaviour 
{   
    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private AWSS3Client m_AWSS3Client;
    [SerializeField] private GameObject m_fbInvalidLoginError;

    private string m_cachedUsername;

    public void Start()
    {
        Facebook.Unity.FB.Init();
        m_cachedUsername = "";
    }

    public void LoginWithFacebook()
    {
        if (Application.isEditor)
        {
            RequestUsername();
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
                // m_AWSS3Client.InitS3ClientFB();//m_AWSS3Client.InitS3ClientFB(AccessToken.CurrentAccessToken.TokenString);
                RequestUsername();
                m_appDirector.SetProfileState();
            } 
            else 
            {
                Debug.Log("------- VREEL: User not logged in through Facebook");
                m_fbInvalidLoginError.SetActive(true);
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

    public bool HasCachedUsername()
    {
        return m_cachedUsername.Length > 0;
    }

    public string GetUsername()
    {
        string username = "Error";
        if ( (FB.IsInitialized && FB.IsLoggedIn) || Application.isEditor)
        {
            username = m_cachedUsername;
        }
        else 
        {
            Debug.Log("------- VREEL: Serious error: Username queried but FB is not LoggedIn and we're not in the Editor!");
        }

        return username;
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
        Debug.Log("------- VREEL: AuthRoleArn assumed: " + m_AWSS3Client.GetCredentials().AuthRoleArn);

    }

    private void RequestUsername()
    {
        FB.API("/me?fields=first_name", HttpMethod.GET, UsernameCallback);
    }

    void UsernameCallback(IGraphResult result)
    {
        Debug.Log("------- VREEL: UsernameCallback() called!");
        if (FB.IsLoggedIn && result.Error == null)
        {                                    
            m_cachedUsername = result.ResultDictionary["first_name"] as string;
        }
        else if (Application.isEditor)
        {
            m_cachedUsername = "Editor";
        }
        else 
        {
            Debug.Log("------- VREEL: Error Response: " + result.Error);
        }
        Debug.Log("------- VREEL: Cached Username as: " + m_cachedUsername);
    }

    /*
    private void FacebookLoginCallback(ILoginResult result)
    {
        Debug.Log("------- VREEL: FacebookLoginCallback");
        Debug.Log("------- VREEL: FB.IsInitialized: " + FB.IsInitialized + ", FB.IsLoggedIn: " + FB.IsLoggedIn);
        if (FB.IsLoggedIn)
        {
            AddFacebookTokenToCognito();
            m_appDirector.SetProfileState();
        }
        else
        {
            m_fbInvalidLoginError.SetActive(true);
        }

            //Debug.Log("------- VREEL: FB.LogInWithReadPermissions");
            //FB.LogInWithReadPermissions (null, FacebookLoginCallback);
    }
    */
}