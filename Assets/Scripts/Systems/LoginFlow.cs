using UnityEngine;
using System.Collections;   // IEnumerator
using UnityEngine.UI;       // UI
using RestSharp;

public class LoginFlow : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    public float m_maxTimeoutForLogin = 7.0f;

    [SerializeField] private AppDirector m_appDirector;  
    [SerializeField] private GameObject m_staticLoadingIcon;
    [SerializeField] private Text m_loginUsernameEmailInput;
    [SerializeField] private Text m_loginPasswordInput;
    [SerializeField] private Text m_signUpUsernameInput;
    [SerializeField] private Text m_signUpEmailInput;
    [SerializeField] private Text m_signUpPasswordInput;
    [SerializeField] private Text m_signUpPasswordConfirmationInput;

    [SerializeField] private GameObject m_loginPage;
    [SerializeField] private GameObject m_signUpPage1;
    [SerializeField] private GameObject m_signUpPage2;

    private CoroutineQueue m_coroutineQueue;
    private BackEndAPI m_backEndAPI;

    //TO DELETE
    /*
    [SerializeField] private AWSS3Client m_AWSS3Client;
    [SerializeField] private GameObject m_fbInvalidLoginError;
      
    private string m_cachedCognitoId;
    private string m_cachedFBId;        // Used to check that the user hasn't changed
    private string m_cachedFBUsername;  // Used to give a welcome message to the user
    */

    // **************************
    // Public functions
    // **************************

    public void Start()
    {        
        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        m_backEndAPI = new BackEndAPI(this);

        m_staticLoadingIcon.SetActive(false);
    }

    public void SetLoginFlowPage(int pageNumber)
    {
        if (pageNumber == 0)
        {
            m_loginPage.SetActive(true);
            m_signUpPage1.SetActive(false);
            m_signUpPage2.SetActive(false);
        }
        else if (pageNumber == 1)
        {
            m_loginPage.SetActive(false);
            m_signUpPage1.SetActive(true);
            m_signUpPage2.SetActive(false);
        }
        else if (pageNumber == 2)
        {
            m_loginPage.SetActive(false);
            m_signUpPage1.SetActive(false);
            m_signUpPage2.SetActive(true);
        }
    }

    public void Login() // Called when User Logs in for the first time, or has logged out.
    {
        m_coroutineQueue.EnqueueAction(LoginInternal());
    }

    public void SignUp() // Called when User Logs in for the first time, or has logged out.
    {
        m_coroutineQueue.EnqueueAction(SignUpInternal());
    }

    public void Logout()
    {
        m_coroutineQueue.EnqueueAction(LogoutInternal());
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator LoginInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LogIn() called");

        m_staticLoadingIcon.SetActive(true);

        yield return m_backEndAPI.Session_SignIn(
            m_loginUsernameEmailInput.text, 
            m_loginPasswordInput.text
        );

        m_staticLoadingIcon.SetActive(false);
    }

    private IEnumerator SignUpInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: SignUp() called");

        m_staticLoadingIcon.SetActive(true);

        yield return m_backEndAPI.Register_CreateUser(
            m_signUpUsernameInput.text, 
            m_signUpEmailInput.text, 
            m_signUpPasswordInput.text, 
            m_signUpPasswordConfirmationInput.text
        );

        m_staticLoadingIcon.SetActive(false);
    }

    private IEnumerator LogoutInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LogOut() called");

        yield return m_backEndAPI.Session_SignOut();
    }

    /*
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
            //CacheFacebookIdAndUsername();
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
            //m_cachedFBUsername = result.ResultDictionary["first_name"] as string;
        }
        else 
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Error Response: " + result.Error);
        }

        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Cached Username as: " + m_cachedFBUsername);
    }
    */
        
    private IEnumerator ProgressAppDirectorPastLogin()
    {     
        /*
        float timeoutTimer = m_maxTimeoutForLogin;
        while (!m_AWSS3Client.IsS3ClientValid()) // This moves the App Director to the profile state when the S3 Client has been initialised
        {
            yield return new WaitForEndOfFrame();
            
            timeoutTimer -= Time.deltaTime;
            if (timeoutTimer <= 0)
            {
                if (Debug.isDebugBuild) Debug.Log("------- VREEL: ProgressAppDirectorPastLogin timed out, login error!");
                //m_fbInvalidLoginError.SetActive(true);
                m_staticLoadingIcon.SetActive(false);
                yield break;
            }
        }

        m_appDirector.RequestProfileState();
        m_staticLoadingIcon.SetActive(false);
        */

        yield break;
    }
}