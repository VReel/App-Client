using UnityEngine;
using System.Collections;

public class VRCameraController : MonoBehaviour 
{

    public float m_upRate = 1f;
    public float m_rightRate = 1f;

	void Update () 
    {
        
        if (Input.GetKey(KeyCode.DownArrow))
        {
            transform.Rotate(Vector3.right * Time.deltaTime * m_upRate);
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            transform.Rotate(Vector3.right * Time.deltaTime * -m_upRate);
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Rotate(Vector3.up * Time.deltaTime * m_rightRate);
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Rotate(Vector3.up * Time.deltaTime * -m_rightRate);
        }           

	}
}
