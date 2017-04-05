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

    const string m_vreelStagingSaveFile = "vreelStagingSave.dat";
    const string m_vreelProductionSaveFile = "vreelProductionSave.dat";
    private string m_vreelSaveFile = "";

    private string m_dataFilePath;
    private LoginData m_loginData;

    private BackEndAPI m_backEndAPI;
    private CoroutineQueue m_coroutineQueue;
    private ThreadJob m_threadJob;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        // Version dependent code
        m_vreelSaveFile = m_vreelProductionSaveFile; //m_vreelStagingSaveFile; m_vreelProductionSaveFile;
        m_dataFilePath = Application.persistentDataPath + m_vreelSaveFile;

        m_loginData = new LoginData();
        m_loginData.m_client = m_loginData.m_uid = m_loginData.m_accessToken = "";
        m_handle = m_email = m_name = m_profileDescription = "";

        m_backEndAPI = new BackEndAPI(this, m_errorMessage, this);

        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        m_threadJob = new ThreadJob(this);

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
        m_coroutineQueue.EnqueueAction(SaveLoginData());
    }

    public string GetUID()
    {
        return m_loginData.m_uid;
    }

    public void SetUID(string uid)
    {
        m_loginData.m_uid = uid;
        m_coroutineQueue.EnqueueAction(SaveLoginData());
    }

    public string GetAcceessToken()
    {
        return m_loginData.m_accessToken;
    }

    public void SetAcceessToken(string acceessToken)
    {
        m_loginData.m_accessToken = acceessToken;
        m_coroutineQueue.EnqueueAction(SaveLoginData());
    }

    // **************************
    // Private/Helper functions
    // **************************

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

    private IEnumerator SaveLoginData()
    {
        yield return m_threadJob.WaitFor();
        bool result = false;
        m_threadJob.Start( () => 
            result = SaveLoginDataToFile()
        );
        yield return m_threadJob.WaitFor(); 
    }

    private bool SaveLoginDataToFile()
    {
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        using (FileStream fileStream = File.Create(m_dataFilePath)) // We call Create() to ensure we always overwrite the file
        {
            binaryFormatter.Serialize(fileStream, m_loginData);
        }

        return true;
    }
}