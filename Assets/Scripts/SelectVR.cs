using UnityEngine;
using UnityEngine.VR;
using System.Collections;

public class SelectVR : MonoBehaviour 
{
    void Awake()
    {
        UnityEngine.VR.VRSettings.enabled = false;
    }

    void OnMouseDown()
    {       
        VRSettings.enabled = !VRSettings.enabled;
    }
}