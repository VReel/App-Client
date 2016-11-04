using UnityEngine;

public class MenuController : MonoBehaviour 
{
    public bool m_swipeEnabled = true;      // When on, swiping switches the menu on/off

    [SerializeField] private GameObject m_menuContainerObject;
    [SerializeField] private GameObject m_GUICanvas;
    [SerializeField] private VRStandardAssets.Utils.VRInput m_input;
    [SerializeField] private GameObject[] m_menuBarButtons;

    private bool m_isMenuActive = true;

    public bool GetMenuActive()
    {
        return m_isMenuActive;
    }

    private void OnEnable ()
    {
        m_input.OnSwipe += OnSwipe;

        foreach(GameObject menuButtonObject in m_menuBarButtons)
        {
            var menuButton = menuButtonObject.GetComponent<VRStandardAssets.Menu.MenuButton>();
            if (menuButton != null)
            {
                menuButton.OnButtonSelected += OnButtonSelected;
            }
        }
    }

    private void OnDisable ()
    {
        m_input.OnSwipe -= OnSwipe;

        foreach(GameObject menuButtonObject in m_menuBarButtons)
        {
            var menuButton = menuButtonObject.GetComponent<VRStandardAssets.Menu.MenuButton>();
            if (menuButton != null)
            {
                menuButton.OnButtonSelected -= OnButtonSelected;
            }
        }
    }     

    private void OnButtonSelected(VRStandardAssets.Menu.MenuButton button)
    {
        foreach(GameObject menuButtonObject in m_menuBarButtons)
        {       
            var selectedButton = menuButtonObject.GetComponent<SelectedButton>();
            if (menuButtonObject.GetComponent<VRStandardAssets.Menu.MenuButton>() == button)
            {
                selectedButton.OnButtonSelected();
                selectedButton.SetMenuSectionActive(true);
            }
            else 
            {
                selectedButton.OnButtonDeselected();
                selectedButton.SetMenuSectionActive(false);
            }
        }
    }

    private void OnSwipe(VRStandardAssets.Utils.VRInput.SwipeDirection swipe)
    {
        if (!m_swipeEnabled)
        {
            return;
        }

        if (swipe == VRStandardAssets.Utils.VRInput.SwipeDirection.UP)
        {
            SetMenuActive(false);

            Debug.Log("------- VREEL: Hide Menu");
        }

        if (swipe == VRStandardAssets.Utils.VRInput.SwipeDirection.DOWN)
        {
            SetMenuActive(true);

            Debug.Log("------- VREEL: Show Menu");
        }
    }

    private void SetMenuActive(bool active)
    {        
        if (m_menuContainerObject != null)
        {
            //We Trawl through all the subobjects, hiding all meshes, images and colliders!
            foreach(var mesh in m_menuContainerObject.GetComponentsInChildren<MeshRenderer>())
            {                
                mesh.enabled = active;
            }

            foreach(var image in m_menuContainerObject.GetComponentsInChildren<UnityEngine.UI.Image>())
            {                
                image.enabled = active;
            }

            foreach(var collider in m_menuContainerObject.GetComponentsInChildren<Collider>())
            {                
                collider.enabled = active;
            }
        }

        if (m_GUICanvas != null)
        {
            m_GUICanvas.SetActive(active);
        }

        m_isMenuActive = active;
    }
}