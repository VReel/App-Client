using UnityEngine;
using UnityEngine.VR;       //VRSettings

public class CarouselElement : MonoBehaviour
{        
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private Battlehub.SplineEditor.Spline m_spline;
    [SerializeField] private float m_startingDistAlongSpline = 0.5f;
       
    private float m_distAlongSpline = 0.0f;
    private ImageSphere m_imageSphere = null;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        m_distAlongSpline = m_startingDistAlongSpline;

        m_imageSphere = gameObject.GetComponent<ImageSphere>();
    }  

    public float GetDistAlongSpline()
    {
        return m_distAlongSpline;
    }

    public void SetDistAlongSpline(float newDistAlongSpline)
    {               
        if (newDistAlongSpline < 0.0f)
        {
            newDistAlongSpline += 1.0f;

            if (m_imageSphere != null)
            {
                m_imageSphere.NextImage();
            }
        }

        if (newDistAlongSpline > 1.0f)
        {
            newDistAlongSpline -= 1.0f;

            if (m_imageSphere != null)
            {
                m_imageSphere.PrevImage();
            }
        }

        m_distAlongSpline = newDistAlongSpline;
    }

    public void ResetToStartingDistAlongSpline()
    {
        m_distAlongSpline = m_startingDistAlongSpline;
    }
        
    public void Update()
    {      
        transform.position = m_spline.GetPoint(m_distAlongSpline);
        transform.LookAt(Camera.current.transform);
        transform.RotateAround(transform.position, Vector3.up, 180);
    }       

    // **************************
    // Private/Helper functions
    // **************************
                   
}