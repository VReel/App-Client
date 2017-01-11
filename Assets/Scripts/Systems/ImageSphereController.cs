using UnityEngine;
using UnityEngine.VR;       // VRSettings
using UnityEngine.UI;       // Text
using System.Collections;   // IEnumerator

public class ImageSphereController : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private float m_defaultSphereScale = 1.0f;
    [SerializeField] private float m_scalingFactor = 0.88f;
    [SerializeField] private GameObject[] m_imageSpheres;
    [SerializeField] private Texture2D m_loadingSphereTexture;

    private string kLoadingTextureFilePath = "LoadingImage";

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        ForceHideAllImageSpheres();
        SetIndexOnAllImageSpheres();
    }

    public int GetAvailablePluginTextureIndex(int sphereIndex)
    {
        int textureIndexAvailable = -1;

        int currTextureIndex = m_imageSpheres[sphereIndex].GetComponent<ImageSphere>().GetCurrPluginTextureIndex();
        if (currTextureIndex == -1 || currTextureIndex == (sphereIndex + GetNumSpheres()) )
        {
            textureIndexAvailable = sphereIndex;
        }
        else if (currTextureIndex == sphereIndex)
        {
            textureIndexAvailable = sphereIndex + GetNumSpheres();
        }

        return textureIndexAvailable;
    }

    public float GetDefaultSphereScale()
    {
        return m_defaultSphereScale;
    }

    public float GetScalingFactor()
    {
        return m_scalingFactor;
    }

    public int GetNumSpheres()
    {
        return m_imageSpheres.GetLength(0);
    }

    //TODO: Have an underlying texture reserved just for the Loading image!
    public void SetAllImageSpheresToLoading()
    {
        for (int sphereIndex = 0; sphereIndex < GetNumSpheres(); sphereIndex++)
        {
            SetImageAndFilePathAtIndex(sphereIndex, m_loadingSphereTexture, kLoadingTextureFilePath, -1);
        }
    }

    public void SetImageAndFilePathAtIndex(int sphereIndex, Texture2D texture, string filePath, int pluginTextureIndex)
    {
        if (0 <= sphereIndex && sphereIndex < GetNumSpheres())
        {
            m_imageSpheres[sphereIndex].GetComponent<ImageSphere>().SetImageAndFilePath(texture, filePath, pluginTextureIndex);
        }
        else
        {
            Debug.Log("------- VREEL: Invalid request to SetImageAndFilePathAtIndex: " + sphereIndex);
        }
    }

    public void HideAllImageSpheres()
    {
        for (int sphereIndex = 0; sphereIndex < GetNumSpheres(); sphereIndex++)
        {
            HideSphereAtIndex(sphereIndex);
        }
    }

    public void HideSphereAtIndex(int sphereIndex)
    {
        if (0 <= sphereIndex && sphereIndex < GetNumSpheres())
        {
            m_imageSpheres[sphereIndex].GetComponent<ImageSphere>().Hide();
        }
        else
        {
            Debug.Log("------- VREEL: Invalid request to HideSphereAtIndex: " + sphereIndex);
        }
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void SetIndexOnAllImageSpheres()
    {
        for (int sphereIndex = 0; sphereIndex < GetNumSpheres(); sphereIndex++)
        {
            m_imageSpheres[sphereIndex].GetComponent<ImageSphere>().SetSphereIndex(sphereIndex);
        }
    }

    private void ForceHideAllImageSpheres()
    {
        for (int sphereIndex = 0; sphereIndex < GetNumSpheres(); sphereIndex++)
        {
            m_imageSpheres[sphereIndex].GetComponent<ImageSphere>().ForceHide();
        }
    }
}