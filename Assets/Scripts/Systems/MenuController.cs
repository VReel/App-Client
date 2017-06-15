using UnityEngine;
using UnityEngine.VR;               // VRSettings
using UnityEngine.UI;               // Text
using System.Collections;           // IEnumerator
using System.Collections.Generic;   // List

public class MenuController : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    public bool m_swipeEnabled = true;      // When on, swiping switches the menu on/off

    [SerializeField] private AppDirector m_appDirector;  
    [SerializeField] private User m_user;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private KeyBoard m_keyboard;
    [SerializeField] private VRStandardAssets.Utils.Reticle m_reticle;
    [SerializeField] private VRStandardAssets.Utils.VRInput m_input;
    [SerializeField] private GameObject m_menuSubTree;
    [SerializeField] private GameObject m_loginSubMenu;
    [SerializeField] private GameObject m_searchSubMenu;
    [SerializeField] private GameObject m_profileSubMenu;
    [SerializeField] private GameObject m_gallerySubMenu;
    [SerializeField] private GameObject m_menuBar;
    [SerializeField] private GameObject[] m_menuBarButtons;

    public class MenuConfig
    {
        public MonoBehaviour owner { get; set; }
        public bool menuVisible { get; set; }
        public bool menuBarVisible { get; set; }
        public bool imageSpheresVisible { get; set; }
        public bool subMenuVisible { get; set; }
    }

    private class MenuConfigChanged // Doing lots of redundant GetComponentsInChildren() calls is unnecessarily expensive, this struct removes that
    {
        public bool menuVisibleChanged { get; set; }
        public bool menuBarVisibleChanged { get; set; }
        public bool imageSpheresVisibleChanged { get; set; }
    }

    private int m_currMenuConfigIndex;
    private List<MenuConfig> m_menuConfigList;
    private MenuConfig m_lastMenuConfig;
    private MenuConfigChanged m_menuConfigChanged;
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start() 
    {
        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();

        if (m_menuConfigList == null)
        {
            m_menuConfigList = new List<MenuConfig>();
        }

        m_lastMenuConfig = new MenuConfig();
        m_lastMenuConfig.owner = null;
        m_lastMenuConfig.menuVisible = true; // We don't want keyboard.Hide() to get called early on...
        m_lastMenuConfig.menuBarVisible = false;
        m_lastMenuConfig.imageSpheresVisible = false;
        m_lastMenuConfig.subMenuVisible = false;

        m_menuConfigChanged = new MenuConfigChanged();
    }

    public bool IsMenuActive()
    {
        return m_menuConfigList[m_currMenuConfigIndex].menuVisible;
    }
        
    public void RegisterToUseMenuConfig(MonoBehaviour owner)
    {
        if (m_menuConfigList == null)
        {
            m_menuConfigList = new List<MenuConfig>();
        }

        MenuConfig newMenuConfig = new MenuConfig();
        newMenuConfig.owner = owner;
        newMenuConfig.menuVisible = true;
        newMenuConfig.menuBarVisible = false;
        newMenuConfig.imageSpheresVisible = false;
        newMenuConfig.subMenuVisible = false;
        m_menuConfigList.Add( newMenuConfig );
    }

    public MenuConfig GetMenuConfigForOwner(MonoBehaviour owner)
    {
        int index = GetMenuConfigIndexForOwner(owner);
        return m_menuConfigList[index];
    }

    public void UpdateMenuConfig(MonoBehaviour owner)
    {
        m_currMenuConfigIndex = GetMenuConfigIndexForOwner(owner);

        m_menuConfigChanged.menuVisibleChanged = m_menuConfigList[m_currMenuConfigIndex].menuVisible != m_lastMenuConfig.menuVisible;
        m_menuConfigChanged.menuBarVisibleChanged = m_menuConfigList[m_currMenuConfigIndex].menuBarVisible != m_lastMenuConfig.menuBarVisible;
        m_menuConfigChanged.imageSpheresVisibleChanged = m_menuConfigList[m_currMenuConfigIndex].imageSpheresVisible != m_lastMenuConfig.imageSpheresVisible;

        RefreshMenuBasedOnConfig();
    }     

    public void SetSkyboxDimOn(bool visible)
    {
        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(SetSkyboxDimOnInternal(visible));
    }

    // **************************
    // Private/Helper functions
    // **************************   

    private int GetMenuConfigIndexForOwner(MonoBehaviour owner)
    {        
        for (int menuConfigIndex = 0; menuConfigIndex < m_menuConfigList.Count; menuConfigIndex++)
        {
            if (m_menuConfigList[menuConfigIndex].owner == owner)
            {
                return menuConfigIndex;
            }
        }

        return -1;
    }                

    private void RefreshMenuBasedOnConfig()
    {
        m_lastMenuConfig.owner = m_menuConfigList[m_currMenuConfigIndex].owner;
        m_lastMenuConfig.menuVisible = m_menuConfigList[m_currMenuConfigIndex].menuVisible;
        m_lastMenuConfig.menuBarVisible = m_menuConfigList[m_currMenuConfigIndex].menuBarVisible;
        m_lastMenuConfig.imageSpheresVisible = m_menuConfigList[m_currMenuConfigIndex].imageSpheresVisible;
        m_lastMenuConfig.subMenuVisible = m_menuConfigList[m_currMenuConfigIndex].subMenuVisible;

        if (m_menuConfigChanged.menuVisibleChanged) 
        {
            SetMenuVisible(m_menuConfigList[m_currMenuConfigIndex].menuVisible);
        }

        if (m_menuConfigChanged.menuBarVisibleChanged) 
        {
            SetSubTreeVisible(m_menuBar.gameObject, m_menuConfigList[m_currMenuConfigIndex].menuBarVisible);
        }

        if (m_menuConfigChanged.imageSpheresVisibleChanged) 
        {
            SetSubTreeVisible(m_imageSphereController.gameObject, m_menuConfigList[m_currMenuConfigIndex].imageSpheresVisible);
        }

        SetCurrentSubMenuActive(m_menuConfigList[m_currMenuConfigIndex].subMenuVisible);
    }     
        
    private void SetCurrentSubMenuActive(bool active)
    {
        SetAllSubMenusActive(false);

        if ((m_appDirector.GetState() == AppDirector.AppState.kInit))
        {
            // Empty on purpose
        }
        else if (m_appDirector.GetState() == AppDirector.AppState.kLogin)
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
        else
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - SetCurrentSubMenuActive() called but invalid AppDirector State is set");
        }
    }

    private void SetAllSubMenusActive(bool active)
    {
        m_loginSubMenu.SetActive(active);
        m_searchSubMenu.SetActive(active);
        m_profileSubMenu.SetActive(active);
        m_gallerySubMenu.SetActive(active);
    }

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

    private void SetMenuVisible(bool visible)
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
    }

    private void SetSubTreeVisible(GameObject subtree, bool visible)
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