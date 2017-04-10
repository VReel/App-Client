using UnityEngine;

/* Up - 333333
 * Over - b9b9b9
 * Down - dbdbdb
 */

// This class is a helper for the MenuController, all the functions in it are called from MenuController
public class MenuBarButton : MonoBehaviour
{
    [SerializeField] private GameObject m_menuSection;      // This Section of the menu is only visible when this object is Selected!
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;
    [SerializeField] private Color m_buttonColourSelected;

    private bool m_isSelected = false;

    public bool GetIsSelected()
    {
        return m_isSelected;
    }
        
    public void OnButtonSelected()
    {
        m_isSelected = true;
        m_menuButton.SetForceColour(true, m_buttonColourSelected);
        m_menuButton.RefreshButtonColor();
    }
        
    public void OnButtonDeselected()
    {
        m_isSelected = false;
        m_menuButton.SetForceColour(false, m_buttonColourSelected);
        m_menuButton.RefreshButtonColor();
    }

    public void SetMenuSectionActive(bool active)
    {
        if (m_menuSection != null)
        {
            m_menuSection.SetActive(active);
        }
    }
}