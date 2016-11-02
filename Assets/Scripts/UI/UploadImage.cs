using UnityEngine;

public class UploadImage : MonoBehaviour 
{
    [SerializeField] private AWSS3Client m_awsS3Client;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;

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
        UploadToBucket();
    }

    private void UploadToBucket()
    {    
        Debug.Log("------- VREEL: Called UploadImage()");

        m_awsS3Client.UploadImage();
    }
}