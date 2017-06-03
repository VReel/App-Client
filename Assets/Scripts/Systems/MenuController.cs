using UnityEngine;
using UnityEngine.VR;       // VRSettings
using UnityEngine.UI;       // Text
using System.Collections;   // IEnumerator

public class MenuController : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    public bool m_swipeEnabled = true;      // When on, swiping switches the menu on/off

    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private GameObject m_menuSubTree;
    [SerializeField] private GameObject m_loginSubMenu;
    [SerializeField] private GameObject m_homeSubMenu;
    [SerializeField] private GameObject m_searchSubMenu;
    [SerializeField] private GameObject m_profileSubMenu;
    [SerializeField] private GameObject m_gallerySubMenu;
    [SerializeField] private User m_user;
    [SerializeField] private KeyBoard m_keyboard;
    [SerializeField] private VRStandardAssets.Utils.Reticle m_reticle;
    [SerializeField] private VRStandardAssets.Utils.VRInput m_input;
    [SerializeField] private GameObject m_menuBar;
    [SerializeField] private GameObject[] m_menuBarButtons;

    private bool m_isMenuActive = true;

    // **************************
    // Public functions
    // **************************

    public bool IsMenuActive()
    {
        return m_isMenuActive;
    }

    public void SetImagesAndMenuBarActive(bool active)
    {
        m_imageSphereController.EnableOrDisableAllImageSpheres(active);
        m_menuBar.SetActive(active);
    }

    public void SetLoginSubMenuActive(bool active)
    {
        m_loginSubMenu.SetActive(active);
    }

    public void SetProfileSubMenuActive(bool active)
    {
        m_profileSubMenu.SetActive(active);
        OnButtonSelected(m_menuBarButtons[0]);  // button 0 = Profile button
    }

    public void SetExploreSubMenuActive(bool active)
    {
        m_homeSubMenu.SetActive(active);
        OnButtonSelected(m_menuBarButtons[1]);  // button 1 = Explore button
    }

    public void SetFollowingSubMenuActive(bool active)
    {
        m_homeSubMenu.SetActive(active);
        OnButtonSelected(m_menuBarButtons[2]);  // button 2 = Following button
    }

    public void SetSearchSubMenuActive(bool active)
    {
        m_searchSubMenu.SetActive(active);
        OnButtonSelected(m_menuBarButtons[3]);  // button 3 = Search button
    }        

    public void SetGallerySubMenuActive(bool active)
    {
        m_gallerySubMenu.SetActive(active);
        OnButtonSelected(m_menuBarButtons[4]);  // button 4 = Gallery button
    }

    public void SetAllSubMenusActive(bool active)
    {
        m_loginSubMenu.SetActive(active);
        m_homeSubMenu.SetActive(active);
        m_searchSubMenu.SetActive(active);
        m_profileSubMenu.SetActive(active);
        m_gallerySubMenu.SetActive(active);
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void OnEnable ()
    {
        m_input.OnSwipe += OnSwipe;
    }

    private void OnDisable ()
    {
        m_input.OnSwipe -= OnSwipe;
    }     

    private void OnButtonSelected(GameObject button)
    {
        foreach(GameObject currButton in m_menuBarButtons)
        {       
            var menuBarButton = currButton.GetComponent<SelectedButton>();
            if (button == currButton)
            {
                menuBarButton.OnButtonSelected();
                menuBarButton.GetAdditionalReference().SetActive(true); // Set's MenuSection to Active
            }
            else 
            {
                menuBarButton.OnButtonDeselected();
                menuBarButton.GetAdditionalReference().SetActive(false); // Set's MenuSection to not Active
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
            SetMenuVisible(false);

            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Hide Menu");
        }

        if (swipe == VRStandardAssets.Utils.VRInput.SwipeDirection.DOWN)
        {
            SetMenuVisible(true);

            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Show Menu");
        }
    }

    private void SetMenuVisible(bool visible)
    {        
        if (m_menuSubTree != null)
        {
            //We Trawl through all the subobjects, hiding all meshes, images and colliders!
            foreach(var renderer in m_menuSubTree.GetComponentsInChildren<Renderer>())
            {                
                renderer.enabled = visible; // Handles Mesh + SpriteRenderer components
            }

            foreach(var ui in m_menuSubTree.GetComponentsInChildren<UnityEngine.UI.Graphic>())
            {                
                ui.enabled = visible; // Handles Images + Text components
            }

            foreach(var collider in m_menuSubTree.GetComponentsInChildren<Collider>())
            {                
                collider.enabled = visible; // Handles BoxCollider + MeshCollider components
            }
        }

        if (m_reticle != null)
        {
            if (visible)
            {
                m_reticle.Show();
            }
            else
            {
                m_reticle.Hide();
            }
        }

        if (m_keyboard != null)
        {
            if (visible && m_keyboard.ShouldBeShowing())
            {
                m_keyboard.Show();
            }
            else
            {
                m_keyboard.Hide();
            }                
        }

        m_isMenuActive = visible;
    }
}