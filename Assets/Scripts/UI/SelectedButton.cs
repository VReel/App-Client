﻿using UnityEngine;

// Gives the option of displaying a different color on selection, functions must be controlled by another class

public class SelectedButton : MonoBehaviour
{        
    //[SerializeField] private GameObject m_additionalReference; // UNCOMMENT IF NEEDED
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;
    [SerializeField] private Color m_buttonColourSelected;

    private bool m_isSelected = false;

    public bool GetIsSelected()
    {
        return m_isSelected;
    }

    /*
    public GameObject GetAdditionalReference()
    {
        return m_additionalReference;
    }
    */

    public void ButtonSelected(bool selected)
    {
        if (selected)
        {
            OnButtonSelected();
        }
        else
        {
            OnButtonDeselected();
        }
    }

    public void OnButtonSelected()
    {
        m_isSelected = true;
        if (m_menuButton != null)
        {
            m_menuButton.SetForceColour(true, m_buttonColourSelected);
            m_menuButton.RefreshColour();
        }
    }

    public void OnButtonDeselected()
    {
        m_isSelected = false;
        if (m_menuButton != null)
        {
            m_menuButton.SetForceColour(false, m_buttonColourSelected);
            m_menuButton.RefreshColour();
        }
    }
}