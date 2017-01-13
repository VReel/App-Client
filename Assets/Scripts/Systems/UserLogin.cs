using UnityEngine;
using System.Collections;   // IDictionary
using Facebook.Unity;       // FB, AccessToken, HttpMethod, IGraphResult
using Facebook.MiniJSON;    // Json

public class UserLogin : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    public float m_maxTimeoutForLogin = 7.0f;

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private AWSS3Client m_AWSS3Client;
    [SerializeField] private GameObject m_staticLoadingIcon;
    [SerializeField] private GameObject m_fbInvalidLoginError;

    private CoroutineQueue m_coroutineQueue;
    private string m_cachedCognitoId;
    private string m_cachedFBId;        // Used to check that the user hasn't changed
    private string m_cachedFBUsername;  // Used to give a welcome message to the user

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        Facebook.Unity.FB.Init(InitCallback);

        m_cachedCognitoId = "";
        m_cachedFBId = "";
        m_cachedFBUsername = "";

        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        m_staticLoadingIcon.SetActive(false);
        m_fbInvalidLoginError.SetActive(false);
    }

    public void LoginWithFacebook() // Called when User Logs in for the first time, or has logged out.
    {
        m_coroutineQueue.EnqueueAction(LoginWithFacebookInternal());
    }

    public void LogoutWithFacebook()
    {
        m_coroutineQueue.EnqueueAction(LogoutWithFacebookInternal());
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
            Debug.Log("------- VREEL: ERROR - UserID queried but FB is not LoggedIn!");
        }

        return userID;
    }

    public string GetFBUserID()
    {
        string userID = "Error";
        if (FB.IsInitialized && FB.IsLoggedIn)
        {
            userID = m_cachedFBId;
        }
        else 
        {
            Debug.Log("------- VREEL: ERROR - UserID queried but FB is not LoggedIn!");
        }

        return userID;
    }

    public bool HasCachedUsername()
    {
        return m_cachedFBUsername.Length > 0;
    }

    public string GetUsername()
    {
        string username = "Error";
        if (FB.IsInitialized && FB.IsLoggedIn)
        {
            username = m_cachedFBUsername;
        }
        else 
        {
            Debug.Log("------- VREEL: ERROR - Username queried but FB is not LoggedIn!");
        }            

        return username;
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void InitCallback()
    {
        Debug.Log("------- VREEL: InitCallback() activating app and attempting to RunLoginCode()");

        if (FB.IsInitialized)
        {
            FB.ActivateApp();
            RunLoginCode();
        }
        else
        {
            Debug.Log("------- VREEL: ERROR - FB failed to Initialise!");
        }
    }        

    private void RunLoginCode()
    {
        if (FB.IsLoggedIn)
        {
            Debug.Log("------- VREEL: Running Login Code!");
            
            m_cachedCognitoId = "";
            m_cachedFBId = "";
            m_cachedFBUsername = "";
            m_staticLoadingIcon.SetActive(true);

            Debug.Log("------- VREEL: Calling Init with: " + AccessToken.CurrentAccessToken.TokenString + 
                ", for Facebook user with ID: " + AccessToken.CurrentAccessToken.UserId +
                ", token's LastRefresh: " + AccessToken.CurrentAccessToken.LastRefresh);

            m_AWSS3Client.InitS3ClientFB(AccessToken.CurrentAccessToken.TokenString);
            CacheFacebookIdAndUsername();
            m_coroutineQueue.EnqueueAction(ProgressAppDirectorPastLogin());
        }
        else
        {
            Debug.Log("------- VREEL: Failed to run Login Code!");
        }
    }

    private IEnumerator LoginWithFacebookInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (FB.IsInitialized) // About to log into Cognito account through Facebook
        {                       
            Debug.Log("------- VREEL: User about to Log In to Facebook");

            FB.LogInWithReadPermissions(null, (logInResponseObj) => 
            {
                if (FB.IsLoggedIn)
                {
                    Debug.Log("------- VREEL: User successfully logged into Facebook");
                    RunLoginCode();
                }
                else
                {
                    Debug.Log("------- VREEL: Failed to login through Facebook");
                    m_fbInvalidLoginError.SetActive(true);
                }
            });
        }
        else
        {
            Debug.Log("------- VREEL: ERROR - FB failed to Initialise!");
        }
    }

    private IEnumerator LogoutWithFacebookInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        Debug.Log("------- VREEL: LogOut() called");

        if (FB.IsInitialized) 
        {                        
            m_AWSS3Client.ClearClient();
            FB.LogOut();
            m_appDirector.RequestLoginState();
        }
    }

    private void CacheFacebookIdAndUsername()
    {
        m_cachedFBId = AccessToken.CurrentAccessToken.UserId.ToString();
        FB.API("/me?fields=first_name", HttpMethod.GET, UsernameCallback);
    }

    private void UsernameCallback(IGraphResult result)
    {
        Debug.Log("------- VREEL: UsernameCallback() called!");
        if (FB.IsLoggedIn && result.Error == null)
        {                                    
            m_cachedFBUsername = result.ResultDictionary["first_name"] as string;
        }
        else 
        {
            Debug.Log("------- VREEL: Error Response: " + result.Error);
        }

        Debug.Log("------- VREEL: Cached Username as: " + m_cachedFBUsername);
    }

    private IEnumerator ProgressAppDirectorPastLogin()
    {        
        float timeoutTimer = m_maxTimeoutForLogin;
        while (!m_AWSS3Client.IsS3ClientValid()) // This moves the App Director to the profile state when the S3 Client has been initialised
        {
            yield return new WaitForEndOfFrame();
            
            timeoutTimer -= Time.deltaTime;
            if (timeoutTimer <= 0)
            {
                Debug.Log("------- VREEL: ProgressAppDirectorPastLogin timed out, login error!");
                m_fbInvalidLoginError.SetActive(true);
                m_staticLoadingIcon.SetActive(false);
                yield break;
            }
        }

        m_appDirector.RequestProfileState();
        m_staticLoadingIcon.SetActive(false);
    }
}