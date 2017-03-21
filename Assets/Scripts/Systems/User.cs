using UnityEngine;

// This class holds a blackboard of user variables that are updated depending on the user logged in

public class User : MonoBehaviour
{
    // **************************
    // Member Variables
    // **************************

    public string m_client {get; set;}
    public string m_uid {get; set;}
    public string m_accessToken {get; set;}

    public string m_handle {get; set;}
    public string m_email {get; set;}
    public string m_name {get; set;}
    public string m_profileDescription {get; set;}

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        Clear();
    }

    public bool IsLoggedIn()
    {
        return (m_client.Length + m_uid.Length > 0);
    }

    public void Clear()
    {
        m_client = m_uid = "";
        m_handle = m_email = m_name = m_profileDescription = "";
    }
}