using UnityEngine;

public class ImageSkybox : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************
    
    private Texture2D m_skyboxTexture;
    private string m_imageFilePath; // Points to where the Image came from (S3 Bucket, or Local Device)

    // **************************
    // Public functions
    // **************************

    public void Awake()
    {
        m_skyboxTexture = new Texture2D(2,2);
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

    public void SetImageAndPath(Texture2D texture, string filePath)
    {
        if (filePath.Length <= 0)
        {
            Debug.Log("------- VREEL: ERROR - attempting to set skybox to an empty filepath!");
            return;
        }

        m_imageFilePath = filePath;
        m_skyboxTexture = texture;

        gameObject.GetComponent<MeshRenderer>().material.mainTexture = m_skyboxTexture;

        // RenderSettings.skybox = texture; // TODO: have the skybox be used instead of this sphere around the user

        Debug.Log("------- VREEL: Changed skybox to = " + filePath);
    }
}