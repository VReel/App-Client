using UnityEngine;
using UnityEngine.UI;               // Text
using System;                       // Exception
using System.Net;                   // HttpWebRequest
using System.IO;                    // Stream, FileStream
using System.Text;                  // Encoding
using System.Collections;           // IEnumerator
using RestSharp;                    // RestClient

// This class acts simply as an API into the Back-End

public class BackEndAPI
{       
    // **************************
    // Member Variables
    // **************************

    const string m_vreelStagingURL = "https://vreel-staging.herokuapp.com/v1";
    const string m_vreelProductionURL = "https://api.vreel.io/v1";
    const string m_vreelStagingApplicationID = "366vapr5iwscaicaswycf8lvwetzmkj1r6loby9nc3uq26flimxpbqnadbt6vam3";
    const string m_vreelProductionapplicationID = "ic4ycp0w6jagoi67liubw5dug6zszbq9gcvhfayu25ulc3vj5xp02sf99ingc0s4";

    private string m_vreelURL = "";
    private string m_applicationID = "";

    private RestClient m_vreelClient;
    private MonoBehaviour m_owner = null;
    private GameObject m_errorMessage;
    private User m_user;
    private ThreadJob m_threadJob;

    // **************************
    // Public functions
    // **************************

    public BackEndAPI(MonoBehaviour owner, GameObject errorMessage, User user)
    {
        m_owner = owner;
        m_errorMessage = errorMessage;
        m_user = user;
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: A BackEndAPI object was Created by = " + m_owner.name);

        // Version dependent code
        m_vreelURL = m_vreelStagingURL; //m_vreelProductionURL;
        m_applicationID = m_vreelStagingApplicationID; //m_vreelProductionapplicationID;

        m_vreelClient = new RestClient(m_vreelURL);
        m_threadJob = new ThreadJob(owner);
    }

    ~BackEndAPI()
    {
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: A BackEndAPI object was Destructed by = " + m_owner.name);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users' - Response: " + response.Content);

        if (IsSuccessCode(response.StatusCode))
        {
            UpdateAccessToken(response);
            UpdateLoginTokens(response);

            m_user.m_handle = handle;
            m_user.m_email = email;
            m_user.m_name = "";
            m_user.m_profileDescription = "";
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/users/'");
        }
    }

    // ------ !CURRENTLY UNUSED! -------- //
    public IEnumerator Register_UpdateUser()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> PUT to '/users' - Update User");
        
        var request = new RestRequest("/users", Method.PUT);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.m_client);
        request.AddHeader("uid", m_user.m_uid);
        request.AddHeader("access-token", m_user.m_accessToken);

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> PUT to '/users' - Response: " + response.Content);

        if (IsSuccessCode(response.StatusCode))
        {
            UpdateAccessToken(response);
        }
        else // Error Handling
        {            
            ShowErrors(response, "PUT to '/users'");
        }
    }

    // ------ !CURRENTLY UNUSED! -------- //
    public IEnumerator Register_DeleteUser()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/users' - Delete User");

        var request = new RestRequest("/users", Method.DELETE);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.m_client);
        request.AddHeader("uid", m_user.m_uid);
        request.AddHeader("access-token", m_user.m_accessToken);

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/users' - Response: " + response.Content);

        if (IsSuccessCode(response.StatusCode))
        {
            UpdateAccessToken(response);
            m_user.Clear();
        }
        else // Error Handling
        {            
            ShowErrors(response, "DELETE to '/users'");
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users/password' - Response: " + response.Content);

        if (IsSuccessCode(response.StatusCode))
        {
            UpdateAccessToken(response);
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/users/password'");
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users/sign_in' - Response: " + response.Content);

        if (IsSuccessCode(response.StatusCode))
        {
            UpdateAccessToken(response);
            UpdateLoginTokens(response);

            var result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_SignIn>(response.Content);

            m_user.m_handle = result.data.attributes.handle;
            m_user.m_email = result.data.attributes.email;
            m_user.m_name = result.data.attributes.name;
            m_user.m_profileDescription = result.data.attributes.profile;
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/users/sign_in'");
        }
    }
        
    public IEnumerator Session_SignOut()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/users/sign_out' - Sign Out");
        
        var request = new RestRequest("/users/sign_out", Method.DELETE);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.m_client);
        request.AddHeader("uid", m_user.m_uid);
        request.AddHeader("access-token", m_user.m_accessToken);

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/users/sign_out' - Response: " + response.Content);

        if (IsSuccessCode(response.StatusCode))
        {
            UpdateAccessToken(response);
            m_user.Clear();
        }
        else // Error Handling
        {            
            ShowErrors(response, "DELETE to '/users/sign_out'");
        }
    }

    // ------ !CURRENTLY UNUSED! -------- //
    public IEnumerator S3_PresignedURL()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/s3_presigned_url' - S3 Presigned URL");
        
        var request = new RestRequest("/s3_presigned_url", Method.GET);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.m_client);
        request.AddHeader("uid", m_user.m_uid);
        request.AddHeader("access-token", m_user.m_accessToken);

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/s3_presigned_url' - Response: " + response.Content);

        if (IsSuccessCode(response.StatusCode))
        {
            UpdateAccessToken(response);

            var result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_S3PresignedURL>(response.Content);

            Debug.Log("Original - Key: " + result.data.attributes.original.key.ToString());
            Debug.Log("Original - URL: " + result.data.attributes.original.url.ToString());
            Debug.Log("Thumbnail - Key: " + result.data.attributes.thumbnail.key.ToString());
            Debug.Log("Thumbnail - URL: " + result.data.attributes.thumbnail.url.ToString());

            /*
            UploadObject(
                response.Data.attributes.original.url.ToString(),
                System.IO.Directory.GetCurrentDirectory() + "/Assets/Berlin_Original.jpg"
            );
            */
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/s3_presigned_url'");
        }
    }
        
    public IEnumerator Posts_GetAll()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts' - Get all posts");

        var request = new RestRequest("/posts", Method.GET);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.m_client);
        request.AddHeader("uid", m_user.m_uid);
        request.AddHeader("access-token", m_user.m_accessToken);

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts' - Response: " + response.Content);

        if (IsSuccessCode(response.StatusCode))
        {
            UpdateAccessToken(response);

            var result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Profile>(response.Content);

            Debug.Log("Test: " + result.GetType());
            //Debug.Log("Test2: " + result.data.GetType());

            //Debug.Log("Thumbnail URL: " + result.data.attributes.thumbnail_url.ToString());
            //Debug.Log("Caption: " + result.data.attributes.caption.ToString());
            //Debug.Log("Created At: " + result.data.attributes.created_at.ToString());
            //Debug.Log("Edited: " + result.data.attributes.edited.ToString());
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/posts'");
        }
    }

    // ------ !CURRENTLY UNUSED! -------- //
    public IEnumerator Posts_Create(string thumbnailKey, string originalKey)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/posts' - Create new user");
        
        var request = new RestRequest("/posts", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.m_client);
        request.AddHeader("uid", m_user.m_uid);
        request.AddHeader("access-token", m_user.m_accessToken);

        request.AddParameter("thumbnail_key", thumbnailKey);
        request.AddParameter("original_key", originalKey);
        request.AddParameter("caption", "");

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/posts' - Response: " + response.Content);

        if (IsSuccessCode(response.StatusCode))
        {
            UpdateAccessToken(response);
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/posts'");
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

    private bool IsSuccessCode(HttpStatusCode statusCode)
    {
        return ((int)statusCode >= 200 && (int)statusCode < 300);
    }

    private void UpdateAccessToken(IRestResponse response)
    {
        foreach (Parameter parameter in response.Headers)
        {
            if (parameter.Name == "Access-Token")
            {
                m_user.m_accessToken = parameter.Value.ToString();
            }               
        }
    }

    private void UpdateLoginTokens(IRestResponse response)
    {
        foreach (Parameter parameter in response.Headers)
        {
            if (parameter.Name == "Client")
            {
                m_user.m_client = parameter.Value.ToString();
            }

            if (parameter.Name == "Uid")
            {
                m_user.m_uid = parameter.Value.ToString();
            }
        }
    }

    private void ShowErrors(IRestResponse response, string debugString)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> " + debugString + " - Error Code: " + response.StatusCode);

        var result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Error>(response.Content);

        var errorText = m_errorMessage.GetComponentInChildren<Text>();
        errorText.text = "";

        foreach(var error in result.errors)
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> " + debugString + " - Error: " + error.detail.ToString());

            errorText.text += error.detail.ToString();
            errorText.text += "\n";
            m_errorMessage.SetActive(true);
        }
    }        
}