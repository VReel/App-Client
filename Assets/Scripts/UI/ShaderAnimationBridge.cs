using UnityEngine;

// This component allows you to update pre-defined shader params, 
// hence animating them even if they don't appear as an option on the animation timeline
public class ShaderAnimationBridge : MonoBehaviour
{        
    // **************************
    // Member Variables
    // **************************

    /*
    public float m_alpha
    {
        get 
        {
            return GetAlpha();
        }
        set 
        {
            SetAlpha(value);
        }
    }
    */

    public float m_alpha = 1;

    private float m_previousAlpha = 1;
    private Material m_myMaterial;

    // **************************
    // Public functions
    // **************************   

    public void Start()
    {
        m_myMaterial = gameObject.GetComponent<MeshRenderer>().material;
        //m_alpha = m_previousAlpha = GetAlpha();
    }

    public void Update()
    {
        if (m_alpha != m_previousAlpha)
        {
            SetAlpha(m_alpha);
        }
    }

    public float GetAlpha()
    {
        return m_myMaterial.GetFloat("_Alpha");
    }

    public void SetAlpha(float alpha)
    {
        m_myMaterial.SetFloat("_Alpha", alpha);
    }
}