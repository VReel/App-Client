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

    private bool m_isSelected = false;

    public bool GetIsSelected()
    {
        return m_isSelected;
    }

    public void HeartOn()
    {
        m_isSelected = true;
        m_heartButton.color = m_buttonColourSelected;
    }

    public void HeartOff()
    {
        m_isSelected = false;
        m_heartButton.color = m_buttonColourDeselected;
    }

    // **************************
    // Public functions
    // **************************

    public void HeartOnOffSwitch()
    {
        m_isSelected = !m_isSelected;
        if (m_isSelected)
        {
            m_heartButton.color = m_buttonColourSelected;
        }
        else
        {
            m_heartButton.color = m_buttonColourDeselected;
        }
    }
}