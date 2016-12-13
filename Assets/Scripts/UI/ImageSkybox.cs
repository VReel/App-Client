using UnityEngine;

public class ImageSkybox : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private GameObject m_uploadButton;

    private Texture2D m_skyboxTexture;
    private string m_imageFilePath; // Points to where the Image came from (S3 Bucket, or Local Device)
    private string m_imagesTopLevelDirectory;

    // **************************
    // Public functions
    // **************************

    public void Awake()
    {
        m_skyboxTexture = new Texture2D(2,2);
        m_imageFilePath = "InvalidFilePath";
        m_imagesTopLevelDirectory = "InvalidTopLevelDirectory";
    }

    public bool IsTextureValid()
    {
        return m_skyboxTexture != null;
    }

    public Texture GetTexture()
    {
        return m_skyboxTexture;
    }

    public string GetImageFilePath()
    {
        return m_imageFilePath;
    }

    public void SetTopLevelDirectory(string imagesTopLevelDirectory)
    {
        m_imagesTopLevelDirectory = imagesTopLevelDirectory;
    }

    public void SetImageAndPath(Texture2D texture, string filePath)
    {        
        if (filePath.Length <= 0)
        {
            m_uploadButton.SetActive(false);
            Debug.Log("------- VREEL: ERROR - attempting to set skybox to an empty filepath!");
            return;
        }

        m_imageFilePath = filePath;
        m_skyboxTexture = texture;

        // Currently the ImageSkybox class is responsible for switching on the Upload button when its possible to select it
        bool isImageFromDevice = m_imageFilePath.StartsWith(m_imagesTopLevelDirectory);
        m_uploadButton.SetActive(isImageFromDevice);

        gameObject.GetComponent<MeshRenderer>().material.mainTexture = m_skyboxTexture;

        // RenderSettings.skybox = texture; // TODO: have the skybox be used instead of this sphere around the user

        Debug.Log("------- VREEL: Changed skybox to = " + filePath);
    }
}