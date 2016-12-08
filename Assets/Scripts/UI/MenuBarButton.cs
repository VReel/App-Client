using UnityEngine;
using UnityEngine.UI;
using System;

// This class is a helper for the MenuController, all the functions in it are called from MenuController

public class MenuBarButton : MonoBehaviour
{
    [SerializeField] private GameObject m_menuSection;      // This Section of the menu is only visible when this object is Selected!
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;
    [SerializeField] private Sprite m_spriteButtonUp;
    [SerializeField] private Sprite m_spriteButtonSelected;

    private bool m_isSelected = false;

    public bool GetIsSelected()
    {
        return m_isSelected;
    }
        
    public void OnButtonSelected()
    {
        m_isSelected = true;
        m_menuButton.SetSpriteButtonUp(m_spriteButtonSelected);
        m_menuButton.RefreshButtonSprite();
    }
        
    public void OnButtonDeselected()
    {
        m_isSelected = false;
        m_menuButton.SetSpriteButtonUp(m_spriteButtonUp);
        m_menuButton.RefreshButtonSprite();
    }

    public void SetMenuSectionActive(bool active)
    {
        if (m_menuSection != null)
        {
            m_menuSection.SetActive(active);
        }
    }
}