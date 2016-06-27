using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SelectScene : MonoBehaviour 
{	
    public string m_sceneName = "";   

    void OnMouseDown()
    {
        SceneManager.LoadScene(m_sceneName);
    }

    /*
    void Update () 
    {        
        if (SelectedObject())
        {
            SceneManager.LoadScene(m_sceneName);
        }

    }

    bool SelectedObject()
    {
        
        if ( Input.GetMouseButtonDown(0))
        {
            var hit : RaycastHit;
            var ray : Ray = Camera.main.ScreenPointToRay (Input.mousePosition);

            if (Physics.Raycast (ray, hit, 100.0))
            {  
                return true;
            }
        }       

        return false;
    }
    */

}