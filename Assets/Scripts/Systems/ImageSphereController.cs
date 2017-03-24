using UnityEngine;
using UnityEngine.VR;               // VRSettings
using UnityEngine.UI;               // Text
using System;                       // string.Join
using System.Collections;           // IEnumerator
using System.Collections.Generic;   // List
using System.IO;                    // Stream
using System.Net;                   // HttpWebRequest

public class ImageSphereController : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private float m_defaultSphereScale = 1.0f;
    [SerializeField] private float m_scalingFactor = 0.88f;
    [SerializeField] private CppPlugin m_cppPlugin;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private GameObject[] m_imageSpheres;
    [SerializeField] private Texture2D m_sphereLoadingTexture;

    private string kLoadingTextureFilePath = "LoadingImage";

    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        ForceHideAllImageSpheres();
        SetIndexOnAllImageSpheres();
    }
        
    public void LoadImageFromPathIntoImageSphere(int sphereIndex, string filePathAndIdentifier, bool showLoading)
    {
        m_cppPlugin.LoadImageFromPathIntoImageSphere(this, sphereIndex, filePathAndIdentifier, showLoading);
    }

    public void LoadImageFromURLIntoImageSphere(string url, int sphereIndex, string imageIdentifier, bool showLoading)
    {
        m_cppPlugin.LoadImageFromURLIntoImageSphere(this, sphereIndex, url, imageIdentifier, showLoading);
    }

    public void SetTextureInUse(int textureID, bool inUse)
    {
        m_cppPlugin.SetTextureInUse(textureID, inUse);
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

    public string GetIdentifierAtIndex(int sphereIndex)
    {
        string identifier = "invalid";
        if (sphereIndex == -1)
        {
            identifier = m_imageSkybox.GetImageIdentifier();
        }
        else if (0 <= sphereIndex && sphereIndex < GetNumSpheres())
        {
            identifier = m_imageSpheres[sphereIndex].GetComponent<ImageSphere>().GetImageIdentifier();
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Invalid request to     public string GetIdentifierAtIndex(int sphereIndex)\n: " + sphereIndex);
        }

        return identifier;
    }

    public void SetAllImageSpheresToLoading()
    {
        for (int sphereIndex = 0; sphereIndex < GetNumSpheres(); sphereIndex++)
        {
            SetImageAtIndex(sphereIndex, m_sphereLoadingTexture, kLoadingTextureFilePath, m_cppPlugin.GetLoadingTextureIndex());
        }
    }

    public void SetImageAtIndex(int sphereIndex, Texture2D texture, string imageIdentifier, int pluginTextureIndex)
    {
        if (sphereIndex == -1)
        {
            m_imageSkybox.SetImage(texture, imageIdentifier, pluginTextureIndex);
        }
        else if (0 <= sphereIndex && sphereIndex < GetNumSpheres())
        {
            m_imageSpheres[sphereIndex].GetComponent<ImageSphere>().SetImage(texture, imageIdentifier, pluginTextureIndex);
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Invalid request to SetImageAndFilePathAtIndex: " + sphereIndex);
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
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Invalid request to HideSphereAtIndex: " + sphereIndex);
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