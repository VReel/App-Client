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
    [SerializeField] private ImageLoader m_imageLoader;
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

    public void SetTextureInUse(int textureID, bool inUse)
    {
        m_imageLoader.SetTextureInUse(textureID, inUse);
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
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Invalid request to GetIdentifierAtIndex(int sphereIndex): " + sphereIndex);
        }

        return identifier;
    }

    public void SetAllImageSpheresToLoading()
    {
        m_coroutineQueue.EnqueueAction(SetAllImageSpheresToLoadingInternal());
    }
        
    public void HideAllImageSpheres()
    {
        m_coroutineQueue.EnqueueAction(HideAllImageSpheresInternal());
    }

    public void SetImageWithId(string imageIdentifier, Texture2D texture, int pluginTextureIndex)
    {
        int sphereIndex = ConvertIdToIndex(imageIdentifier);
        SetImageAtIndex(sphereIndex, texture, imageIdentifier, pluginTextureIndex, false);
    }

    public void SetImageAtIndex(int sphereIndex, Texture2D texture, string imageIdentifier, int pluginTextureIndex, bool animateOnSet)
    {
        if (sphereIndex == -1)
        {
            m_imageSkybox.SetImage(texture, imageIdentifier, pluginTextureIndex);
        }
        else if (0 <= sphereIndex && sphereIndex < GetNumSpheres())
        {
            m_imageSpheres[sphereIndex].GetComponent<ImageSphere>().SetImage(texture, imageIdentifier, pluginTextureIndex, animateOnSet);
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Invalid request to SetImageAtIndex: " + sphereIndex);
        }
    }    

    public void SetMetadataAtIndex(int sphereIndex, string handle, string caption, int likes)
    {
        if (sphereIndex == -1)
        {
            //m_imageSkybox.SetMetadata(handle, caption, likes);
        }
        else if (0 <= sphereIndex && sphereIndex < GetNumSpheres())
        {
            m_imageSpheres[sphereIndex].GetComponent<ImageSphere>().SetMetadata(handle, caption, likes);
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Invalid request to SetMetadataAtIndex: " + sphereIndex);
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

    public IEnumerator SetAllImageSpheresToLoadingInternal()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling SetAllImageSpheresToLoading()");

        for (int sphereIndex = 0; sphereIndex < GetNumSpheres(); sphereIndex++)
        {            
            SetImageAtIndex(sphereIndex, m_sphereLoadingTexture, kLoadingTextureFilePath, m_imageLoader.GetLoadingTextureIndex(), true);
            yield return null; // Only calling SetImageAtIndex() once per frame
        }
    }

    public IEnumerator HideAllImageSpheresInternal()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling HideAllImageSpheres()");

        for (int sphereIndex = 0; sphereIndex < GetNumSpheres(); sphereIndex++)
        {
            HideSphereAtIndex(sphereIndex);
            yield return null; // Only calling HideSphereAtIndex() once per frame
        }
    }

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

    private int ConvertIdToIndex(string imageIdentifier)
    {
        int sphereIndex = 0;
        for (; sphereIndex < GetNumSpheres(); sphereIndex++)
        {
            if (m_imageSpheres[sphereIndex].GetComponent<ImageSphere>().GetImageIdentifier().CompareTo(imageIdentifier) == 0)
            {
                break;
            }
        }

        return sphereIndex;
    }
}