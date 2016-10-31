using UnityEngine;

public class SelectImage : MonoBehaviour 
{
    [SerializeField] private ImageSkybox m_imageSphereSkybox;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;

    private string m_imageFilePath; // All ImageSphere's have a path (either through the internet, or local to the device) associated with them!

    public string GetImageFilePath()
    {
        return m_imageFilePath;
    }

    public void SetImageAndPath(Texture texture, string filePath)
    {
        m_imageFilePath = filePath;
        gameObject.GetComponent<MeshRenderer>().material.mainTexture = texture;
    }

    public void OnMouseDown()
    {
        SetSkybox();
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
        SetSkybox();
    }

    private void SetSkybox()
    {    
        if (m_imageSphereSkybox != null)
        {
            Texture myTexture = gameObject.GetComponent<MeshRenderer>().material.mainTexture;
            m_imageSphereSkybox.SetImageAndPath(myTexture, m_imageFilePath);
        }
    }
}