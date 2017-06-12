using UnityEngine;
using UnityEngine.UI;

public class MenuHider : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private VRStandardAssets.Utils.VRInteractiveItem m_InteractiveItem;       // The interactive item used to know how the user is interacting with the button
    [SerializeField] private MenuController m_menuController;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        m_InteractiveItem.OnOver += HandleOver;
        m_InteractiveItem.OnUp += HandleUp;
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
        m_menuController.SetMenuVisible(false);
        m_menuController.SetSkyboxDimOn(false);
        gameObject.GetComponent<Collider>().enabled = true; // we switch our box collider back on so that it can still react!
    }

    private void HandleUp()
    {
        m_menuController.SetSkyboxDimOn(true);
        m_menuController.SetMenuVisible(true);
        m_menuController.SetImagesAndMenuBarActive(false);
    }
}