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

    [SerializeField] private GameObject m_menuSubTree;
    [SerializeField] private GameObject m_loginSubMenu;
    [SerializeField] private GameObject m_profileSubMenu;
    [SerializeField] private GameObject m_gallerySubMenu;
    [SerializeField] private GameObject m_profileMessage;
    [SerializeField] private UserLogin m_userLogin;
    [SerializeField] private VRStandardAssets.Utils.Reticle m_reticle;
    [SerializeField] private VRStandardAssets.Utils.VRInput m_input;
    [SerializeField] private GameObject[] m_menuBarButtons;

    private bool m_isMenuActive = true;

    // **************************
    // Public functions
    // **************************

    public bool GetMenuActive()
    {
        return m_isMenuActive;
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

    public void SetGallerySubMenuActive(bool active)
    {
        m_gallerySubMenu.SetActive(active);
        OnButtonSelected(m_menuBarButtons[1]);  // button 1 = Gallery button
    }

    public void SetAllSubMenusActive(bool active)
    {
        m_loginSubMenu.SetActive(active);
        m_profileSubMenu.SetActive(active);
        m_gallerySubMenu.SetActive(active);
    }

    public void ShowWelcomeText()
    {
        StartCoroutine(InternalShowWelcomeText());
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator InternalShowWelcomeText()
    {
        while (!m_userLogin.HasCachedUsername())
        {
            yield return new WaitForEndOfFrame();
        }

        Debug.Log("------- VREEL: Setting Welcome Text!");
        Text profileTextComponent = m_profileMessage.GetComponentInChildren<Text>();
        if (profileTextComponent != null)
        {
            profileTextComponent.text = "Welcome " + m_userLogin.GetUsername() + "!";
            profileTextComponent.color = Color.black;
        }
        m_profileMessage.SetActive(true);
    }

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
            var menuBarButton = currButton.GetComponent<MenuBarButton>();
            if (button == currButton)
            {
                menuBarButton.OnButtonSelected();
                menuBarButton.SetMenuSectionActive(true);
            }
            else 
            {
                menuBarButton.OnButtonDeselected();
                menuBarButton.SetMenuSectionActive(false);
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

            Debug.Log("------- VREEL: Hide Menu");
        }

        if (swipe == VRStandardAssets.Utils.VRInput.SwipeDirection.DOWN)
        {
            SetMenuVisible(true);

            Debug.Log("------- VREEL: Show Menu");
        }
    }

    private void SetMenuVisible(bool visible)
    {        
        if (m_menuSubTree != null)
        {
            //We Trawl through all the subobjects, hiding all meshes, images and colliders!
            foreach(var mesh in m_menuSubTree.GetComponentsInChildren<MeshRenderer>())
            {                
                mesh.enabled = visible;
            }

            foreach(var image in m_menuSubTree.GetComponentsInChildren<UnityEngine.UI.Image>())
            {                
                image.enabled = visible;
            }

            foreach(var text in m_menuSubTree.GetComponentsInChildren<Text>())
            {                
                text.enabled = visible;
            }

            foreach(var collider in m_menuSubTree.GetComponentsInChildren<Collider>())
            {                
                collider.enabled = visible;
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

        m_isMenuActive = visible;
    }
}