using UnityEngine;
using System;                                           //Serializable
using System.Runtime.Serialization.Formatters.Binary;   //BinaryFormatter
using System.IO;                                        //Filestream, File
using System.Collections;                               // IEnumerator

// This class holds a blackboard of user variables that are updated depending on the user logged in
public class User : MonoBehaviour
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private GameObject m_errorMessage;

    [Serializable]
    public class LoginData
    {
        public string m_client {get; set;}
        public string m_uid {get; set;}
        public string m_accessToken {get; set;}
    }
        
    public string m_handle {get; set;}
    public string m_email {get; set;}
    public string m_name {get; set;}
    public string m_profileDescription {get; set;}

    private string m_dataFilePath;
    private LoginData m_loginData;

    private BackEndAPI m_backEndAPI;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        m_dataFilePath = Application.persistentDataPath + "vreelLogin.dat";

        m_loginData = new LoginData();
        m_loginData.m_client = m_loginData.m_uid = m_loginData.m_accessToken = "";
        m_handle = m_email = m_name = m_profileDescription = "";

        m_backEndAPI = new BackEndAPI(this, m_errorMessage, this);

        LoadLoginData();
    }

    public bool IsLoggedIn()
    {
        return (m_loginData.m_client.Length + m_loginData.m_uid.Length > 0);
    }

    public bool IsUserDataStored()
    {
        return (m_handle.Length > 0);
    }

    public void Clear()
    {
        m_loginData.m_client = m_loginData.m_uid = "";
        m_handle = m_email = m_name = m_profileDescription = "";

        if (File.Exists(m_dataFilePath))
        {
            File.Delete(m_dataFilePath);
        }
    }

    public string GetClient()
    {
        return m_loginData.m_client;
    }

    public void SetClient(string client)
    {
        m_loginData.m_client = client;
        SaveLoginData();
    }

    public string GetUID()
    {
        return m_loginData.m_uid;
    }

    public void SetUID(string uid)
    {
        m_loginData.m_uid = uid;
        SaveLoginData();
    }

    public string GetAcceessToken()
    {
        return m_loginData.m_accessToken;
    }

    public void SetAcceessToken(string acceessToken)
    {
        m_loginData.m_accessToken = acceessToken;
        SaveLoginData();
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void SaveLoginData()
    {
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        using (FileStream fileStream = File.Create(m_dataFilePath)) // We call Create() to ensure we always overwrite the file
        {
            binaryFormatter.Serialize(fileStream, m_loginData);
        }
    }

    private void LoadLoginData()
    {
        if (File.Exists(m_dataFilePath))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (FileStream fileStream = File.Open(m_dataFilePath, FileMode.Open))
            {
                m_loginData = (LoginData) binaryFormatter.Deserialize(fileStream);
            }

            StartCoroutine(m_backEndAPI.Register_GetUser());
        }
    }
}