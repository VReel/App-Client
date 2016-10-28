using UnityEngine;
using System.Collections;

public class VRCameraController : MonoBehaviour 
{
    public bool m_swipeEnabled = true;      // Should swipe move the camera or not
    public float m_upRate = 1f;
    public float m_rightRate = 1f;

    private bool m_directionLeft = true;

    [SerializeField] private VRStandardAssets.Utils.VRInput m_input;

	public void Update () 
    {
        // --- TEMP
        /*
        if (m_directionLeft)
        {
            transform.Rotate(Vector3.up * Time.deltaTime * -m_rightRate);
        }
        else 
        {
            transform.Rotate(Vector3.up * Time.deltaTime * m_rightRate);
        }
        */
        // --------


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

    private void OnEnable ()
    {
        m_input.OnDown += InvertDirection;
    }

    private void OnDisable ()
    {
        m_input.OnDown -= InvertDirection;
    }    

    private void InvertDirection()
    {
        m_directionLeft = !m_directionLeft;
    }
}