using UnityEngine;
using UnityEngine.UI;

//TODO: If this remains as it is, then it can be merged into the same class as the HeartButton...
public class FollowButton : MonoBehaviour
{        
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private Text m_followText;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;
    [SerializeField] private Color m_buttonColourSelected;
    [SerializeField] private Color m_buttonColourDeselected;

    private bool m_isOn = false;

    // **************************
    // Public functions
    // **************************

    public bool GetIsOn()
    {
        return m_isOn;
    }
        
    public void FollowOnOffSwitch(bool isOn)
    {
        m_isOn = isOn;
        if (m_isOn)
        {
            m_menuButton.SetForceColour(true, m_buttonColourSelected);
            m_menuButton.RefreshButtonColor();
  
            m_followText.color = m_buttonColourSelected;
            m_followText.text = "Unfollow";
        }
        else
        {
            m_menuButton.SetForceColour(false, m_buttonColourSelected);
            m_menuButton.RefreshButtonColor();

            m_followText.color = m_buttonColourDeselected;
            m_followText.text = "Follow";
        }
    }
}