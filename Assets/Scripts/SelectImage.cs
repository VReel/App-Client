using UnityEngine;
using System.Collections;
using VRStandardAssets.Utils;

public class SelectImage : MonoBehaviour 
{
    public Material m_image = null; // Badly named - this is the CUBEMAP that we set on a Skybox!
    public GameObject m_imageSphereSkybox = null; // If this is set then we use it instead of trying to set RenderSettings.Skybox

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
        if (m_imageSphereSkybox != null)
        {
            Texture myTexture = gameObject.GetComponent<MeshRenderer>().material.mainTexture;
            m_imageSphereSkybox.GetComponent<MeshRenderer>().material.mainTexture = myTexture;
        }
        else if (skybox != null)
        {            
            RenderSettings.skybox = skybox;
        }

        Debug.Log("------- VREEL: Changed skybox to material = " + skybox.ToString());
    }
}
