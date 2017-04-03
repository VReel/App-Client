using UnityEngine;
using System.Collections;

public class FPSDisplay : MonoBehaviour
{
    // **************************
    // Member Variables
    // **************************

<<<<<<< HEAD:Assets/Scripts/Systems/FPSDisplay.cs
    public bool m_isVisible = false;
    public bool m_isDebugOn = false;
    public float m_fontSize = 2 / 50;
=======
    public bool m_isFPSTextVisible = false;
    public bool m_isDebugMessagesOn = false;
    public float m_fpsTextFontSize = 2.0f / 50.0f;
    //public float m_garbageCollectionTimeFreq = 2.0f; // Frequency of which Garbage Collection occurs - only occurs if we are in frame!
>>>>>>> WIP:Assets/Scripts/Systems/FPSSystem.cs

    private const float kFrameOutThreshold = 54.0f;
    private float m_deltaTime = 0.0f;
<<<<<<< HEAD:Assets/Scripts/Systems/FPSDisplay.cs
=======
    //private float m_garbageCollectionTimeSinceLast = 0.0f;
>>>>>>> WIP:Assets/Scripts/Systems/FPSSystem.cs

    // **************************
    // Public functions
    // **************************

    void Update()
    {
        m_deltaTime += (Time.deltaTime - m_deltaTime) * 0.1f;
<<<<<<< HEAD:Assets/Scripts/Systems/FPSDisplay.cs
=======
        //m_garbageCollectionTimeSinceLast += Time.deltaTime;
>>>>>>> WIP:Assets/Scripts/Systems/FPSSystem.cs

        float fps = 1.0f / m_deltaTime;
        if (fps < kFrameOutThreshold)
        {
<<<<<<< HEAD:Assets/Scripts/Systems/FPSDisplay.cs
            if (m_isDebugOn && Debug.isDebugBuild) Debug.Log("------- VREEL: We are Framing out at FPS = " + fps);
=======
            if (m_isDebugMessagesOn && Debug.isDebugBuild) Debug.Log("------- VREEL: We are Framing out at FPS = " + fps);
        }
        /*
        else if (m_garbageCollectionTimeSinceLast > m_garbageCollectionTimeFreq)
        {            
            Resources.UnloadUnusedAssets();
            m_garbageCollectionTimeSinceLast = 0.0f;
>>>>>>> WIP:Assets/Scripts/Systems/FPSSystem.cs
        }
        */
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