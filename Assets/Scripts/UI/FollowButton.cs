using UnityEngine;
using UnityEngine.UI;

//TODO: If this remains as it is, then it can be merged into the same class as the HeartButton...
public class FollowButton : MonoBehaviour
{        
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private Image m_followButton;
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
            m_followButton.color = m_buttonColourSelected;                      
        }
        else
        {
            m_followButton.color = m_buttonColourDeselected;
        }
    }
}