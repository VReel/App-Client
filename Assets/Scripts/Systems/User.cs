using UnityEngine;
using System;                                           //Serializable
using System.Runtime.Serialization.Formatters.Binary;   //BinaryFormatter
using System.IO;                                        //Filestream, File
using System.Collections;                               //IEnumerator

// This class holds a blackboard of user variables that are updated depending on the user logged in
public class User : MonoBehaviour
{
    // **************************
    // Member Variables
    // **************************

    public enum BackEndEnvironment
    {
        kDevelopment,
        kStaging,
        kProduction
    };
        
    [SerializeField] private BackEndEnvironment m_backEndEnvironment;
    [SerializeField] private PushNotifications m_pushNotifications;
    [SerializeField] private GameObject m_errorMessage;

    [Serializable]
    public class LoginData
    {
        public string m_client {get; set;}
        public string m_uid {get; set;}
        public string m_accessToken {get; set;}
    }

    public string m_id {get; set;}
    public string m_handle {get; set;}
    public string m_email {get; set;}
    public string m_name {get; set;}
    public string m_profileDescription {get; set;}

    const string m_vreelDevelopmentSaveFile = "vreelDevelopmentSave.dat";
    const string m_vreelStagingSaveFile = "vreelStagingSave.dat";
    const string m_vreelProductionSaveFile = "vreelProductionSave.dat";
    private string m_vreelSaveFile = "";

    private string m_dataFilePath;
    private bool m_loadingLoginData; //This is necessary because we don't want to start the app properly until we've tried to log you in!
    private LoginData m_loginData;

    private BackEndAPI m_backEndAPI;
    private CoroutineQueue m_coroutineQueue;
    private ThreadJob m_threadJob;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);

        // Version dependent code
        m_vreelSaveFile = GetSaveFile();
        m_dataFilePath = Application.persistentDataPath + m_vreelSaveFile;

        m_loginData = new LoginData();
        m_loginData.m_client = m_loginData.m_uid = m_loginData.m_accessToken = "";
        m_id = m_handle = m_email = m_name = m_profileDescription = "";

        m_backEndAPI = new BackEndAPI(this, m_errorMessage, this);

        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        m_threadJob = new ThreadJob(this);

        m_loadingLoginData = true;
        m_coroutineQueue.EnqueueAction(LoadLoginData());
    }

    public BackEndEnvironment GetBackEndEnvironment()
    {
        return m_backEndEnvironment;
    }

    public bool IsLoadingLoginData()
    {
        return m_loadingLoginData;
    }

    public bool IsLoggedIn()
    {
        return (m_loginData.m_client.Length > 0 && m_loginData.m_uid.Length > 0 && m_id.Length > 0);
    }

    public bool IsCurrentUser(string userId)
    {
        return m_id.CompareTo(userId) == 0;
    }

    public void Clear()
    {
        m_loginData.m_client = m_loginData.m_uid = m_loginData.m_accessToken = "";
        m_id = m_handle = m_email = m_name = m_profileDescription = "";

        if (File.Exists(m_dataFilePath))
        {
            File.Delete(m_dataFilePath);
        }
    }

    public string GetPushNotificationUserID()
    {
        return m_pushNotifications.m_oneSignalPlayerID;
    }        

    public GameObject GetErrorMessage()
    {
        return m_errorMessage;
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

    public void SetAccessToken(string accessToken)
    {
        m_loginData.m_accessToken = accessToken;
        m_coroutineQueue.EnqueueAction(SaveLoginData());
    }

    // **************************
    // Private/Helper functions
    // **************************

    private string GetSaveFile()
    {
        if (m_backEndEnvironment == BackEndEnvironment.kProduction)
        {
            return m_vreelProductionSaveFile;
        }
        else if (m_backEndEnvironment == BackEndEnvironment.kStaging)
        {
            return m_vreelStagingSaveFile;
        }
        else
        {
            return m_vreelDevelopmentSaveFile;
        }
    }

    private IEnumerator LoadLoginData()
    {
        if (File.Exists(m_dataFilePath))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (FileStream fileStream = File.Open(m_dataFilePath, FileMode.Open))
            {
                if (fileStream.Length > 0) // could have been corrupted
                {
                    m_loginData = (LoginData) binaryFormatter.Deserialize(fileStream);

                    yield return m_backEndAPI.Register_GetUser();
                }
            }
        }

        m_loadingLoginData = false;
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