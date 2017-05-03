using UnityEngine;
using System.Collections;   // IEnumerator
using UnityEngine.UI;       // Text

public class LoginFlow : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;  
    [SerializeField] private LoadingIcon m_loadingIcon;
    [SerializeField] private Text m_loginUsernameEmailInput;
    [SerializeField] private Text m_loginPasswordInput;
    [SerializeField] private Text m_signUpUsernameInput;
    [SerializeField] private Text m_signUpEmailInput;
    [SerializeField] private Text m_signUpPasswordInput;
    [SerializeField] private Text m_signUpPasswordConfirmationInput;
    [SerializeField] private Text m_resetPasswordEmailInput;
    [SerializeField] private GameObject m_resetConfirmedText;
    [SerializeField] private User m_user;

    [SerializeField] private GameObject m_loginPage;
    [SerializeField] private GameObject m_signUpPage1;
    [SerializeField] private GameObject m_signUpPage2;
    [SerializeField] private GameObject m_resetPasswordPage;

    private CoroutineQueue m_coroutineQueue;
    private BackEndAPI m_backEndAPI;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {        
        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();

        m_backEndAPI = new BackEndAPI(this, m_user.GetErrorMessage(), m_user);
    }

    /*
    public void Restart()
    {
        if (m_coroutineQueue == null)
        {
            m_coroutineQueue = new CoroutineQueue(this);
        }

        m_coroutineQueue.StartLoop();
    }
    */
        
    public void SetLoginFlowPage(int pageNumber)
    {
        if (pageNumber == 0)
        {
            m_loginPage.SetActive(true);
            m_signUpPage1.SetActive(false);
            m_signUpPage2.SetActive(false);
            m_resetPasswordPage.SetActive(false);
        }
        else if (pageNumber == 1)
        {
            m_loginPage.SetActive(false);
            m_signUpPage1.SetActive(true);
            m_signUpPage2.SetActive(false);
            m_resetPasswordPage.SetActive(false);
        }
        else if (pageNumber == 2)
        {
            m_loginPage.SetActive(false);
            m_signUpPage1.SetActive(false);
            m_signUpPage2.SetActive(true);
            m_resetPasswordPage.SetActive(false);
        }
        else if (pageNumber == 3)
        {
            m_loginPage.SetActive(false);
            m_signUpPage1.SetActive(false);
            m_signUpPage2.SetActive(false);
            m_resetPasswordPage.SetActive(true);
        }
    }

    public void Login() 
    {
        m_coroutineQueue.EnqueueAction(LoginInternal());
    }

    public void SignUp()
    {
        m_coroutineQueue.EnqueueAction(SignUpInternal());
    } 

    public void ResetPassword()
    {
        m_coroutineQueue.EnqueueAction(ResetPasswordInternal());
    } 

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator LoginInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LogIn() called");

        m_loadingIcon.Display();

        yield return m_backEndAPI.Session_SignIn(
            m_loginUsernameEmailInput.text, 
            m_loginPasswordInput.GetComponent<PasswordText>().GetString()
        );

        m_loadingIcon.Hide();
    }

    private IEnumerator SignUpInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: SignUp() called");

        m_loadingIcon.Display();

        yield return m_backEndAPI.Register_CreateUser(
            m_signUpUsernameInput.text, 
            m_signUpEmailInput.text, 
            m_signUpPasswordInput.GetComponent<PasswordText>().GetString(),
            m_signUpPasswordConfirmationInput.GetComponent<PasswordText>().GetString()
        );

        m_loadingIcon.Hide();
    } 

    private IEnumerator ResetPasswordInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: ResetPassword() called");

        m_loadingIcon.Display();

        yield return m_backEndAPI.Passwords_PasswordReset(
            m_resetPasswordEmailInput.text
        );

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            m_resetConfirmedText.SetActive(true);
        }

        m_loadingIcon.Hide();
    } 
}