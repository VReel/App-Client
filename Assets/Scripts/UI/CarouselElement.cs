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

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        m_distAlongSpline = m_startingDistAlongSpline;
    }  

    public float GetDistAlongSpline()
    {
        return m_distAlongSpline;
    }

    public void SetDistAlongSpline(float newDistAlongSpline)
    {       
        m_distAlongSpline = (newDistAlongSpline < 0) ? (newDistAlongSpline + 1.0f) : (newDistAlongSpline % 1.0f);
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