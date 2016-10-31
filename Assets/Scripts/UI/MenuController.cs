using UnityEngine;

public class MenuController : MonoBehaviour 
{
    public bool m_swipeEnabled = true;      // Should swipe switch menu on/off

    [SerializeField] private GameObject m_quadMenu;
    [SerializeField] private GameObject m_sphereMenu;
    [SerializeField] private GameObject m_GUICanvas;
    [SerializeField] private VRStandardAssets.Utils.VRInput m_input;

    private void OnEnable ()
    {
        m_input.OnSwipe += OnSwipe;
    }

    private void OnDisable ()
    {
        m_input.OnSwipe -= OnSwipe;
    }     

	void Update () 
    {
        InvertMenuEnabled();
	}

    private void InvertMenuEnabled()
    {
        if (m_quadMenu != null && Input.GetKeyDown(KeyCode.N))
        {
            bool active = m_quadMenu.activeInHierarchy;

            m_quadMenu.SetActive(!active);
        }

        if (m_sphereMenu != null && Input.GetKeyDown(KeyCode.M))
        {
            bool active = m_sphereMenu.activeInHierarchy;

            m_sphereMenu.SetActive(!active);
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
            if (m_quadMenu != null)
            {
                m_quadMenu.SetActive(false);
            }

            if (m_sphereMenu != null)
            {
                m_sphereMenu.SetActive(false);
            }

            if (m_GUICanvas != null)
            {
                m_GUICanvas.SetActive(false);
            }

            Debug.Log("------- VREEL: Hide Menu");
        }

        if (swipe == VRStandardAssets.Utils.VRInput.SwipeDirection.DOWN)
        {
            if (m_quadMenu != null)
            {
                m_quadMenu.SetActive(true);
            }

            if (m_sphereMenu != null)
            {
                m_sphereMenu.SetActive(true);
            }

            if (m_GUICanvas != null)
            {
                m_GUICanvas.SetActive(true);
            }

            Debug.Log("------- VREEL: Show Menu");
        }
    }
}