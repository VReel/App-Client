using UnityEngine;
using System.Collections;           // IEnumerator

public class ImageSkybox : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private Posts m_posts;
    [SerializeField] private Profile m_profile;

    private Material m_myMaterial;
    private int m_currTextureIndex = -1; // ImageSkybox must track the index of the underlying texture it points to in C++ plugin
    private Texture2D m_skyboxTexture;
    private string m_imageIdentifier; // Points to where the Image came from (S3 Bucket, or Local Device)

    // **************************
    // Public functions
    // **************************

    public void Awake()
    {
        m_skyboxTexture = new Texture2D(2,2);
        m_imageIdentifier = "Invalid";
        m_myMaterial = gameObject.GetComponent<MeshRenderer>().material;
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

    public float GetDim()
    {
        return m_myMaterial.GetFloat("_Dim");
    }

    public void SetDim(float dim)
    {
        m_myMaterial.SetFloat("_Dim", dim);
    }

    public void SetImage(Texture2D texture, string imageIdentifier, int textureIndex)
    {        
        if (imageIdentifier.Length <= 0)
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - attempting to set skybox to an empty filepath!");
            return;
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: SetImage() got called with ImageIdentifier: " + imageIdentifier + ", and TextureIndex: " + textureIndex);

        m_imageIdentifier = imageIdentifier;
        m_skyboxTexture = texture;

        m_imageSphereController.SetTextureInUse(m_currTextureIndex, false);
        m_currTextureIndex = textureIndex;
        m_imageSphereController.SetTextureInUse(m_currTextureIndex, true);

        /*
        bool isGalleryState = m_appDirector.GetState() == AppDirector.AppState.kGallery;
        bool isProfileImage = m_profile.IsUser(imageIdentifier); // Identifier is of the User for Profile Pictures
        bool isImageFromDevice = m_imageIdentifier.StartsWith(m_imageSphereController.GetTopLevelDirectory());
        */

        m_myMaterial.mainTexture = m_skyboxTexture;
        m_myMaterial.SetFloat("_FlipY", 1.0f); // This is only ever set to 0 on the default background image - should be 1 every time after...

        // TODO: have the skybox be used instead of just a sphere around the user?
        // RenderSettings.skybox = texture; 
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Changed skybox to = " + m_imageIdentifier + ", with TextureID = " + m_currTextureIndex);
    }
}