using UnityEngine;
using System;               // Exception
using System.Net;           // HttpWebRequest
using System.IO;            // Stream, FileStream
using System.Text;          // Encoding
using System.Collections;   // IEnumerator
using RestSharp;            // RestClient

public class BackEndAPI
{
    // **************************
    // JSON Deserialisation
    // **************************

    public class S3PresignedURL
    {
        public S3PresignedURLData data { get; set; }
    }

    public class S3PresignedURLData
    {
        public string type { get; set; }
        public Attributes attributes { get; set; }
    }

    public class Attributes
    {
        public KeyAndURL original { get; set; }
        public KeyAndURL thumbnail { get; set; }
    }

    public class KeyAndURL
    {
        public string key { get; set; }
        public string url { get; set; }
    }

    // **************************
    // Member Variables
    // **************************

    const string m_vreelStagingApplicationID = "366vapr5iwscaicaswycf8lvwetzmkj1r6loby9nc3uq26flimxpbqnadbt6vam3";
    const string m_vreelProductionapplicationID = "ic4ycp0w6jagoi67liubw5dug6zszbq9gcvhfayu25ulc3vj5xp02sf99ingc0s4";
    const string m_vreelStagingURL = "https://vreel-staging.herokuapp.com/v1";
    const string m_vreelProductionURL = "https://api.vreel.io/v1";

    private RestClient m_vreelClient;
    private string m_applicationID = "";
    private string m_client = "";
    private string m_uid = "";
    private string m_accessToken = "";

    private MonoBehaviour m_owner = null;
    private ThreadJob m_threadJob;

    // **************************
    // Public functions
    // **************************

    public BackEndAPI(MonoBehaviour owner)
    {
        m_owner = owner;
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: An BackEndAPI object was Created by = " + m_owner.name);

        m_applicationID = m_vreelStagingApplicationID;
        m_vreelClient = new RestClient(m_vreelStagingURL);
        m_threadJob = new ThreadJob(owner);
    }

    ~BackEndAPI()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: A BackEndAPI object was Destructed by = " + m_owner.name);
    }
        
    public IEnumerator Register_CreateUser(string handle, string email, string password, string passwordConfirmation)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users' - Create New User");

        var request = new RestRequest("/users", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);

        request.AddParameter("handle", handle);
        request.AddParameter("email", email);
        request.AddParameter("password", password);
        request.AddParameter("password_confirmation", passwordConfirmation);
        request.AddParameter("name", "");
        request.AddParameter("profile", "");

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (Debug.isDebugBuild) Debug.Log(response.Content);

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            UpdateAccessToken(response);
        }
        else 
        {
            // TODO: Error handling...
        }
    }

    // ------ !CURRENTLY UNUSED! -------- //
    public IEnumerator Register_UpdateUser()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> PUT to '/users' - Update User");
        
        var request = new RestRequest("/users", Method.PUT);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_client);
        request.AddHeader("uid", m_uid);
        request.AddHeader("access-token", m_accessToken);

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            UpdateAccessToken(response);
        }
        else 
        {
            // TODO: Error handling...
        }
    }

    // ------ !CURRENTLY UNUSED! -------- //
    public IEnumerator Register_DeleteUser()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/users' - Delete User");

        var request = new RestRequest("/users", Method.DELETE);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_client);
        request.AddHeader("uid", m_uid);
        request.AddHeader("access-token", m_accessToken);

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            UpdateAccessToken(response);
        }
        else 
        {
            // TODO: Error handling...
        }
    }

    // ------ !CURRENTLY UNUSED! -------- //
    public IEnumerator Passwords_Password(string email)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users/password' - New Password Reset");

        var request = new RestRequest("/users/password", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);

        request.AddParameter("email", email);

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            UpdateAccessToken(response);
        }
        else 
        {
            // TODO: Error handling...
        }
    }
        
    public IEnumerator Session_SignIn(string login, string password)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users/sign_in' - Sign In");
        
        var request = new RestRequest("/users/sign_in", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);

        request.AddParameter("login", login);
        request.AddParameter("password", password);

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (Debug.isDebugBuild) Debug.Log(response.Content);

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            UpdateAccessToken(response);

            foreach (Parameter parameter in response.Headers)
            {
                if (parameter.Name == "Client")
                {
                    m_client = parameter.Value.ToString();
                }

                if (parameter.Name == "Uid")
                {
                    m_uid = parameter.Value.ToString();
                }
            }
        }
        else
        {
            // TODO: Error handling...
        }
    }

    // ------ !CURRENTLY UNUSED! -------- //
    public IEnumerator Session_SignOut()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/users/sign_out' - Sign Out");
        
        var request = new RestRequest("/users/sign_out", Method.DELETE);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_client);
        request.AddHeader("uid", m_uid);
        request.AddHeader("access-token", m_accessToken);

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            UpdateAccessToken(response);
        }
        else 
        {
            // TODO: Error handling...
        }
    }

    // ------ !CURRENTLY UNUSED! -------- //
    public IEnumerator S3_PresignedURL()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/s3_presigned_url' - S3 Presigned URL");
        
        var request = new RestRequest("/s3_presigned_url", Method.GET);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_client);
        request.AddHeader("uid", m_uid);
        request.AddHeader("access-token", m_accessToken);

        yield return m_threadJob.WaitFor();
        IRestResponse<S3PresignedURL> response = new RestResponse<S3PresignedURL>();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute<S3PresignedURL>(request)
        );
        yield return m_threadJob.WaitFor();

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            UpdateAccessToken(response);

            Debug.Log("Original - Key: " + response.Data.data.attributes.original.key.ToString());
            Debug.Log("Original - URL: " + response.Data.data.attributes.original.url.ToString());
            Debug.Log("Thumbnail - Key: " + response.Data.data.attributes.thumbnail.key.ToString());
            Debug.Log("Thumbnail - URL: " + response.Data.data.attributes.thumbnail.url.ToString());

            /*
            UploadObject(
                response.Data.attributes.original.url.ToString(),
                System.IO.Directory.GetCurrentDirectory() + "/Assets/Berlin_Original.jpg"
            );
            */
        }
        else 
        {
            // TODO: Error handling...
        }
    }

    // ------ !CURRENTLY UNUSED! -------- //
    public IEnumerator Posts_GetAll()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts' - Get all posts");

        var request = new RestRequest("/posts", Method.GET);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_client);
        request.AddHeader("uid", m_uid);
        request.AddHeader("access-token", m_accessToken);

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            UpdateAccessToken(response);
        }
        else 
        {
            // TODO: Error handling...
        }
    }

    // ------ !CURRENTLY UNUSED! -------- //
    public IEnumerator Posts_Create(string thumbnailKey, string originalKey)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/posts' - Create new user");
        
        var request = new RestRequest("/posts", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_client);
        request.AddHeader("uid", m_uid);
        request.AddHeader("access-token", m_accessToken);

        request.AddParameter("thumbnail_key", thumbnailKey);
        request.AddParameter("original_key", originalKey);
        request.AddParameter("caption", "");

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            UpdateAccessToken(response);
        }
        else 
        {
            // TODO: Error handling...
        }
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void UploadObject(string url, string filePath)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Uploading Object called with url: " + url + ", and filePath: " + filePath);

        HttpWebRequest httpRequest = WebRequest.Create(url) as HttpWebRequest;
        httpRequest.Method = "PUT";

        try
        {
            byte[] byteArray = File.ReadAllBytes(filePath);
            httpRequest.ContentLength = byteArray.Length;
            Stream dataStream = httpRequest.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            /*
            using (Stream dataStream = httpRequest.GetRequestStream())
            {                
                byte[] buffer = new byte[8000];
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {         
                    int totalBytesRead = 0;
                    int bytesRead = 0;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        dataStream.Write(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                    }

                    Debug.Log("Total Bytes Read = " + totalBytesRead);
                }
            }
            */
        }
        catch (Exception e)
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Upload Exception caught: " + e);
        }

        Debug.Log("Finished Uploading FileStream...");
        try
        {
            HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse;
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Response Status Code: " + response.StatusCode);
        }
        catch (Exception e)
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: Response Exception caught: " + e);
        }
    }

    private void UpdateAccessToken(IRestResponse response)
    {
        foreach (Parameter parameter in response.Headers)
        {
            if (parameter.Name == "Access-Token")
            {
                m_accessToken = parameter.Value.ToString();
            }               
        }
    }
}