using UnityEngine;
using UnityEngine.VR;       //VRSettings

public class ImageSphereAnimation : MonoBehaviour
{        
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private bool m_active = true;
    [SerializeField] private float m_floatingTimeTakenForFullSineWave = 15; // x second for full sign rotation
    [SerializeField] private float m_floatingMagnitude = 0.03f; // magnitude of variance
    [SerializeField] private float m_floatingChanceOfSignFlip = 0.001f; // chance of flipping float direction
    [SerializeField] private VRStandardAssets.Utils.VREyeRaycaster m_eyeRayCaster;
    [SerializeField] private float m_rotationalSwipeSenstivity = 1.0f; // sensitivity of rotation effect
    [SerializeField] private float m_rotationalDamp = 1.0f; // damping on rotation effect

    // Float effect vars
    private const float kLengthOfSine = 2 * Mathf.PI;
    private Vector3 m_originalPos;
    private Vector3 m_currPos;
    private Vector3 m_currTimeVec;
    private Vector3 m_currTimeVecSign;

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
        m_originalPos = transform.localPosition;
        m_currPos = m_originalPos;
        m_currTimeVec = new Vector3( Random.Range(0.0f, kLengthOfSine), Random.Range(0.0f, kLengthOfSine), Random.Range(0.0f, kLengthOfSine));
        m_currTimeVecSign = new Vector3( RandomSign(), RandomSign(), RandomSign());

        m_menuButton = GetComponent<VRStandardAssets.Menu.MenuButton>();
    }

    public void Update()
    {      
        if (!m_active)
        {
            return;
        }

        UpdateFloatEffect();

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

    private void UpdateFloatEffect()
    {
        float updateBasedOnFullTimeTaken = kLengthOfSine/m_floatingTimeTakenForFullSineWave;

        m_currTimeVec.x = (m_currTimeVec.x + m_currTimeVecSign.x *
            ((Time.fixedDeltaTime * updateBasedOnFullTimeTaken))) // + Random.Range(0.0f, updateBasedOnFullTimeTaken) * m_randomVariation)) 
            % kLengthOfSine;
        m_currTimeVec.y = (m_currTimeVec.y + m_currTimeVecSign.y *
            ((Time.fixedDeltaTime * updateBasedOnFullTimeTaken))) // + Random.Range(0.0f, updateBasedOnFullTimeTaken) * m_randomVariation)) 
            % kLengthOfSine;
        m_currTimeVec.z = (m_currTimeVec.z + m_currTimeVecSign.z *
            ((Time.fixedDeltaTime * updateBasedOnFullTimeTaken)))// + Random.Range(0.0f, updateBasedOnFullTimeTaken) * m_randomVariation)) 
            % kLengthOfSine;

        if (Random.value < m_floatingChanceOfSignFlip) m_currTimeVecSign.x *= -1;
        if (Random.value < m_floatingChanceOfSignFlip) m_currTimeVecSign.y *= -1;
        if (Random.value < m_floatingChanceOfSignFlip) m_currTimeVecSign.z *= -1;

        m_currPos.x = m_originalPos.x + (Mathf.Sin(m_currTimeVec.x) * m_floatingMagnitude);
        m_currPos.y = m_originalPos.y + (Mathf.Sin(m_currTimeVec.y) * m_floatingMagnitude);
        m_currPos.z = m_originalPos.z + (Mathf.Sin(m_currTimeVec.z) * m_floatingMagnitude);
        transform.localPosition = m_currPos;

        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Transform LocalPos: " + m_currPos.ToString("F5") + ", CurrTimeVector: " + m_currTimeVec);
    }
                   
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