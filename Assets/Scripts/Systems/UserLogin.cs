using UnityEngine;
using System.Collections;   // IDictionary
using Facebook.Unity;       // FB, AccessToken, HttpMethod, IGraphResult
using Facebook.MiniJSON;    // Json

public class UserLogin : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private AWSS3Client m_AWSS3Client;
    [SerializeField] private GameObject m_staticLoadingIcon;
    [SerializeField] private GameObject m_fbInvalidLoginError;

    private CoroutineQueue m_coroutineQueue;
    private string m_cachedCognitoId;
    private string m_cachedUsername;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        Facebook.Unity.FB.Init();

        m_cachedCognitoId = "";
        m_cachedUsername = "";

        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        m_staticLoadingIcon.SetActive(false);
        m_fbInvalidLoginError.SetActive(false);
    }

    public void LoginWithFacebook()
    {
        if (Application.isEditor)
        {
            m_cachedCognitoId = "Editor";
            m_cachedUsername = "Editor";

            m_appDirector.RequestProfileState();
            return;
        }

        if (FB.IsInitialized)
        {
            FB.ActivateApp();

            if (FB.IsLoggedIn) 
            {
                Debug.Log("------- VREEL: User is logged into Facebook");

                m_staticLoadingIcon.SetActive(true);
                m_AWSS3Client.InitS3ClientFB(AccessToken.CurrentAccessToken.TokenString);
                RequestUsername();
                m_coroutineQueue.EnqueueAction(ProgressAppDirectorPastLogin());
            } 
            else 
            {
                Debug.Log("------- VREEL: User not logged in through Facebook");
                m_fbInvalidLoginError.SetActive(true);
            }
        }
        else
        {
            Debug.Log("------- VREEL: ERROR - FB failed to Initialise!");
        }
    }

    public void SetCognitoUserID(string cognitoUserID)
    {
        m_cachedCognitoId = cognitoUserID;
    }

    public string GetCognitoUserID()
    {
        string userID = "Error";
        if (FB.IsInitialized && FB.IsLoggedIn && m_cachedCognitoId != "")
        {
            userID = m_cachedCognitoId;
        }
        else 
        {
            Debug.Log("------- VREEL: ERROR - UserID queried but FB is not LoggedIn and we're not in the Editor!");
        }

        return userID;
    }

    public string GetFBUserID()
    {
        string userID = "Error";
        if (FB.IsInitialized && FB.IsLoggedIn)
        {
            userID = AccessToken.CurrentAccessToken.UserId.ToString();
        }
        else 
        {
            Debug.Log("------- VREEL: ERROR - UserID queried but FB is not LoggedIn and we're not in the Editor!");
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
            Debug.Log("------- VREEL: ERROR - Username queried but FB is not LoggedIn and we're not in the Editor!");
        }            

        return username;
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void RequestUsername()
    {
        FB.API("/me?fields=first_name", HttpMethod.GET, UsernameCallback);
    }

    private void UsernameCallback(IGraphResult result)
    {
        Debug.Log("------- VREEL: UsernameCallback() called!");
        if (FB.IsLoggedIn && result.Error == null)
        {                                    
            m_cachedUsername = result.ResultDictionary["first_name"] as string;
        }
        else 
        {
            Debug.Log("------- VREEL: Error Response: " + result.Error);
        }
        Debug.Log("------- VREEL: Cached Username as: " + m_cachedUsername);
    }

    private IEnumerator ProgressAppDirectorPastLogin()
    {
        // This moves the App Director to the profile state when the S3 Client has been initialised
        while (!m_AWSS3Client.IsS3ClientValid())
        {
            yield return new WaitForEndOfFrame();
        }

        m_appDirector.RequestProfileState();
        m_staticLoadingIcon.SetActive(false);
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

    /*
    private void PrintDebugTokenToCognito()
    {
        Debug.Log("------- VREEL: AddFacebookTokenToCognito");
        Debug.Log("------- VREEL: FB.IsInitialized: " + FB.IsInitialized + ", FB.IsLoggedIn: " + FB.IsLoggedIn);
        Debug.Log("------- VREEL: AccessToken.UserId: " + AccessToken.CurrentAccessToken.UserId.ToString());
        Debug.Log("------- VREEL: AccessToken.CurrentAccessToken: " + AccessToken.CurrentAccessToken.TokenString);
        //Debug.Log("------- VREEL: LoginsCount Before AddLogin(): " + m_AWSS3Client.GetCredentials().LoginsCount);
        //m_AWSS3Client.GetCredentials().AddLogin ("graph.facebook.com", AccessToken.CurrentAccessToken.TokenString);
        //Debug.Log("------- VREEL: LoginsCount After AddLogin(): " + m_AWSS3Client.GetCredentials().LoginsCount);
        //Debug.Log("------- VREEL: AuthRoleArn assumed: " + m_AWSS3Client.GetCredentials().AuthRoleArn);
    }
    */
}