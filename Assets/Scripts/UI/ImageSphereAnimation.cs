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
    [SerializeField] private float m_rotationalDamp = 5.0f; // damping on rotation effect

    // Float effect vars
    private const float kLengthOfSine = 2 * Mathf.PI;
    private Vector3 m_originalPos;
    private Vector3 m_currPos;
    private Vector3 m_currTimeVec;
    private Vector3 m_currTimeVecSign;

    // Rotation effect vars
    private VRStandardAssets.Menu.MenuButton m_menuButton;
    private float m_currRotationalSpeed = 0.0f;
    private Vector3 m_hitPointOnDown;
    private Vector3 m_newHitPointOnOver;
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
        const float kEpsilon = 0.1f;
        return (Mathf.Abs(m_currRotationalSpeed) > kEpsilon);
    }

    public void OnSphereSelectedDown()
    {
        Vector3 sphereHitPoint = m_eyeRayCaster.LastRaycastHit.point;
        Vector3 flattenedSphereHitPoint = (sphereHitPoint - (Vector3.Dot(sphereHitPoint, Vector3.up) * Vector3.up));
        m_hitPointOnDown = flattenedSphereHitPoint;
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
            Vector3 sphereHitPoint = m_eyeRayCaster.LastRaycastHit.point;
            Vector3 flattenedSphereHitPoint = (sphereHitPoint - (Vector3.Dot(sphereHitPoint, Vector3.up) * Vector3.up));
            m_newHitPointOnOver = flattenedSphereHitPoint;                

            Vector3 hitPointOut = Vector3.Cross(Vector3.up, m_hitPointOnDown);
            float angleSign = Vector3.Dot(hitPointOut, m_newHitPointOnOver) > 0.0f ? 1.0f : -1.0f;

            float newAngle = Vector3.Angle(transform.position - m_hitPointOnDown, transform.position - m_newHitPointOnOver);
            newAngle *= angleSign;

            Quaternion newRot = m_rotOnDown * Quaternion.Euler(0, newAngle, 0);
            const float kDivisionFactor = 10.0f;
            m_currRotationalSpeed = (Quaternion.Angle(transform.localRotation, newRot) * angleSign) / kDivisionFactor;

            transform.localRotation = newRot;
        }
        else
        {
            float absCurrSpeed = Mathf.Abs(m_currRotationalSpeed) - (m_rotationalDamp * Time.fixedDeltaTime);
            m_currRotationalSpeed = absCurrSpeed * (m_currRotationalSpeed > 0.0f ? 1.0f : -1.0f);

            transform.RotateAround(transform.position, Vector3.up, m_currRotationalSpeed);
        }

        /*
        if (Debug.isDebugBuild)
        {
            if (m_hitPointOnDown.sqrMagnitude > 0.0f)
            {
                DrawDebugLine(m_hitPointOnDown, transform.position, Color.red);
                if (Debug.isDebugBuild) Debug.Log("------- VREEL: UpdateRotationEffect() - m_hitPointOnDown: " + m_hitPointOnDown + " transform.position: " + transform.position);
            }

            if (m_newHitPointOnOver.sqrMagnitude > 0.0f)
            {
                DrawDebugLine(m_newHitPointOnOver, transform.position, Color.green);
                if (Debug.isDebugBuild) Debug.Log("------- VREEL: UpdateRotationEffect() - m_newHitPointOnOver: " + m_newHitPointOnOver + " transform.position: " + transform.position);
            }
        }
        */
    }

    private int RandomSign() 
    {
        return Random.value < 0.5f? 1 : -1;
    }

    private void DrawDebugLine(Vector3 start, Vector3 end, Color color, float duration = 0.016f)
    {
        GameObject myLine = new GameObject();
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        //lr.material = new Material(Shader.Find("Standard"));
        //lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = 0.1f;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        GameObject.Destroy(myLine, duration);
    }
}