using UnityEngine;
using System.Collections;
using VRStandardAssets.Utils;

public class MenuController : MonoBehaviour 
{
    public GameObject m_quadMenu = null;
    public GameObject m_sphereMenu = null;

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
        }
    }
}