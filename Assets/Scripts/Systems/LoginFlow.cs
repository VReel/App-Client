using UnityEngine;
using System.Collections;   // IEnumerator
using UnityEngine.UI;       // Text

public class LoginFlow : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private Profile m_profile;
    [SerializeField] private LoadingIcon m_loadingIcon;
    [SerializeField] private KeyBoard m_keyboard;
    [SerializeField] private Text m_loginUsernameEmailInput;
    [SerializeField] private Text m_loginPasswordInput;
    [SerializeField] private Text m_signUpUsernameInput;
    [SerializeField] private Text m_signUpEmailInput;
    [SerializeField] private Text m_signUpPasswordInput;
    [SerializeField] private Text m_signUpPasswordConfirmationInput;
    [SerializeField] private Text m_resetPasswordEmailInput;
    [SerializeField] private GameObject m_resetConfirmedText;

    [SerializeField] private GameObject m_loginSubMenu;
    [SerializeField] private GameObject m_loginPage;
    [SerializeField] private GameObject m_signUpPage; 
    [SerializeField] private GameObject m_resetPasswordPage;

    private bool m_loginOpen = false;
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

        m_menuController.RegisterToUseMenuConfig(this);
        MenuController.MenuConfig menuConfig = m_menuController.GetMenuConfigForOwner(this);
        menuConfig.menuBarVisible = false;
        menuConfig.imageSpheresVisible = false;
        menuConfig.subMenuVisible = false;
    }

    public void OpenCloseSwitch()
    {
        if (!m_loginOpen)
        {
            OpenLogin();
        }
        else
        {
            CloseLogin();
        }
    }
        
    public void SetLoginFlowPage(int pageNumber)
    {
        m_loginSubMenu.SetActive(true);

        if (pageNumber == -1)
        {
            m_loginSubMenu.SetActive(false);
        }
        else if (pageNumber == 0) // LoginPage
        {
            m_loginPage.SetActive(true);
            m_signUpPage.SetActive(false);
            m_resetPasswordPage.SetActive(false);
        }
        else if (pageNumber == 1) // SignUp
        {
            m_loginPage.SetActive(false);
            m_signUpPage.SetActive(true);
            m_resetPasswordPage.SetActive(false);
        }
        else if (pageNumber == 2) // ResetPassword
        {
            m_loginPage.SetActive(false);
            m_signUpPage.SetActive(false);
            m_resetPasswordPage.SetActive(true);
        }
    }

    public void Login() 
    {
        m_keyboard.AcceptText();
        m_coroutineQueue.EnqueueAction(LoginInternal());
    }

    public void SignUp()
    {
        m_keyboard.AcceptText();
        m_coroutineQueue.EnqueueAction(SignUpInternal());
    } 

    public void ResetPassword()
    {
        m_keyboard.AcceptText();
        m_coroutineQueue.EnqueueAction(ResetPasswordInternal());
    } 

    // **************************
    // Private/Helper functions
    // **************************

    private void OpenLogin()
    {
        SetLoginFlowPage(0);
        m_loginOpen = true;

        m_menuController.UpdateMenuConfig(this);
        m_appDirector.SetOverlayShowing(true);
    }

    private void CloseLogin()
    {
        m_keyboard.CancelText();
        SetLoginFlowPage(-1);
        m_loginOpen = false;

        m_menuController.UpdateMenuConfig(m_appDirector);
        m_appDirector.SetOverlayShowing(false);
    }

    private IEnumerator LoginInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LogIn() called");

        m_loadingIcon.Display();

        yield return m_backEndAPI.Session_SignIn(
            m_loginUsernameEmailInput.text, 
            m_loginPasswordInput.GetComponent<PasswordText>().GetString(),
            m_user.GetPushNotificationUserID()
        );

        m_loadingIcon.Hide();

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            CloseLogin();
            m_profile.SetMenuBarProfileDetails();
            m_appDirector.RefreshCurrentState();
        }
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
            m_signUpPasswordConfirmationInput.GetComponent<PasswordText>().GetString(),
            m_user.GetPushNotificationUserID()
        );

        m_loadingIcon.Hide();

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            CloseLogin();
            m_profile.SetMenuBarProfileDetails();
            m_appDirector.RefreshCurrentState();
        }
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