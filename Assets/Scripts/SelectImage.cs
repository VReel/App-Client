using UnityEngine;
using System.Collections;
using VRStandardAssets.Utils;

public class SelectImage : MonoBehaviour 
{
    public Material m_image = null;

    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;

    public void OnMouseDown()
    {
        SetSkybox(m_image);
    }

    private void OnEnable ()
    {
        m_menuButton.OnButtonSelected += OnButtonSelected;
    }

    private void OnDisable ()
    {
        m_menuButton.OnButtonSelected -= OnButtonSelected;
    }        

    private void OnButtonSelected(VRStandardAssets.Menu.MenuButton button)
    {
        SetSkybox(m_image);
    }

    private void SetSkybox(Material skybox)
    {        
        if (skybox != null)
        {
            RenderSettings.skybox = skybox;
        }

        Debug.Log("------- VREEL: Changed skybox to material = " + skybox.ToString());
    }
}
