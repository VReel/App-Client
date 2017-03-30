using UnityEngine;
using System.Collections;

public class FPSSystem : MonoBehaviour
{
    // **************************
    // Member Variables
    // **************************

    public bool m_isFPSTextVisible = false;
    public bool m_isDebugMessagesOn = false;
    public float m_fpsTextFontSize = 2.0f / 50.0f;
    public float m_garbageCollectionTimeFreq = 2.0f; // Frequency of which Garbage Collection occurs - only occurs if we are in frame!

    private const float kFrameOutThreshold = 55.0f;
    private float m_deltaTime = 0.0f;
    private float m_garbageCollectionTimeSinceLast = 0.0f;

    // **************************
    // Public functions
    // **************************

    void Update()
    {
        m_deltaTime += (Time.deltaTime - m_deltaTime) * 0.1f;
        m_garbageCollectionTimeSinceLast += Time.deltaTime;

        float fps = 1.0f / m_deltaTime;
        if (fps < kFrameOutThreshold)
        {
            if (m_isDebugMessagesOn && Debug.isDebugBuild) Debug.Log("------- VREEL: We are Framing out at FPS = " + fps);
        }
        else if (m_garbageCollectionTimeSinceLast < m_garbageCollectionTimeFreq)
        {            
            Resources.UnloadUnusedAssets();
            m_garbageCollectionTimeSinceLast = 0.0f;
        }
    }

    void OnGUI()
    {
        if (!m_isFPSTextVisible || !Debug.isDebugBuild)
        {
            return;
        }

        float msec = m_deltaTime * 1000.0f;
        float fps = 1.0f / m_deltaTime;

        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = (int) (Screen.height * m_fpsTextFontSize);

        if (fps < kFrameOutThreshold)
        {
            style.normal.textColor = new Color (0.5f, 0.0f, 0.0f, 1.0f);
        }
        else 
        {
            style.normal.textColor = new Color (0.0f, 0.0f, 0.0f, 1.0f);
        }

        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        Rect rect = new Rect(0, 0, Screen.width, Screen.height * m_fpsTextFontSize);
        GUI.Label(rect, text, style);
    }
}