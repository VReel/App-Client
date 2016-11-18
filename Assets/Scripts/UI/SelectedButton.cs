using UnityEngine;
using UnityEngine.UI;
using System;

public class SelectedButton : MonoBehaviour
{
    [SerializeField] private GameObject m_menuSection;      // This Section of the menu is only visible when this object is Selected!
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;
    [SerializeField] private Sprite m_spriteButtonSelected;

    private bool m_isSelected = false;   
    private Sprite m_spriteButtonUp;    // This is used to store the Button's Up sprite while we're selected, and reset it afterwards

    public bool GetIsSelected()
    {
        return m_isSelected;
    }

    // This is called by the MenuController!
    public void OnButtonSelected()
    {
        m_isSelected = true;
        m_spriteButtonUp = m_menuButton.GetSpriteButtonUp();
        m_menuButton.SetSpriteButtonUp(m_spriteButtonSelected);
        m_menuButton.RefreshButtonSprite();
    }

    // This is called by the MenuController!
    public void OnButtonDeselected()
    {
        m_isSelected = false;
        if (m_spriteButtonUp != null)
        {
            m_menuButton.SetSpriteButtonUp(m_spriteButtonUp);
            m_menuButton.RefreshButtonSprite();
        }
    }

    public void SetMenuSectionActive(bool active)
    {
        if (m_menuSection != null)
        {
            m_menuSection.SetActive(active);
        }
    }
}