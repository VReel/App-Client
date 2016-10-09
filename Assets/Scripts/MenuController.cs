using UnityEngine;
using System.Collections;

public class MenuController : MonoBehaviour 
{
    public GameObject m_quadMenu = null;
    public GameObject m_sphereMenu = null;

	void Update () 
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
}
