using UnityEngine;
using UnityEngine.VR;       //VRSettings

public class Carousel : MonoBehaviour
{        
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private bool m_active = true;
    [SerializeField] private float m_rotationalSwipeSenstivity = 0.1f; // sensitivity of rotation effect
    [SerializeField] private float m_rotationalDamp = 1.0f; // damping on rotation effect
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;
    [SerializeField] private CarouselElement[] m_carouselItems;

    // Rotation effect vars
    const float kRotationEpsilon = 0.1f;
    public float m_currRotationalSpeed = 0.0f;
    public bool m_isBeingManuallyRotated = false;

    private float m_rotationalSpeedOnRelease = 0.0f;
    private float m_touchXPosOnDown;
    private float m_touchXPosLastFrame;

    // **************************
    // Public functions
    // **************************

    public void Update()
    {      
        if (!m_active)
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

    public void OnScrollSelectedDown()
    {       
        m_touchXPosLastFrame = Input.mousePosition.x;
    }

    // **************************
    // Private/Helper functions
    // **************************
                   
    private void UpdateRotationEffect()
    {
        if (m_menuButton.GetButtonDown() && m_menuButton.GetGazeOver())
        {            
            float movementInFrame = (Input.mousePosition.x - m_touchXPosLastFrame) * m_rotationalSwipeSenstivity;
            for (int i = 0; i < m_carouselItems.Length; i++)
            {
                m_carouselItems[i].SetDistAlongSpline(m_carouselItems[i].GetDistAlongSpline() + movementInFrame);
            }


            const float kSpeedDivisionFactor = 10.0f;
            float diffInXFromLastFrame = Input.mousePosition.x - m_touchXPosLastFrame;
            m_touchXPosLastFrame = Input.mousePosition.x;
            m_currRotationalSpeed = (diffInXFromLastFrame * Time.fixedDeltaTime) + (m_currRotationalSpeed / kSpeedDivisionFactor); // always taking last speed slightly into account
            m_isBeingManuallyRotated |= Mathf.Abs(m_currRotationalSpeed) > kRotationEpsilon;

            m_rotationalSpeedOnRelease = m_currRotationalSpeed;
        }
        else
        {
            float newAngle = m_currRotationalSpeed;
            //TODO...

            float absCurrSpeed = Mathf.Max(Mathf.Abs(m_currRotationalSpeed) - (Mathf.Abs(m_rotationalSpeedOnRelease) * (m_rotationalDamp * Time.fixedDeltaTime)), 0.0f);
            m_currRotationalSpeed = absCurrSpeed * (m_currRotationalSpeed > 0.0f ? 1.0f : -1.0f);

            m_isBeingManuallyRotated = false;
        }
    }
}