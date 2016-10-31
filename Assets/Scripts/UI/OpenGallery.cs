using UnityEngine;

public class OpenGallery : MonoBehaviour 
{
    [SerializeField] private DeviceGallery m_deviceGallery;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;

    public void OnMouseDown()
    {
        OpenAndroidGallery();
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
        OpenAndroidGallery();
    }

    private void OpenAndroidGallery()
    {
        Debug.Log("------- VREEL: Called OpenAndroidGallery()");

        m_deviceGallery.OpenAndroidGallery();
    }
}