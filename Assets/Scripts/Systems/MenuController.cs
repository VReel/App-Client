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

    [SerializeField] private AppDirector m_appDirector;  
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private GameObject m_menuSubTree;
    [SerializeField] private GameObject m_loginSubMenu;
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
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start() 
    {
        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();
    }

    public bool IsMenuActive()
    {
        return m_isMenuActive;
    }

    public void SetCurrentSubMenuActive(bool active)
    {
        SetAllSubMenusActive(false);

        if (m_appDirector.GetState() == AppDirector.AppState.kLogin)
        {
            SetLoginSubMenuActive(active);
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kExplore)
        {
            SetExploreSubMenuActive(active);
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kFollowing)
        {
            SetFollowingSubMenuActive(active);
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kProfile)
        {
            SetProfileSubMenuActive(active);
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kSearch)
        {
            SetSearchSubMenuActive(active);
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kGallery)
        {
            SetGallerySubMenuActive(active);
        }
    }

    //TODO: BE MORE RESTRICTIVE WITH THE ABILITY FOR OTHER CLASSES TO JUST ACTIVATE/DEACTIVATE UI!
    public void SetMenuBarActive(bool active)
    {
        //m_menuBar.SetActive(active);
        SetSubTreeVisible(m_menuBar.gameObject, active);
    }

    public void SetImagesActive(bool active)
    {
        SetSubTreeVisible(m_imageSphereController.gameObject, active);
    }

    public void SetImagesAndMenuBarActive(bool active)
    {
        SetImagesActive(active);
        SetMenuBarActive(active);
    }
                
    public void SetAllSubMenusActive(bool active)
    {
        m_loginSubMenu.SetActive(active);
        m_searchSubMenu.SetActive(active);
        m_profileSubMenu.SetActive(active);
        m_gallerySubMenu.SetActive(active);
    }

    public void SetSubTreeVisible(GameObject subtree, bool visible)
    {
        SetSubTreeVisibleInternal(subtree, visible);
    }

    public void SetMenuVisible(bool visible)
    {
        SetMenuVisibleInternal(visible);
    }

    public void SetSkyboxDimOn(bool visible)
    {
        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(SetSkyboxDimOnInternal(visible));
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void SetLoginSubMenuActive(bool active)
    {
        m_loginSubMenu.SetActive(active);
    }

    private void SetProfileSubMenuActive(bool active)
    {
        m_profileSubMenu.SetActive(active);
        OnButtonSelected(m_menuBarButtons[0]);  // button 0 = Profile button
    }

    private void SetExploreSubMenuActive(bool active)
    {        
        OnButtonSelected(m_menuBarButtons[1]);  // button 1 = Explore button
    }

    private void SetFollowingSubMenuActive(bool active)
    {        
        OnButtonSelected(m_menuBarButtons[2]);  // button 2 = Following button
    }

    private void SetSearchSubMenuActive(bool active)
    {
        m_searchSubMenu.SetActive(active);
        OnButtonSelected(m_menuBarButtons[3]);  // button 3 = Search button
    }        

    private void SetGallerySubMenuActive(bool active)
    {        
        m_gallerySubMenu.SetActive(active);
        OnButtonSelected(m_menuBarButtons[4]);  // button 4 = Gallery button
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
            var menuBarButton = currButton.GetComponent<SelectedButton>();
            if (button == currButton)
            {
                menuBarButton.OnButtonSelected();
            }
            else 
            {
                menuBarButton.OnButtonDeselected();
            }
        }
    }

    private void OnSwipe(VRStandardAssets.Utils.VRInput.SwipeDirection swipe)
    {
        return;
        
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

    private void SetMenuVisibleInternal(bool visible)
    {        
        if (m_menuSubTree != null)
        {
            SetSubTreeVisible(m_menuSubTree, visible);
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

    private void SetSubTreeVisibleInternal(GameObject subtree, bool visible)
    {
        //We Trawl through all the subobjects, hiding all meshes, images and colliders!
        foreach(var renderer in subtree.GetComponentsInChildren<Renderer>())
        {                
            renderer.enabled = visible; // Handles Mesh + SpriteRenderer components
        }

        foreach(var ui in subtree.GetComponentsInChildren<UnityEngine.UI.Graphic>())
        {                
            ui.enabled = visible; // Handles Images + Text components
        }

        foreach(var collider in subtree.GetComponentsInChildren<Collider>())
        {                
            collider.enabled = visible; // Handles BoxCollider + MeshCollider components
        }
    }

    private IEnumerator SetSkyboxDimOnInternal(bool dimOn)
    {
        const float kMaxDimOn = 0.7f;
        const float kDuration = 2.0f;

        float lerpDelta = (Time.fixedDeltaTime / kDuration) * kMaxDimOn;
        float currentDim = m_imageSkybox.GetDim();
        while (0 <= currentDim && currentDim <= kMaxDimOn)
        {                        
            m_imageSkybox.SetDim(currentDim);
            currentDim += dimOn ? lerpDelta : -lerpDelta;
            yield return null;
        }
    }        
}