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
    [SerializeField] private ImageSphere m_profileImageSphere;
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
        m_coroutineQueue.EnqueueAction(HideAllImageSpheresInternal(false));
    }

    public void ForceHideAllImageSpheres()
    {
        m_coroutineQueue.EnqueueAction(HideAllImageSpheresInternal(true));
    }

    public void SetImageWithId(string imageIdentifier, Texture2D texture, int pluginTextureIndex)
    {
        int sphereIndex = ConvertIdToIndex(imageIdentifier);
        SetImageAtIndex(sphereIndex, texture, imageIdentifier, pluginTextureIndex, false);
    }

    public void SetImageAtIndex(int sphereIndex, Texture2D texture, string imageIdentifier, int pluginTextureIndex, bool animateOnSet)
    {
        if (sphereIndex == Helper.kSkyboxSphereIndex)
        {
            m_imageSkybox.SetImage(texture, imageIdentifier, pluginTextureIndex);
        }
        if (sphereIndex == Helper.kProfileSphereIndex)
        {
            m_profileImageSphere.SetImage(texture, imageIdentifier, pluginTextureIndex, animateOnSet);
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

    public void SetMetadataAtIndex(int sphereIndex, string userId, string handle, string caption, int likes, bool likedByMe)
    {
        if (0 <= sphereIndex && sphereIndex < GetNumSpheres())
        {
            m_imageSpheres[sphereIndex].GetComponent<ImageSphere>().SetMetadata(userId, handle, caption, likes, likedByMe);
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Invalid request to SetMetadataAtIndex: " + sphereIndex);
        }
    }    

    public void HideSphereAtIndex(int sphereIndex, bool forceHide = false)
    {
        ImageSphere imageSphereAtIndex = null;

        if (sphereIndex == Helper.kProfileSphereIndex)
        {
            imageSphereAtIndex = m_profileImageSphere;
        }
        else if (0 <= sphereIndex && sphereIndex < GetNumSpheres())
        {
            imageSphereAtIndex = m_imageSpheres[sphereIndex].GetComponent<ImageSphere>();
        }
        else
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Invalid request to HideSphereAtIndex: " + sphereIndex);
        }

        if (imageSphereAtIndex != null)
        {
            if (forceHide)
            {
                imageSphereAtIndex.ForceHide();
            }
            else
            {
                imageSphereAtIndex.Hide();
            }
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
        }
        yield break;
    }

    public IEnumerator HideAllImageSpheresInternal(bool forceHide)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling HideAllImageSpheres() with ForceHide set to: " + forceHide);

        for (int sphereIndex = 0; sphereIndex < GetNumSpheres(); sphereIndex++)
        {
            HideSphereAtIndex(sphereIndex, forceHide);
        }
        yield break;
    }

    private void SetIndexOnAllImageSpheres()
    {
        for (int sphereIndex = 0; sphereIndex < GetNumSpheres(); sphereIndex++)
        {
            m_imageSpheres[sphereIndex].GetComponent<ImageSphere>().SetSphereIndex(sphereIndex);
        }

        m_profileImageSphere.SetSphereIndex(Helper.kProfileSphereIndex);
    }
        
    private int ConvertIdToIndex(string imageIdentifier)
    {
        int sphereIndex = 0;
        if (m_profileImageSphere.GetImageIdentifier().CompareTo(imageIdentifier) == 0)
        {
            sphereIndex = Helper.kProfileSphereIndex;
        }
        else
        {
            for (; sphereIndex < GetNumSpheres(); sphereIndex++)
            {
                if (m_imageSpheres[sphereIndex].GetComponent<ImageSphere>().GetImageIdentifier().CompareTo(imageIdentifier) == 0)
                {
                    break;
                }
            }
        }
            
        return sphereIndex;
    }
}