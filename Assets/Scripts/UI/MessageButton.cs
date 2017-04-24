using UnityEngine;
using UnityEngine.UI;

public class MessageButton : MonoBehaviour
{        
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private ProfileDetails m_profileDetails;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;   
    [SerializeField] private Text m_messageText;
    [SerializeField] private Color m_inactiveButtonColour;
    [SerializeField] private Color m_textColourWithActiveButton;
    [SerializeField] private Color m_textColourWithInactiveButton;
    [SerializeField] private Color m_textColourError;

    private bool m_isActiveButton = false;
    private string m_userId;

    // **************************
    // Public functions
    // **************************

    public bool GetIsActiveButton()
    {
        return m_isActiveButton;
    }

    public void SetIsActiveButton(bool activeButton, string userId = null)
    {
        m_isActiveButton = activeButton;
        m_userId = userId;
        m_menuButton.SetForceColour(!m_isActiveButton, m_inactiveButtonColour);
    }
     
    public void OnButtonSelected()
    {
        if (m_isActiveButton)
        {
            m_profileDetails.OpenProfileDetails(m_userId);
        }
    }

    public string GetText()
    {
        return m_messageText.text;
    }

    public void SetText(string text)
    {
        m_messageText.text = text;
        m_messageText.color = m_isActiveButton ? m_textColourWithActiveButton : m_textColourWithInactiveButton;
    }

    public void SetTextAsError(string text)
    {
        m_messageText.text = text;
        m_messageText.color = m_textColourError;
    }
}