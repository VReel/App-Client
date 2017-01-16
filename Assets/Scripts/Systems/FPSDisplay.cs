using UnityEngine;
using System.Collections;

public class FPSDisplay : MonoBehaviour
{
    // **************************
    // Member Variables
    // **************************

    public bool m_isVisible = false;
    public float m_fontSize = 2 / 50;

    private const float kFrameOutThreshold = 55.0f;
    private float m_deltaTime = 0.0f;

    // **************************
    // Public functions
    // **************************

    void Update()
    {
        m_deltaTime += (Time.deltaTime - m_deltaTime) * 0.1f;

        float fps = 1.0f / m_deltaTime;
        if (fps < kFrameOutThreshold)
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: We are Framing out at FPS = " + fps);
        }
    }

    void OnGUI()
    {
        if (!m_isVisible || !Debug.isDebugBuild)
        {
            return;
        }

        float msec = m_deltaTime * 1000.0f;
        float fps = 1.0f / m_deltaTime;

        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = (int) (Screen.height * m_fontSize);

        if (fps < kFrameOutThreshold)
        {
            style.normal.textColor = new Color (0.5f, 0.0f, 0.0f, 1.0f);
        }
        else 
        {
            style.normal.textColor = new Color (0.0f, 0.0f, 0.0f, 1.0f);
        }

        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        Rect rect = new Rect(0, 0, Screen.width, Screen.height * m_fontSize);
        GUI.Label(rect, text, style);
    }
}