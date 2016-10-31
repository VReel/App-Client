using UnityEngine;

public class ImageSkybox : MonoBehaviour 
{
    private string m_imageFilePath; // Points to where the Image came from (S3 Bucket, or Local Device)

    public Texture GetTexture()
    {
        return gameObject.GetComponent<MeshRenderer>().material.mainTexture;
    }

    public string GetImageFilePath()
    {
        return m_imageFilePath;
    }

    public void SetImageAndPath(Texture texture, string filePath)
    {
        m_imageFilePath = filePath;
        gameObject.GetComponent<MeshRenderer>().material.mainTexture = texture;

        // RenderSettings.skybox = texture; // TODO: have the skybox be used instead of this sphere around the user

        Debug.Log("------- VREEL: Changed skybox to = " + filePath);
    }
}