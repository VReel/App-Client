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
    public Vector2 m_currRotationalSpeed = Vector2.zero;
    public bool m_isBeingManuallyRotated = false;

    private Vector2 m_rotationalSpeedOnRelease = Vector2.zero;
    private Vector2 m_touchPosOnDown;
    private Vector2 m_touchPosLastFrame;
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
        return (Mathf.Abs(m_currRotationalSpeed.x) + Mathf.Abs(m_currRotationalSpeed.y) > kRotationEpsilon) || m_isBeingManuallyRotated;
    }

    public void OnSphereSelectedDown()
    {       
        m_touchPosOnDown.x = m_touchPosLastFrame.x = Input.mousePosition.x;
        m_touchPosOnDown.y = m_touchPosLastFrame.y = Input.mousePosition.y;
        m_rotOnDown = transform.localRotation;
    }

    // **************************
    // Private/Helper functions
    // **************************                   

    private void UpdateRotationEffect()
    {
        if (m_menuButton.GetButtonDown() && m_menuButton.GetGazeOver())
        {            
            float newXAngle = (Input.mousePosition.x - m_touchPosOnDown.x) * m_rotationalSwipeSenstivity;
            float newYAngle = (Input.mousePosition.y - m_touchPosOnDown.y) * m_rotationalSwipeSenstivity;
            Quaternion newRot = Quaternion.AngleAxis(newYAngle, Vector3.right) * m_rotOnDown * Quaternion.AngleAxis(newXAngle, Vector3.up); // world up, local right - this line took me way too long to figure out
            transform.localRotation = newRot;

            float diffInXFromLastFrame = Input.mousePosition.x - m_touchPosLastFrame.x;
            float diffInYFromLastFrame = Input.mousePosition.y - m_touchPosLastFrame.y;
            m_touchPosLastFrame.x = Input.mousePosition.x;
            m_touchPosLastFrame.y = Input.mousePosition.y;

            const float kSpeedDivisionFactor = 10.0f;
            m_currRotationalSpeed.x = (diffInXFromLastFrame * Time.deltaTime) + (m_currRotationalSpeed.x / kSpeedDivisionFactor); // always taking last speed slightly into account
            m_currRotationalSpeed.y = (diffInYFromLastFrame * Time.deltaTime) + (m_currRotationalSpeed.y / kSpeedDivisionFactor); // always taking last speed slightly into account
            m_isBeingManuallyRotated |= (Mathf.Abs(m_currRotationalSpeed.x) + Mathf.Abs(m_currRotationalSpeed.y) > kRotationEpsilon);

            m_rotationalSpeedOnRelease = m_currRotationalSpeed;
        }
        else
        {                        
            float newXAngle = m_currRotationalSpeed.x;
            float newYAngle = m_currRotationalSpeed.y;
            Quaternion newRot = Quaternion.AngleAxis(newYAngle, Vector3.right) * transform.localRotation * Quaternion.AngleAxis(newXAngle, Vector3.up); // world up, local right - this line took me way too long to figure out
            transform.localRotation = newRot;

            float absCurrXSpeed = Mathf.Max( Mathf.Abs(m_currRotationalSpeed.x) - (Mathf.Abs(m_rotationalSpeedOnRelease.x) * (m_rotationalDamp * Time.deltaTime)), 0.0f);
            m_currRotationalSpeed.x = absCurrXSpeed * (m_currRotationalSpeed.x > 0.0f ? 1.0f : -1.0f);

            float absCurrYSpeed = Mathf.Max( Mathf.Abs(m_currRotationalSpeed.y) - (Mathf.Abs(m_rotationalSpeedOnRelease.y) * (m_rotationalDamp * Time.deltaTime)), 0.0f);
            m_currRotationalSpeed.y = absCurrYSpeed * (m_currRotationalSpeed.y > 0.0f ? 1.0f : -1.0f);

            m_isBeingManuallyRotated = false;
        }
    }
}  