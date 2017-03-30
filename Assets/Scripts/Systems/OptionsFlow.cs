using UnityEngine;
using System.Collections;   // IEnumerator
using UnityEngine.UI;       // Text

public class OptionsFlow : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;  
    [SerializeField] private GameObject m_staticLoadingIcon;
    [SerializeField] private Text m_currentPasswordInput;
    [SerializeField] private Text m_newPasswordInput;
    [SerializeField] private Text m_confirmPasswordInput;
    [SerializeField] private GameObject m_setPasswordConfirmedMessage;
    [SerializeField] private GameObject m_deleteConfirmedMessage;
    [SerializeField] private GameObject m_errorMessage;
    [SerializeField] private User m_user;
    [SerializeField] private KeyBoard m_keyboard;

    [SerializeField] private GameObject m_optionsPage;
    [SerializeField] private GameObject m_setPasswordPage;
    [SerializeField] private GameObject m_deleteAccountPage;

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

        m_backEndAPI = new BackEndAPI(this, m_errorMessage, m_user);

        m_staticLoadingIcon.SetActive(false);
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
        if (pageNumber == -1)
        {
            m_optionsPage.SetActive(false);
            m_setPasswordPage.SetActive(false);
            m_deleteAccountPage.SetActive(false);
        }
        if (pageNumber == 0)
        {
            m_optionsPage.SetActive(true);
            m_setPasswordPage.SetActive(false);
            m_deleteAccountPage.SetActive(false);
        }
        else if (pageNumber == 1)
        {
            m_optionsPage.SetActive(false);
            m_setPasswordPage.SetActive(true);
            m_deleteAccountPage.SetActive(false);
        }
        else if (pageNumber == 2)
        {
            m_optionsPage.SetActive(false);
            m_setPasswordPage.SetActive(false);
            m_deleteAccountPage.SetActive(true);
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
    }

    private void CloseMenu()
    {
        m_keyboard.CancelText();
        SetOptionsFlowPage(-1);
        m_menuOpen = false;
    }

    private IEnumerator LogoutInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LogOut() called");

        m_staticLoadingIcon.SetActive(true);

        yield return m_backEndAPI.Session_SignOut();

        m_staticLoadingIcon.SetActive(false);

        CloseMenu();
    }   

    private IEnumerator SetPasswordInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LogIn() called");

        m_staticLoadingIcon.SetActive(true);

        yield return m_backEndAPI.Register_UpdateUser(
            m_user.m_handle,
            m_newPasswordInput.GetComponent<PasswordText>().GetString(),
            m_confirmPasswordInput.GetComponent<PasswordText>().GetString(),
            m_currentPasswordInput.GetComponent<PasswordText>().GetString()
        );

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            m_setPasswordConfirmedMessage.SetActive(true);
        }

        m_staticLoadingIcon.SetActive(false);
    }
        
    private IEnumerator DeleteAccountInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: ResetPassword() called");

        m_staticLoadingIcon.SetActive(true);

        yield return m_backEndAPI.Register_DeleteUser();

        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            m_deleteConfirmedMessage.SetActive(true);
        }

        m_staticLoadingIcon.SetActive(false);
    } 
}