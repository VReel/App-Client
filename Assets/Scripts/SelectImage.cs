using UnityEngine;
using System.Collections;

public class SelectImage : MonoBehaviour 
{

    public Material m_image = null;

    void OnMouseDown()
    {
        if (m_image != null)
        {
            RenderSettings.skybox = m_image;
        }
    }

}
