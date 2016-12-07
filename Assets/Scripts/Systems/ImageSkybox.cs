using UnityEngine;

public class ImageSkybox : MonoBehaviour 
{
    private Texture2D m_skyboxTexture;
    private string m_imageFilePath; // Points to where the Image came from (S3 Bucket, or Local Device)

    public void Awake()
    {
        m_skyboxTexture = new Texture2D(2,2);
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
        m_imageFilePath = filePath;
        m_skyboxTexture = texture;

        gameObject.GetComponent<MeshRenderer>().material.mainTexture = m_skyboxTexture;

        // RenderSettings.skybox = texture; // TODO: have the skybox be used instead of this sphere around the user

        if (filePath == null)
        {
            // TODO: Workout where its coming from...
        }

        Debug.Log("------- VREEL: Changed skybox to = " + filePath);
    }
}