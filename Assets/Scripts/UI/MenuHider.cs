using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR;       //VRSettings

public class MenuHider : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private VRStandardAssets.Utils.VRInteractiveItem m_InteractiveItem;       // The interactive item used to know how the user is interacting with the button
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private Collider m_vreelCollider;
    [SerializeField] private Image m_vreelLogo;

    private MonoBehaviour m_previousMenuConfigOwner;
    private bool m_showing = true;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        m_InteractiveItem.OnOver += HandleOver;
        m_InteractiveItem.OnUp += HandleUp;

        if (m_menuController.GetMenuConfigForOwner(this) == null)
        {
            m_menuController.RegisterToUseMenuConfig(this);
        }
    }

    public void OnDestroy()
    {
        m_InteractiveItem.OnOver -= HandleOver;
        m_InteractiveItem.OnUp -= HandleUp;
    }        

    public void SetMenuVisibility(bool on)
    {
        if (m_menuController.GetMenuConfigForOwner(this) == null)
        {
            m_menuController.RegisterToUseMenuConfig(this);
        }

        if (on)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    public void SkyboxSelected()
    {
        if (!VRSettings.enabled)
        {
            Hide();
        }
    }

    public void VReelButtonSelected()
    {
        if (!VRSettings.enabled)
        {
            Show();
        }
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void HandleOver()
    {        
        Hide();
    }

    private void HandleUp()
    {
        Show();
    }

    private void Hide()
    {
        if (!m_showing)
        {
            return;
        }
        m_showing = false;

        if (m_menuController.GetMenuConfigForOwner(this) == null)
        {
            m_menuController.RegisterToUseMenuConfig(this);
        }            

        m_previousMenuConfigOwner = m_menuController.GetCurrMenuConfig().owner;
        m_menuController.GetMenuConfigForOwner(this).menuVisible = false;
        m_menuController.UpdateMenuConfig(this);

        m_menuController.SetSkyboxDimOn(false);

        m_vreelLogo.enabled = true;
        Color col = m_vreelLogo.color;
        col.a = 0.5f;
        m_vreelLogo.color = col;

        if (!VRSettings.enabled)
        {
            m_vreelCollider.enabled = true;
        }
        else
        {
            gameObject.GetComponent<Collider>().enabled = true; // we switch the MenuHider's box collider back on so that it can still react!
        }
    }

    private void Show()
    {
        if (m_showing)
        {
            return;
        }
        m_showing = true;

        if (m_menuController.GetMenuConfigForOwner(this) == null)
        {
            m_menuController.RegisterToUseMenuConfig(this);
        }

        m_menuController.SetSkyboxDimOn(true);

        m_menuController.UpdateMenuConfig(m_previousMenuConfigOwner);

        Color col = m_vreelLogo.color;
        col.a = 1;
        m_vreelLogo.color = col;
    }
}