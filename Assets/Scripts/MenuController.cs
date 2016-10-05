using UnityEngine;
using System.Collections;

public class MenuController : MonoBehaviour 
{
    public GameObject m_menu = null;

	void Update () 
    {
        if (m_menu != null && Input.GetKeyDown(KeyCode.M))
        {
            bool active = m_menu.activeInHierarchy;

            m_menu.SetActive(!active);
        }
	}
}
