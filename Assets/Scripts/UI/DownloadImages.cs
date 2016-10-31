using UnityEngine;

public class DownloadImages : MonoBehaviour 
{
    [SerializeField] private AWSS3Client m_awsS3Client;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;   

    public void OnMouseDown()
    {
        DownloadAllImages();
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
        DownloadAllImages();
    }

    private void DownloadAllImages()
    {
        Debug.Log("------- VREEL: Called DownloadImage()");
        
        m_awsS3Client.DownloadAllImages();
    }
}