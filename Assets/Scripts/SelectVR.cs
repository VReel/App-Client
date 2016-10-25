using UnityEngine;
using UnityEngine.VR;
using System.Collections;
using VRStandardAssets.Utils;

public class SelectVR : MonoBehaviour 
{
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;

    public void OnMouseDown()
    {
        InvertVREnabled();
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
        InvertVREnabled();
    }

    private void InvertVREnabled()
    {       
        //UnityEngine.VR.VRSettings.enabled = false;
        VRSettings.enabled = !VRSettings.enabled;

        Debug.Log("------- VREEL: Flipped VRSettings.enabled = " + VRSettings.enabled);
    }
}