using UnityEngine;
using UnityEngine.UI;

public class HeartButton : MonoBehaviour
{        
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private Image m_heartButton;
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
        
    public void HeartOnOffSwitch(bool heartOn)
    {
        m_isOn = heartOn;
        if (m_isOn)
        {
            m_heartButton.color = m_buttonColourSelected;
        }
        else
        {
            m_heartButton.color = m_buttonColourDeselected;
        }
    }
}