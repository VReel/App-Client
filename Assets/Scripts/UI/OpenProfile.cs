using UnityEngine;

public class OpenProfile : MonoBehaviour 
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
        OpenUserProfile();
    }

    private void OpenUserProfile()
    {
        Debug.Log("------- VREEL: Called DownloadImage()");
        
        m_awsS3Client.DownloadAllImages();
    }
}