using UnityEngine;
using System.Collections;   // IEnumerator
using UnityEngine.UI;       // Text

public class LoginFlow : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;  
    [SerializeField] private GameObject m_staticLoadingIcon;
    [SerializeField] private Text m_loginUsernameEmailInput;
    [SerializeField] private Text m_loginPasswordInput;
    [SerializeField] private Text m_signUpUsernameInput;
    [SerializeField] private Text m_signUpEmailInput;
    [SerializeField] private Text m_signUpPasswordInput;
    [SerializeField] private Text m_signUpPasswordConfirmationInput;
    [SerializeField] private GameObject m_errorMessage;
    [SerializeField] private User m_user;

    [SerializeField] private GameObject m_loginPage;
    [SerializeField] private GameObject m_signUpPage1;
    [SerializeField] private GameObject m_signUpPage2;

    private CoroutineQueue m_coroutineQueue;
    private BackEndAPI m_backEndAPI;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {        
        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();

        m_backEndAPI = new BackEndAPI(this, m_errorMessage, m_user);

        m_staticLoadingIcon.SetActive(false);
    }

    public void Restart()
    {
        m_coroutineQueue.StartLoop();
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

    public void Login() 
    {
        m_coroutineQueue.EnqueueAction(LoginInternal());
    }

    public void SignUp()
    {
        m_coroutineQueue.EnqueueAction(SignUpInternal());
    }     

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator LoginInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LogIn() called");

        m_staticLoadingIcon.SetActive(true);

        // TEMPORARY!!!
        yield return m_backEndAPI.Session_SignIn(
            "arthur", "BWM0SLA5"
        );

        /*
        yield return m_backEndAPI.Session_SignIn(
            m_loginUsernameEmailInput.text, 
            m_loginPasswordInput.GetComponent<PasswordText>().GetString()
        );
        */

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
            m_signUpPasswordInput.GetComponent<PasswordText>().GetString(),
            m_signUpPasswordConfirmationInput.GetComponent<PasswordText>().GetString()
        );

        m_staticLoadingIcon.SetActive(false);
    }        
}