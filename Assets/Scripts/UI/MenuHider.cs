using UnityEngine;
using UnityEngine.UI;

public class MenuHider : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private VRStandardAssets.Utils.VRInteractiveItem m_InteractiveItem;       // The interactive item used to know how the user is interacting with the button
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private Image m_vreelLogo;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        m_InteractiveItem.OnOver += HandleOver;
        m_InteractiveItem.OnUp += HandleUp;

        m_menuController.RegisterToUseMenuConfig(this);
    }

    public void OnDestroy()
    {
        m_InteractiveItem.OnOver -= HandleOver;
        m_InteractiveItem.OnUp -= HandleUp;
    }        

    // **************************
    // Private/Helper functions
    // **************************

    private void HandleOver()
    {
        m_menuController.GetMenuConfigForOwner(this).menuVisible = false;
        m_menuController.UpdateMenuConfig(this);

        m_menuController.SetSkyboxDimOn(false);
        gameObject.GetComponent<Collider>().enabled = true; // we switch our box collider back on so that it can still react!

        m_vreelLogo.enabled = true;
        Color col = m_vreelLogo.color;
        col.a = 0.5f;
        m_vreelLogo.color = col;
    }

    private void HandleUp()
    {
        m_menuController.SetSkyboxDimOn(true);

        MenuController.MenuConfig menuConfig = m_menuController.GetMenuConfigForOwner(this);
        menuConfig.menuVisible = true;
        menuConfig.menuBarVisible = false;
        menuConfig.imageSpheresVisible = false;
        m_menuController.UpdateMenuConfig(this);

        Color col = m_vreelLogo.color;
        col.a = 1;
        m_vreelLogo.color = col;
    }
}