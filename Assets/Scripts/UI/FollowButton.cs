using UnityEngine;
using UnityEngine.UI;

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
            m_menuButton.RefreshColour();
  
            m_followText.color = m_buttonColourSelected;
            m_followText.text = "Following";
        }
        else
        {
            m_menuButton.SetForceColour(false, m_buttonColourSelected);
            m_menuButton.RefreshColour();

            m_followText.color = m_buttonColourDeselected;
            m_followText.text = "Follow +";
        }
    }

    public void SetVisible(bool visible)
    {
        foreach(var renderer in gameObject.GetComponentsInChildren<Renderer>())
        {                
            renderer.enabled = visible; // Handles Mesh + SpriteRenderer components
        }

        foreach(var ui in gameObject.GetComponentsInChildren<UnityEngine.UI.Graphic>())
        {                
            ui.enabled = visible; // Handles Images + Text components
        }

        foreach(var collider in gameObject.GetComponentsInChildren<Collider>())
        {                
            collider.enabled = visible; // Handles BoxCollider + MeshCollider components
        }
    }
}