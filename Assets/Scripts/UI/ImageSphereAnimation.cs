using UnityEngine;
using UnityEngine.UI;

public class ImageSphereAnimation : MonoBehaviour
{        
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private bool m_active = true;
    [SerializeField] private float m_timeTakenForFullSineWave = 10; // x second for full sign rotation
    [SerializeField] private float m_magnitude = 0.015f; // magnitude of variance
    [SerializeField] private float m_ChanceOfSignFlip = 0.001f; // magnitude of randomVariance

    private const float kLengthOfSine = 2 * Mathf.PI;

    private Vector3 m_originalPos;
    private Vector3 m_currPos;
    private Vector3 m_currTimeVec;
    private Vector3 m_currTimeVecSign;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        m_originalPos = transform.localPosition;
        m_currPos = m_originalPos;
        m_currTimeVec = new Vector3( Random.Range(0.0f, kLengthOfSine), Random.Range(0.0f, kLengthOfSine), Random.Range(0.0f, kLengthOfSine));
        m_currTimeVecSign = new Vector3( RandomSign(), RandomSign(), RandomSign());
    }

    public void Update()
    {      
        if (!m_active)
        {
            return;
        }

        float updateBasedOnFullTimeTaken = kLengthOfSine/m_timeTakenForFullSineWave;

        m_currTimeVec.x = (m_currTimeVec.x + m_currTimeVecSign.x *
            ((Time.fixedDeltaTime * updateBasedOnFullTimeTaken))) // + Random.Range(0.0f, updateBasedOnFullTimeTaken) * m_randomVariation)) 
            % kLengthOfSine;
        m_currTimeVec.y = (m_currTimeVec.y + m_currTimeVecSign.y *
                ((Time.fixedDeltaTime * updateBasedOnFullTimeTaken))) // + Random.Range(0.0f, updateBasedOnFullTimeTaken) * m_randomVariation)) 
            % kLengthOfSine;
        m_currTimeVec.z = (m_currTimeVec.z + m_currTimeVecSign.z *
                    ((Time.fixedDeltaTime * updateBasedOnFullTimeTaken)))// + Random.Range(0.0f, updateBasedOnFullTimeTaken) * m_randomVariation)) 
            % kLengthOfSine;

        if (Random.value < m_ChanceOfSignFlip) m_currTimeVecSign.x *= -1;
        if (Random.value < m_ChanceOfSignFlip) m_currTimeVecSign.y *= -1;
        if (Random.value < m_ChanceOfSignFlip) m_currTimeVecSign.z *= -1;

        m_currPos.x = m_originalPos.x + (Mathf.Sin(m_currTimeVec.x) * m_magnitude);
        m_currPos.y = m_originalPos.y + (Mathf.Sin(m_currTimeVec.y) * m_magnitude);
        m_currPos.z = m_originalPos.z + (Mathf.Sin(m_currTimeVec.z) * m_magnitude);
        transform.localPosition = m_currPos;

        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Transform LocalPos: " + m_currPos.ToString("F5") + ", CurrTimeVector: " + m_currTimeVec);
    }       

    public bool IsActive()
    {
        return m_active;
    }

    public void SetActive(bool active)
    {
        m_active = active;
    }

    // **************************
    // Private/Helper functions
    // **************************

    private int RandomSign() 
    {
        return Random.value < 0.5f? 1 : -1;
    }
}