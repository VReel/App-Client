using UnityEngine;
using UnityEngine.UI;               // Text
using System;                       // Exception
using System.Net;                   // HttpWebRequest
using System.IO;                    // Stream, FileStream
using System.Text;                  // Encoding
using System.Collections;           // IEnumerator
using RestSharp;                    // RestClient
using System.Linq;                  // Select

// This class acts as a wrapper to the Back-End API

public class BackEndAPI
{       
    // **************************
    // Member Variables
    // **************************

    const string m_vreelStagingURL = "https://vreel-staging.herokuapp.com/v1";
    const string m_vreelProductionURL = "https://api.vreel.io/v1";
    const string m_vreelStagingApplicationID = "366vapr5iwscaicaswycf8lvwetzmkj1r6loby9nc3uq26flimxpbqnadbt6vam3";
    const string m_vreelProductionApplicationID = "55z96ha833gxes1s1yf80bf1ckcq9ofdf6mm9n365f33bgvcf6n41usdaorya0pu";

    private string m_vreelURL = "";
    private string m_applicationID = "";

    private RestClient m_vreelClient;
    private MonoBehaviour m_owner = null;
    private GameObject m_errorMessage;
    private User m_user;
    private ThreadJob m_threadJob;

    private HttpStatusCode m_lastStatusCode;
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
        m_vreelURL = m_vreelProductionURL; //m_vreelStagingURL; m_vreelProductionURL;
        m_applicationID = m_vreelProductionApplicationID; //m_vreelStagingApplicationID; m_vreelProductionApplicationID;

        m_vreelClient = new RestClient(m_vreelURL);
        m_threadJob = new ThreadJob(owner);


        ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
    }

    ~BackEndAPI()
    {
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: A BackEndAPI object was Destructed by = " + m_owner.name);
    }

    public bool IsLastAPICallSuccessful()
    {
        return IsSuccessCode(m_lastStatusCode);
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

    public IEnumerator UploadObject(string url, string filePath)
    {
        bool success = false;
        bool isDebugBuild = Debug.isDebugBuild;
        m_threadJob.Start( () => 
            success = UploadObjectInternal(url, filePath, isDebugBuild)
        );
        yield return m_threadJob.WaitFor();
    }
        
    public IEnumerator Register_CreateUser(string _handle, string _email, string _password, string _password_confirmation)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users' - Create New User");

        var request = new RestRequest("/users", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);

        request.AddJsonBody(new { 
            handle = _handle, 
            email = _email,
            password = _password,
            password_confirmation = _password_confirmation,
            name = "",
            profile = ""
        });

        yield return m_threadJob.WaitFor();
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
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

        if (Debug.isDebugBuild) LogRequest(request, response, (timeBeforeRequest - timeAfterRequest));
    }

    public IEnumerator Register_GetUser()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/users' - Get user details");

        var request = new RestRequest("/users", Method.GET);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        yield return m_threadJob.WaitFor();
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/users' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            UpdateAccessToken(response);

            yield return m_threadJob.WaitFor();
            VReelJSON.Model_User result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_User>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_user.m_handle = result.data.attributes.handle;
            m_user.m_email = result.data.attributes.email;
            m_user.m_name = result.data.attributes.name;
            m_user.m_profileDescription = result.data.attributes.profile;
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/users'");
        }

        if (Debug.isDebugBuild) LogRequest(request, response, (timeBeforeRequest - timeAfterRequest));
    }

    public IEnumerator Register_UpdateUser(string _handle, string _password, string _password_confirmation, string _current_password)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> PUT to '/users' - Update User");
        
        var request = new RestRequest("/users", Method.PUT);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        request.AddJsonBody(new { 
            handle = _handle,
            password = _password,
            password_confirmation = _password_confirmation,
            current_password = _current_password,
            name = "",
            profile = ""
        });

        yield return m_threadJob.WaitFor();
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> PUT to '/users' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            UpdateAccessToken(response);
        }
        else // Error Handling
        {            
            ShowErrors(response, "PUT to '/users'");
        }

        if (Debug.isDebugBuild) LogRequest(request, response, (timeBeforeRequest - timeAfterRequest));
    }

    public IEnumerator Register_DeleteUser()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/users' - Delete User");

        var request = new RestRequest("/users", Method.DELETE);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        yield return m_threadJob.WaitFor();
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/users' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            UpdateAccessToken(response);
        }
        else // Error Handling
        {            
            ShowErrors(response, "DELETE to '/users'");
        }

        if (Debug.isDebugBuild) LogRequest(request, response, (timeBeforeRequest - timeAfterRequest));
    }
        
    public IEnumerator Passwords_PasswordReset(string _email)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users/password' - New Password Reset Email");

        var request = new RestRequest("/users/password", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);

        request.AddJsonBody(new { 
            email = _email
        });

        yield return m_threadJob.WaitFor();
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users/password' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            UpdateAccessToken(response);
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/users/password'");
        }

        if (Debug.isDebugBuild) LogRequest(request, response, (timeBeforeRequest - timeAfterRequest));
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
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users/sign_in' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            UpdateAccessToken(response);
            UpdateLoginTokens(response);

            yield return m_threadJob.WaitFor();
            VReelJSON.Model_User result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_User>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_user.m_handle = result.data.attributes.handle;
            m_user.m_email = result.data.attributes.email;
            m_user.m_name = result.data.attributes.name;
            m_user.m_profileDescription = result.data.attributes.profile;
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/users/sign_in'");
        }

        if (Debug.isDebugBuild) LogRequest(request, response, (timeBeforeRequest - timeAfterRequest));
    }
        
    public IEnumerator Session_SignOut()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/users/sign_out' - Sign Out");
        
        var request = new RestRequest("/users/sign_out", Method.DELETE);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        yield return m_threadJob.WaitFor();
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/users/sign_out' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            UpdateAccessToken(response);
            m_user.Clear();
        }
        else // Error Handling
        {            
            ShowErrors(response, "DELETE to '/users/sign_out'");
        }

        if (Debug.isDebugBuild) LogRequest(request, response, (timeBeforeRequest - timeAfterRequest));
    }
        
    public IEnumerator S3_PresignedURL()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/s3_presigned_url' - S3 Presigned URL");
        
        var request = new RestRequest("/s3_presigned_url", Method.GET);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        m_s3URLJSONResult = null;

        yield return m_threadJob.WaitFor();
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/s3_presigned_url' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            UpdateAccessToken(response);

            yield return m_threadJob.WaitFor();
            VReelJSON.Model_S3PresignedURL result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_S3PresignedURL>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_s3URLJSONResult = result;          
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/s3_presigned_url'");
        }

        if (Debug.isDebugBuild) LogRequest(request, response, (timeBeforeRequest - timeAfterRequest));
    }
        
    public IEnumerator Posts_GetPage(string page = "")
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts' - Get a page of posts");
                    
        var request = new RestRequest("/posts?page=" + page, Method.GET);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        m_postsJSONResult = null;

        yield return m_threadJob.WaitFor();
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            UpdateAccessToken(response);

            yield return m_threadJob.WaitFor();
            VReelJSON.Model_Posts result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Posts>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_postsJSONResult = result;
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/posts'");
        }

        if (Debug.isDebugBuild) LogRequest(request, response, (timeBeforeRequest - timeAfterRequest));
    }
        
    public IEnumerator Posts_CreatePost(string _thumbnailKey, string _originalKey)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/posts' - Create new post");
        
        var request = new RestRequest("/posts", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        request.AddJsonBody(new { 
            thumbnail_key = _thumbnailKey, 
            original_key = _originalKey,
            caption = ""
        });

        yield return m_threadJob.WaitFor();
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/posts' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            UpdateAccessToken(response);
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/posts'");
        }

        if (Debug.isDebugBuild) LogRequest(request, response, (timeBeforeRequest - timeAfterRequest));
    }

    public IEnumerator Posts_GetPost(string postId)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts/" + postId + "' - Show a post");

        var request = new RestRequest("/posts/" + postId, Method.GET);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        yield return m_threadJob.WaitFor();
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts/" + postId + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            UpdateAccessToken(response);

            yield return m_threadJob.WaitFor();
            VReelJSON.Model_Post result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Post>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_postJSONResult = result;
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/posts/" + postId + "'");
        }

        if (Debug.isDebugBuild) LogRequest(request, response, (timeBeforeRequest - timeAfterRequest));
    }

    public IEnumerator Posts_DeletePost(string postId)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/posts/" + postId + "' - Delete a post");

        var request = new RestRequest("/posts/" + postId, Method.DELETE);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        yield return m_threadJob.WaitFor();
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/posts/" + postId + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            UpdateAccessToken(response);
        }
        else // Error Handling
        {            
            ShowErrors(response, "DELETE to '/posts/" + postId + "'");
        }

        if (Debug.isDebugBuild) LogRequest(request, response, (timeBeforeRequest - timeAfterRequest));
    }

    // **************************
    // Private/Helper functions
    // **************************

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

    private bool UploadObjectInternal(string url, string filePath, bool isDebugBuild)
    {
        if (isDebugBuild) Debug.Log("------- VREEL: Uploading Object with url: " + url ); //+ ", and filePath: " + filePath);

        HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
        httpRequest.Method = "PUT";

        try
        {
            byte[] byteArray = File.ReadAllBytes(filePath);
            httpRequest.ContentLength = byteArray.Length;
            Stream dataStream = httpRequest.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
        }
        catch (Exception e)
        {
            if (isDebugBuild) Debug.Log("------- VREEL: Upload Exception caught: " + e);
            m_lastStatusCode = HttpStatusCode.NotFound;
        }

        if (isDebugBuild) Debug.Log("------- VREEL: Finished Uploading FileStream...");
        try
        {
            HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse;
            m_lastStatusCode = response.StatusCode;
            if (isDebugBuild) Debug.Log("------- VREEL: Response Status Code: " + response.StatusCode);
        }
        catch (Exception e)
        {
            if (isDebugBuild) Debug.Log("------- VREEL: Response Exception caught: " + e);
            m_lastStatusCode = HttpStatusCode.NotFound;
        }

        return true;
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
                m_user.SetAcceessToken(parameter.Value.ToString());
            }               
        }
    }

    private void UpdateLoginTokens(IRestResponse response)
    {
        foreach (Parameter parameter in response.Headers)
        {
            if (parameter.Name == "Client")
            {
                m_user.SetClient(parameter.Value.ToString());
            }

            if (parameter.Name == "Uid")
            {
                m_user.SetUID(parameter.Value.ToString());
            }
        }
    }

    private void ShowErrors(IRestResponse response, string debugString)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> " + debugString + " - Error Code: " + response.StatusCode + " - Content:" + response.Content);

        VReelJSON.Model_Error result = null;
        try
        {
            result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Error>(response.Content);
        }
        catch (Exception e)
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - Failed to serialize the Error! Exception = " + e);
        }

        var errorText = m_errorMessage.GetComponentInChildren<Text>();
        errorText.text = "";

        if (result != null && result.errors != null)
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

    private void LogRequest(IRestRequest request, IRestResponse response, float durationMs)
    {
        var requestToLog = new
        {
            resource = request.Resource,
            // Parameters are custom anonymous objects in order to have the parameter type as a nice string
            // otherwise it will just show the enum value
            parameters = request.Parameters.Select(parameter => new
            {
                name = parameter.Name,
                value = parameter.Value,
                type = parameter.Type.ToString()
            }),
            // ToString() here to have the method as a nice string otherwise it will just show the enum value
            method = request.Method.ToString(),
            // This will generate the actual Uri used in the request
            uri = m_vreelClient.BuildUri(request),
        };

        var responseToLog = new
        {
            statusCode = response.StatusCode,
            content = response.Content,
            headers = response.Headers,
            // The Uri that actually responded (could be different from the requestUri if a redirection occurred)
            responseUri = response.ResponseUri,
            errorMessage = response.ErrorMessage,
        };

        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Request completed in {0} ms, Request: {1}, Response: {2}",
            durationMs, 
            RestSharp.SimpleJson.SerializeObject(requestToLog),
            RestSharp.SimpleJson.SerializeObject(responseToLog)));
    }
}