using UnityEngine;
using System.Collections;   // IEnumerator
using UnityEngine.UI;       // Text

public class OptionsFlow : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private LoadingIcon m_loadingIcon;
    [SerializeField] private Text m_currentPasswordInput;
    [SerializeField] private Text m_newPasswordInput;
    [SerializeField] private Text m_confirmPasswordInput;
    [SerializeField] private GameObject m_setPasswordConfirmedMessage;
    [SerializeField] private GameObject m_deleteConfirmedMessage;
    [SerializeField] private User m_user;
    [SerializeField] private KeyBoard m_keyboard;

    [SerializeField] private GameObject m_optionsSubMenu;
    [SerializeField] private GameObject m_optionsPage;
    [SerializeField] private GameObject m_setPasswordPage;
    [SerializeField] private GameObject m_deleteAccountPage;
    [SerializeField] private GameObject m_aboutPage;

    private bool m_menuOpen = false;
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

    public void OpenCloseSwitch()
    {
        if (!m_menuOpen)
        {
            OpenMenu();
        }
        else
        {
            CloseMenu();
        }
    }
         
    public void SetOptionsFlowPage(int pageNumber)
    {
        m_optionsSubMenu.SetActive(true);

        if (pageNumber == -1)
        {
            m_optionsSubMenu.SetActive(false);
        }
        else if (pageNumber == 0) // Options Page
        {
            m_optionsPage.SetActive(true);
            m_setPasswordPage.SetActive(false);
            m_deleteAccountPage.SetActive(false);
            m_aboutPage.SetActive(false);
        }
        else if (pageNumber == 1) // Set Password
        {
            m_optionsPage.SetActive(false);
            m_setPasswordPage.SetActive(true);
            m_deleteAccountPage.SetActive(false);
            m_aboutPage.SetActive(false);
        }
        else if (pageNumber == 2) // Delete Account
        {
            m_optionsPage.SetActive(false);
            m_setPasswordPage.SetActive(false);
            m_deleteAccountPage.SetActive(true);
            m_aboutPage.SetActive(false);
        }
        else if (pageNumber == 3) // About Account
        {
            m_optionsPage.SetActive(false);
            m_setPasswordPage.SetActive(false);
            m_deleteAccountPage.SetActive(false);
            m_aboutPage.SetActive(true);
        }
    }

    public void Logout()
    {
        m_coroutineQueue.EnqueueAction(LogoutInternal());
    }

    public void SetPassword() 
    {
        m_coroutineQueue.EnqueueAction(SetPasswordInternal());
    }

    public void DeleteAccount()
    {
        m_coroutineQueue.EnqueueAction(DeleteAccountInternal());
    } 

    public void EndDeleteAccount()
    {
        CloseMenu();
        m_user.Clear();
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void OpenMenu()
    {
        SetOptionsFlowPage(0);
        m_menuOpen = true;

        m_menuController.SetCurrentSubMenuActive(false);
        m_menuController.SetImagesAndMenuBarActive(false);
    }

    private void CloseMenu()
    {
        m_keyboard.CancelText();
        SetOptionsFlowPage(-1);
        m_menuOpen = false;

        m_menuController.SetCurrentSubMenuActive(true);
        m_menuController.SetImagesAndMenuBarActive(true);
    }

    private IEnumerator LogoutInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LogOut() called");

        m_loadingIcon.Display();

        yield return m_backEndAPI.Session_SignOut();

        m_loadingIcon.Hide();

        CloseMenu();
    }   

    private IEnumerator SetPasswordInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LogIn() called");

        m_loadingIcon.Display();

        yield return m_backEndAPI.Register_UpdatePassword(
            m_newPasswordInput.GetComponent<PasswordText>().GetString(),
            m_confirmPasswordInput.GetComponent<PasswordText>().GetString(),
            m_currentPasswordInput.GetComponent<PasswordText>().GetString()
        );

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            m_setPasswordConfirmedMessage.SetActive(true);
        }

        m_loadingIcon.Hide();
    }
        
    private IEnumerator DeleteAccountInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: ResetPassword() called");

        m_loadingIcon.Display();

        yield return m_backEndAPI.Register_DeleteUser();

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            m_deleteConfirmedMessage.SetActive(true);
        }

        m_loadingIcon.Hide();
    } 
}