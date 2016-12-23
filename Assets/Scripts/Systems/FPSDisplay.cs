using UnityEngine;
using System.Collections;

public class FPSDisplay : MonoBehaviour
{
    // **************************
    // Member Variables
    // **************************

    public bool m_isVisible = false;
    public Color m_textColour = new Color(0.0f, 0.0f, 0.0f, 1.0f);
    public float m_fontSize = 2 / 50;

    private float m_deltaTime = 0.0f;

    // **************************
    // Public functions
    // **************************

    void Update()
    {
        m_deltaTime += (Time.deltaTime - m_deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        if (!m_isVisible)
        {
            return;
        }

        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = (int) (Screen.height * m_fontSize);
        style.normal.textColor = m_textColour;

        float msec = m_deltaTime * 1000.0f;
        float fps = 1.0f / m_deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);

        Rect rect = new Rect(0, 0, Screen.width, Screen.height * m_fontSize);
        GUI.Label(rect, text, style);
    }
}