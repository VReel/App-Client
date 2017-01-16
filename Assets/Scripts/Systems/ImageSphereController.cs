using UnityEngine;
using UnityEngine.VR;               // VRSettings
using UnityEngine.UI;               // Text
using System;                       // string.Join
using System.Collections;           // IEnumerator
using System.Collections.Generic;   //List
using System.IO;                    // Stream

public class ImageSphereController : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private float m_defaultSphereScale = 1.0f;
    [SerializeField] private float m_scalingFactor = 0.88f;
    [SerializeField] private GameObject[] m_imageSpheres;
    [SerializeField] private Texture2D m_sphereLoadingTexture;

    private const int kLoadingTextureIndex = -1;
    private string kLoadingTextureFilePath = "LoadingImage";

    private const int kMaxNumTextures = 9; // 5 ImageSpheres + 1 Skybox + 3 spare textures
    private int[] m_textureIndexUsage;
    private CppPlugin m_cppPlugin;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        m_cppPlugin = new CppPlugin(this, kMaxNumTextures);
        m_textureIndexUsage = new int[kMaxNumTextures];

        ForceHideAllImageSpheres();
        SetIndexOnAllImageSpheres();
    }
        
    public IEnumerator LoadImageFromPath(int sphereIndex, string filePath)
    {
        yield return m_cppPlugin.LoadImageFromPath(this, sphereIndex, filePath, GetAvailableTextureIndex());
    }

    public IEnumerator LoadImageFromStream(Stream stream, int sphereIndex, string filePath)
    {
        yield return m_cppPlugin.LoadImageFromStream(this, sphereIndex, stream, filePath, GetAvailableTextureIndex());
    }

    public void SetTextureInUse(int textureID, bool inUse)
    {
        if (textureID != -1) // -1 is the textureID for the loadingTexture!
        {
            if (inUse)
            {
                ++m_textureIndexUsage[textureID];
            }
            else
            {
                --m_textureIndexUsage[textureID];
            }

            if (Debug.isDebugBuild) Debug.Log("------- VREEL: TextureID = " + textureID + ", InUse = " + inUse);
            DebugPrintTextureIndexUsage();
        }
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

    public void SetAllImageSpheresToLoading()
    {
        for (int sphereIndex = 0; sphereIndex < GetNumSpheres(); sphereIndex++)
        {
            SetImageAndFilePathAtIndex(sphereIndex, m_sphereLoadingTexture, kLoadingTextureFilePath, kLoadingTextureIndex);
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

    private int GetAvailableTextureIndex()
    {
        for (int i = 0; i < kMaxNumTextures; i++)
        {            
            if (m_textureIndexUsage[i] == 0)
            {
                return i;
            }
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - We have no more textures available!!!");
        DebugPrintTextureIndexUsage();
        
        return -1;
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

    private void DebugPrintTextureIndexUsage()
    {
        if (Debug.isDebugBuild) 
            Debug.Log("------- VREEL: " + m_textureIndexUsage[0] + ", " + m_textureIndexUsage[1] + ", " + m_textureIndexUsage[2] 
            + ", " + m_textureIndexUsage[3] + ", " + m_textureIndexUsage[4] + ", " + m_textureIndexUsage[5] 
            + ", " + m_textureIndexUsage[6] + ", " + m_textureIndexUsage[7]); 
        // + ", " + m_textureIndexUsage[8] + ", " + m_textureIndexUsage[9]);
    }
}