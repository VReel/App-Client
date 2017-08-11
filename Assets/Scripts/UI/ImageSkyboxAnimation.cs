using UnityEngine;
using UnityEngine.VR;       //VRSettings

public class ImageSkyboxAnimation : MonoBehaviour
{        
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private bool m_active = true;
    [SerializeField] private float m_rotationalSwipeSenstivity = 1.0f; // sensitivity of rotation effect
    [SerializeField] private float m_rotationalDamp = 1.0f; // damping on rotation effect

    // Rotation effect vars
    const float kRotationEpsilon = 0.1f;
    private VRStandardAssets.Menu.MenuButton m_menuButton;
    public float m_currRotationalSpeed = 0.0f;
    public bool m_isBeingManuallyRotated = false;

    private float m_touchXPosOnDown;
    private float m_touchXPosLastFrame;
    private Quaternion m_rotOnDown;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        m_menuButton = GetComponent<VRStandardAssets.Menu.MenuButton>();
    }

    public void Update()
    {      
        if (!m_active || VRSettings.enabled)
        {
            return;
        }

        UpdateRotationEffect();
    }       

    public bool IsActive()
    {
        return m_active;
    }

    public void SetActive(bool active)
    {
        m_active = active;
    }

    public bool IsRotating()
    {        
        return (Mathf.Abs(m_currRotationalSpeed) > kRotationEpsilon) || m_isBeingManuallyRotated;
    }

    public void OnSphereSelectedDown()
    {       
        m_touchXPosOnDown = m_touchXPosLastFrame = Input.mousePosition.x;
        m_rotOnDown = transform.localRotation;
    }

    // **************************
    // Private/Helper functions
    // **************************
                   
    private void UpdateRotationEffect()
    {
        if (m_menuButton.GetButtonDown() && m_menuButton.GetGazeOver())
        {            
            float newAngle = (Input.mousePosition.x - m_touchXPosOnDown) * m_rotationalSwipeSenstivity;
            Quaternion newRot = m_rotOnDown * Quaternion.Euler(0, newAngle, 0);
            transform.localRotation = newRot;

            const float kSpeedDivisionFactor = 10.0f;
            float diffInXFromLastFrame = Input.mousePosition.x - m_touchXPosLastFrame;
            m_touchXPosLastFrame = Input.mousePosition.x;
            m_currRotationalSpeed = (diffInXFromLastFrame * Time.fixedDeltaTime) + (m_currRotationalSpeed / kSpeedDivisionFactor); // always taking last speed slightly into account
            m_isBeingManuallyRotated |= Mathf.Abs(m_currRotationalSpeed) > kRotationEpsilon;
        }
        else
        {
            float absCurrSpeed = Mathf.Max(Mathf.Abs(m_currRotationalSpeed) - (m_rotationalDamp * Time.fixedDeltaTime), 0.0f);
            m_currRotationalSpeed = absCurrSpeed * (m_currRotationalSpeed > 0.0f ? 1.0f : -1.0f);

            transform.RotateAround(transform.position, Vector3.up, m_currRotationalSpeed);

            m_isBeingManuallyRotated = false;
        }
    }

    private int RandomSign() 
    {
        return Random.value < 0.5f? 1 : -1;
    }
}