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

    private VReelJSON.Model_Posts m_postsJSONResult;
    private VReelJSON.Model_Post m_postJSONResult;
    private VReelJSON.Model_S3PresignedURL m_s3URLJSONResult;

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

    public VReelJSON.Model_Posts GetAllPostsResult()
    {
        return m_postsJSONResult;
    }

    public VReelJSON.Model_Post GetPostResult()
    {
        return m_postJSONResult;
    }

    public VReelJSON.Model_S3PresignedURL GetS3PresignedURLResult()
    {
        return m_s3URLJSONResult;
    }

    public IEnumerator UploadObject(string url, byte[] byteArray) // string filePath)
    {
        UploadObjectInternal(url, byteArray);
        yield break;
    }
        
    public IEnumerator Register_CreateUser(string _handle, string _email, string _password, string _passwordConfirmation)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users' - Create New User");

        var request = new RestRequest("/users", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);

        request.AddJsonBody(new { 
            handle = _handle, 
            email = _email,
            password = _password,
            password_confirmation = _passwordConfirmation,
            name = "",
            profile = ""
        });

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

            m_user.m_handle = _handle;
            m_user.m_email = _email;
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
    public IEnumerator Passwords_Password(string _email)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users/password' - New Password Reset");

        var request = new RestRequest("/users/password", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);

        request.AddJsonBody(new { 
            email = _email
        });

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
        
    public IEnumerator Session_SignIn(string _login, string _password)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users/sign_in' - Sign In");

        var request = new RestRequest("/users/sign_in", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);

        request.AddJsonBody(new { 
            login = _login, 
            password = _password,
        });

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
        
    public IEnumerator S3_PresignedURL()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/s3_presigned_url' - S3 Presigned URL");
        
        var request = new RestRequest("/s3_presigned_url", Method.GET);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.m_client);
        request.AddHeader("uid", m_user.m_uid);
        request.AddHeader("access-token", m_user.m_accessToken);

        m_s3URLJSONResult = null;

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
            m_s3URLJSONResult = result;          
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

        m_postsJSONResult = null;

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

            var result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Posts>(response.Content);
            m_postsJSONResult = result;
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/posts'");
        }
    }
        
    public IEnumerator Posts_Create(string _thumbnailKey, string _originalKey)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/posts' - Create new post");
        
        var request = new RestRequest("/posts", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.m_client);
        request.AddHeader("uid", m_user.m_uid);
        request.AddHeader("access-token", m_user.m_accessToken);

        request.AddJsonBody(new { 
            thumbnail_key = _thumbnailKey, 
            original_key = _originalKey,
            caption = ""
        });

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

    public IEnumerator Posts_Get(string _postId)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts/" + _postId + "' - Show a post");

        var request = new RestRequest("/posts/" + _postId, Method.GET);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.m_client);
        request.AddHeader("uid", m_user.m_uid);
        request.AddHeader("access-token", m_user.m_accessToken);

        /*
        request.AddJsonBody(new { 
            postId = _postId
        });
        */

        yield return m_threadJob.WaitFor();
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts/" + _postId + "' - Response: " + response.Content);

        if (IsSuccessCode(response.StatusCode))
        {
            UpdateAccessToken(response);

            var result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Post>(response.Content);
            m_postJSONResult = result;
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/posts/" + _postId + "'");
        }
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void UploadObjectInternal(string url, byte[] byteArray) //string filePath)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Uploading Object called with url: " + url ); //+ ", and filePath: " + filePath);

        HttpWebRequest httpRequest = WebRequest.Create(url) as HttpWebRequest;
        httpRequest.Method = "PUT";

        try
        {
            //byte[] byteArray = File.ReadAllBytes(filePath);
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

        Debug.Log("------- VREEL: Finished Uploading FileStream...");
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

        if (result.errors != null)
        {
            foreach(var error in result.errors)
            {
                if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> " + debugString + " - Error: " + error.detail.ToString());

                errorText.text += error.detail.ToString();
                errorText.text += "\n";
                m_errorMessage.SetActive(true);
            }
        }
    }        
}