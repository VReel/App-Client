using UnityEngine;
using System.Collections;           // IEnumerator

public class ImageSkybox : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private Profile m_profile;
    [SerializeField] private GameObject m_uploadButton;
    [SerializeField] private GameObject m_deleteButton;

    private int m_currTextureIndex = -1; // ImageSkybox must track the index of the underlying texture it points to in C++ plugin
    private Texture2D m_skyboxTexture;
    private string m_imageIdentifier; // Points to where the Image came from (S3 Bucket, or Local Device)
    private string m_imagesTopLevelDirectory;   

    // **************************
    // Public functions
    // **************************

    public void Awake()
    {
        m_skyboxTexture = new Texture2D(2,2);
        m_imageIdentifier = "Invalid";
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

    public string GetImageIdentifier()
    {
        return m_imageIdentifier;
    }

    public void SetTopLevelDirectory(string imagesTopLevelDirectory)
    {
        m_imagesTopLevelDirectory = imagesTopLevelDirectory;
    }

    public void SetImage(Texture2D texture, string imageIdentifier, int textureIndex)
    {        
        if (imageIdentifier.Length <= 0)
        {
            m_uploadButton.SetActive(false);
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - attempting to set skybox to an empty filepath!");
            return;
        }

        m_imageIdentifier = imageIdentifier;
        m_skyboxTexture = texture;

        m_imageSphereController.SetTextureInUse(m_currTextureIndex, false);
        m_currTextureIndex = textureIndex;
        m_imageSphereController.SetTextureInUse(m_currTextureIndex, true);

        bool isImageFromDevice = m_imageIdentifier.StartsWith(m_imagesTopLevelDirectory);
        m_uploadButton.SetActive(isImageFromDevice);  // Currently the ImageSkybox class is responsible for switching on the Upload button
        m_deleteButton.SetActive(!isImageFromDevice); // and the Delete button, when its possible to select either

        if (!isImageFromDevice) // This image is being set from the Profile, not the Gallery
        {
            const int kStandardThumbnailWidth = 320; //TODO: This is also hardcoded in DeviceGallery, need to move this into a global variable...
            bool isThumbnail = texture.width <= kStandardThumbnailWidth;
            if (isThumbnail) // This image is a Thumbnail, so we want to download the full image!
            {
                m_profile.DownloadOriginalImage(m_imageIdentifier);
            }
            else // TODO: This is the Original image, so we want to replace the Thumbnail on the ImageSphere!
            {                
                m_imageSphereController.SetImageWithId(imageIdentifier, texture, textureIndex);
            }
        }

        gameObject.GetComponent<MeshRenderer>().material.mainTexture = m_skyboxTexture;

        // TODO: have the skybox be used instead of just a sphere around the user
        // RenderSettings.skybox = texture; 
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Changed skybox to = " + m_imageIdentifier + ", with TextureID = " + m_currTextureIndex);
    }
}