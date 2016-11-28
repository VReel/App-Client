using UnityEngine;
using Facebook.Unity;

public class LoginFlow : MonoBehaviour 
{   
    [SerializeField] private AWSS3Client m_AWSS3Client;
    [SerializeField] private GameObject m_loginPage;
    [SerializeField] private GameObject m_signUpPage1;
    [SerializeField] private GameObject m_signUpPage2;
    [SerializeField] private GameObject m_fbInvalidLoginError;
    [SerializeField] private GameObject m_invalidLoginError;
    [SerializeField] private GameObject m_invalidEmailError;
    [SerializeField] private GameObject m_invalidUsernameError;
    [SerializeField] private GameObject m_invalidPasswordError;

    // --------- FB Login Screen Page

    void Start()
    {
        Facebook.Unity.FB.Init();
    }

    public void FBLogin()
    {
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
            Debug.Log("------- VREEL: Serious error: FB failed to Init");
        }
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
    }

    // --------- Login Screen Page

    public void Login()
    {
        if (m_AWSS3Client.Login())
        {
            // TODO: Switch on ImageSpheres and enter user Profile
        }
        else 
        {
            m_invalidLoginError.SetActive(true);
        }
    }

    public void BeginSignUpProcess()
    {
        Debug.Log("------- VREEL: BeginSignUpProcess called");

        m_loginPage.SetActive(false);
        m_signUpPage1.SetActive(true);
        m_signUpPage2.SetActive(false);
    }

    // --------- Sign Up 1 Page - Check Email availability and set Full Name

    public void ConfirmEmailAvailability()
    {
        if (m_AWSS3Client.ConfirmEmailAvailability())
        {
            m_loginPage.SetActive(false);
            m_signUpPage1.SetActive(false);
            m_signUpPage2.SetActive(true);
        }
        else
        {
            m_invalidEmailError.SetActive(true);
        }
    }

    public void BackToLogin()
    {
        Debug.Log("------- VREEL: BackToLogin called");

        m_loginPage.SetActive(true);
        m_signUpPage1.SetActive(false);
        m_signUpPage2.SetActive(false);
    }

    // --------- Sign Up 2 Page - Set Username and Password

    public void SignUp()
    {
        if (m_AWSS3Client.SignUp())
        {
            m_loginPage.SetActive(false);
            m_signUpPage1.SetActive(false);
            m_signUpPage2.SetActive(true);
        }
        else
        {
            //TODO: Do this properly...
            m_invalidUsernameError.SetActive(true);
            m_invalidPasswordError.SetActive(true);
        }
    }

    public void BackToEmail()
    {
        Debug.Log("------- VREEL: BackToEmail called");

        m_loginPage.SetActive(false);
        m_signUpPage1.SetActive(true);
        m_signUpPage2.SetActive(false);
    }
}