using UnityEngine;

public class LoginFlow : MonoBehaviour 
{   
    [SerializeField] private AWSS3Client m_AWSS3Client;
    [SerializeField] private GameObject m_loginPage;
    [SerializeField] private GameObject m_signUpPage1;
    [SerializeField] private GameObject m_signUpPage2;

    // --------- Login Screen Page

    public void Login()
    {
        if (m_AWSS3Client.Login())
        {
            m_loginPage.SetActive(true); // TODO: Switch on ImageSpheres
            m_signUpPage1.SetActive(false);
            m_signUpPage2.SetActive(false);
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
    }

    public void BackToEmail()
    {
        Debug.Log("------- VREEL: BackToEmail called");

        m_loginPage.SetActive(false);
        m_signUpPage1.SetActive(true);
        m_signUpPage2.SetActive(false);
    }
}