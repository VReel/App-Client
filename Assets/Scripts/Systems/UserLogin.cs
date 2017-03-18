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

    public bool HasCachedCognitoUserID()
    {
        return m_cachedCognitoId.Length > 0;
    }

    public string GetCognitoUserID()
    {        
        if (FB.IsInitialized && FB.IsLoggedIn && HasCachedCognitoUserID())
        {
            return m_cachedCognitoId;
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - CognitoUserID queried but FB is not LoggedIn!");

        return "Error";
    }

    /*
    public string GetFBUserID()
    {
        if (FB.IsInitialized && FB.IsLoggedIn)
        {
            return m_cachedFBId;
        }

        Debug.Log("------- VREEL: ERROR - FBUserID queried but FB is not LoggedIn!");

        return "Error";  
    }
    */

    public bool HasCachedUsername()
    {
        return m_cachedFBUsername.Length > 0;
    }

    public string GetUsername()
    {        
        if (FB.IsInitialized && FB.IsLoggedIn && HasCachedUsername())
        {
            return m_cachedFBUsername;
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - Username queried but FB is not LoggedIn!");

        return "Error";
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void InitCallback()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: InitCallback() activating app and attempting to RunLoginCode()");

        if (FB.IsInitialized)
        {
            FB.ActivateApp();
            RunLoginCode();
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - FB failed to Initialise!");
        }
    }        

    private void RunLoginCode()
    {
        if (FB.IsLoggedIn)
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Running Login Code!");
            
            m_cachedCognitoId = "";
            m_cachedFBId = "";
            m_cachedFBUsername = "";
            m_staticLoadingIcon.SetActive(true);
                       
            if (Debug.isDebugBuild) 
                Debug.Log("------- VREEL: Calling Init with: " + AccessToken.CurrentAccessToken.TokenString + 
                ", for Facebook user with ID: " + AccessToken.CurrentAccessToken.UserId +
                ", token's LastRefresh: " + AccessToken.CurrentAccessToken.LastRefresh);

            m_AWSS3Client.InitS3ClientFB(AccessToken.CurrentAccessToken.TokenString);
            CacheFacebookIdAndUsername();
            m_coroutineQueue.EnqueueAction(ProgressAppDirectorPastLogin());
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Failed to run Login Code!");
        }
    }

    private void CacheFacebookIdAndUsername()
    {
        m_cachedFBId = AccessToken.CurrentAccessToken.UserId.ToString();
        FB.API("/me?fields=first_name", HttpMethod.GET, UsernameCallback);
    }

    private void UsernameCallback(IGraphResult result)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: UsernameCallback() called!");
        if (FB.IsLoggedIn && result.Error == null)
        {                                    
            m_cachedFBUsername = result.ResultDictionary["first_name"] as string;
        }
        else 
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Error Response: " + result.Error);
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Cached Username as: " + m_cachedFBUsername);
    }

    private IEnumerator LoginWithFacebookInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (FB.IsInitialized) // About to log into Cognito account through Facebook
        {                       
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: User about to Log In to Facebook");

            // TODO: Improve the usage of this function as it does 2 bad things:
            // (1) It frames out when its called...
            // (2) If called in VR when the user is logging in for their first time, 
            //      it expects users to come out of VR to accept Facebook Login...
            FB.LogInWithReadPermissions(null, (logInResponseObj) => 
            {
                if (FB.IsLoggedIn)
                {
                    if (Debug.isDebugBuild) Debug.Log("------- VREEL: User successfully logged into Facebook");
                    RunLoginCode();
                }
                else
                {
                    if (Debug.isDebugBuild) Debug.Log("------- VREEL: Failed to login through Facebook");
                    m_fbInvalidLoginError.SetActive(true);
                }
            });
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - FB failed to Initialise!");
        }
    }

    private IEnumerator LogoutWithFacebookInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LogOut() called");

        if (FB.IsInitialized) 
        {                        
            m_AWSS3Client.ClearClient();
            FB.LogOut();
            m_appDirector.RequestLoginState();
        }
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
                if (Debug.isDebugBuild) Debug.Log("------- VREEL: ProgressAppDirectorPastLogin timed out, login error!");
                m_fbInvalidLoginError.SetActive(true);
                m_staticLoadingIcon.SetActive(false);
                yield break;
            }
        }

        m_appDirector.RequestProfileState();
        m_staticLoadingIcon.SetActive(false);
    }
}   