using UnityEngine;
using System.Collections;           // IEnumerator

public class ImageSkybox : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private GameObject m_uploadButton;

    private int m_currTextureIndex = -1; // ImageSkybox must track the index of the underlying texture it points to in C++ plugin
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

    public void SetImageAndPath(Texture2D texture, string filePath, int textureIndex)
    {        
        if (filePath.Length <= 0)
        {
            m_uploadButton.SetActive(false);
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - attempting to set skybox to an empty filepath!");
            return;
        }

        m_imageFilePath = filePath;
        m_skyboxTexture = texture;

        m_imageSphereController.SetTextureInUse(m_currTextureIndex, false);
        m_currTextureIndex = textureIndex;
        m_imageSphereController.SetTextureInUse(m_currTextureIndex, true);

        // Currently the ImageSkybox class is responsible for switching on the Upload button when its possible to select it
        bool isImageFromDevice = m_imageFilePath.StartsWith(m_imagesTopLevelDirectory);
        m_uploadButton.SetActive(isImageFromDevice);

        gameObject.GetComponent<MeshRenderer>().material.mainTexture = m_skyboxTexture;

        // TODO: have the skybox be used instead of just a sphere around the user
        // RenderSettings.skybox = texture; 

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Changed skybox to = " + m_imageFilePath + ", with TextureID = " + m_currTextureIndex);
    }
}