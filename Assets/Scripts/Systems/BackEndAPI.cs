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

    const string m_vreelDevelopmentURL  = "https://vreel-development.herokuapp.com/v1";
    const string m_vreelStagingURL      = "https://vreel-staging.herokuapp.com/v1";
    const string m_vreelProductionURL   = "https://api.vreel.io/v1";
    const string m_vreelDevelopmentApplicationID    = "6ziuhyy8teraucg8vjq5zigx45mqd1qzexmezewy1wuq44kk7eyitjkjz8qj9rcs";
    const string m_vreelStagingApplicationID        = "hn6w6n7me8rkqeby3evdxx8e732gbaypdqh8yeg462vw2dys7du9e2n13vuodkmr";
    const string m_vreelProductionApplicationID     = "edsewyw1a5x59epomtebi7pl6j0qfdn14jsavr5pyktbl7tpacol1irggdqtdib6";

    private string m_vreelURL = "";
    private string m_applicationID = "";

    private RestClient m_vreelClient;
    private MonoBehaviour m_owner = null;
    private GameObject m_errorMessage;
    private User m_user;
    private ThreadJob m_threadJob;

    private HttpStatusCode m_lastStatusCode;
    private VReelJSON.Model_Tag m_tagJSONResult;
    private VReelJSON.Model_Tags m_tagsJSONResult;
    private VReelJSON.Model_User m_userJSONResult;
    private VReelJSON.Model_Users m_usersJSONResult;
    private VReelJSON.Model_Post m_postJSONResult;
    private VReelJSON.Model_Posts m_postsJSONResult;
    private VReelJSON.Model_Comment m_commentJSONResult;
    private VReelJSON.Model_Comments m_commentsJSONResult;
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
        m_vreelURL = GetBackEndURL();
        m_applicationID = GetApplicationID();

        m_vreelClient = new RestClient(m_vreelURL);
        m_threadJob = new ThreadJob(owner);
    }

    ~BackEndAPI()
    {
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: A BackEndAPI object was Destructed by = " + m_owner.name);
    }

    public bool IsLastAPICallSuccessful()
    {
        return IsSuccessCode(m_lastStatusCode);
    }

    public VReelJSON.Model_Tag GetTagResult()
    {
        return m_tagJSONResult;
    }

    public VReelJSON.Model_Tags GetTagsResult()
    {
        return m_tagsJSONResult;
    }  

    public VReelJSON.Model_User GetUserResult()
    {
        return m_userJSONResult;
    }

    public VReelJSON.Model_Users GetUsersResult()
    {
        return m_usersJSONResult;
    }  

    public VReelJSON.Model_Post GetPostResult()
    {
        return m_postJSONResult;
    }

    public VReelJSON.Model_Posts GetPostsResult()
    {
        return m_postsJSONResult;
    }

    public VReelJSON.Model_Comment GetCommentResult()
    {
        return m_commentJSONResult;
    }

    public VReelJSON.Model_Comments GetCommentsResult()
    {
        return m_commentsJSONResult;
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

    //--------------------------------------------
    // Register

    public IEnumerator Register_CreateUser(string _handle, string _email, string _password, string _password_confirmation, string _player_id)
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
            profile = _handle + " is Viewing Reality from a new perspective =)",
            player_id = _player_id
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
            yield return m_threadJob.WaitFor();
            VReelJSON.Model_User result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_User>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            UpdateAccessToken(response);
            UpdateLoginTokens(response);

            m_user.m_id = result.data.id;
            m_user.m_handle = result.data.attributes.handle;
            m_user.m_email = result.data.attributes.email;
            m_user.m_name = result.data.attributes.name;
            m_user.m_profileDescription = result.data.attributes.profile;
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/users/'");
        }

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator Register_GetUser()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/users' - Get my user details");

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
            yield return m_threadJob.WaitFor();
            VReelJSON.Model_User result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_User>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            UpdateAccessToken(response);
            UpdateLoginTokens(response);

            m_user.m_id = result.data.id;
            m_user.m_handle = result.data.attributes.handle;
            m_user.m_email = result.data.attributes.email;
            m_user.m_name = result.data.attributes.name;
            m_user.m_profileDescription = result.data.attributes.profile;
        }
        else // Error Handling
        {            
            m_user.Clear();

            ShowErrors(response, "GET to '/users'");
        }

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator Register_UpdatePassword(string _password, string _password_confirmation, string _current_password)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> PUT to '/users' - Update Password");

        var request = new RestRequest("/users", Method.PUT);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        request.AddJsonBody(new {
            password = _password,
            password_confirmation = _password_confirmation,
            current_password = _current_password
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
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "PUT to '/users'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }
          
    public IEnumerator Register_UpdateHandle(string _handle)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> PUT to '/users' - Update Handle");

        var request = new RestRequest("/users", Method.PUT);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        request.AddJsonBody(new {
            handle = _handle
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
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "PUT to '/users'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }  

    public IEnumerator Register_UpdateProfileDescription(string _profile)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> PUT to '/users' - Update Profile Description");

        var request = new RestRequest("/users", Method.PUT);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        request.AddJsonBody(new {
            profile = _profile
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
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "PUT to '/users'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }        

    public IEnumerator Register_UpdateProfileImage(string _thumbnailKey, string _originalKey)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> PUT to '/users' - Update Profile Image");

        var request = new RestRequest("/users", Method.PUT);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        request.AddJsonBody(new { 
            thumbnail_key = _thumbnailKey, 
            original_key = _originalKey
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
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "PUT to '/users'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
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
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "DELETE to '/users'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    //--------------------------------------------
    // Passwords

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
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/users/password'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    //--------------------------------------------
    // Session

    public IEnumerator Session_SignIn(string _login, string _password, string _player_id)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/users/sign_in' - Sign In");

        var request = new RestRequest("/users/sign_in", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);

        request.AddJsonBody(new { 
            login = _login, 
            password = _password,
            player_id = _player_id
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
            yield return m_threadJob.WaitFor();
            VReelJSON.Model_User result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_User>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            UpdateAccessToken(response);
            UpdateLoginTokens(response);

            m_user.m_id = result.data.id;
            m_user.m_handle = result.data.attributes.handle;
            m_user.m_email = result.data.attributes.email;
            m_user.m_name = result.data.attributes.name;
            m_user.m_profileDescription = result.data.attributes.profile;
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/users/sign_in'");
        }

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
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
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "DELETE to '/users/sign_out'");
        }

        UpdateAccessToken(response);

        m_user.Clear();

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    //--------------------------------------------
    // S3

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

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    //--------------------------------------------
    // Post

    public IEnumerator Post_GetPosts(string page = "")
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts?page=" + page + "' - Get a page of posts");

        var request = new RestRequest("/posts?page=" + page, Method.GET);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts?page=" + page + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
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
            ShowErrors(response, "GET to '/posts?page=" + page + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator Post_CreatePost(string _thumbnailKey, string _originalKey, string _caption)
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
            caption = _caption
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
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/posts'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator Post_GetPost(string postId)
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

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator Post_DeletePost(string postId)
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
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "DELETE to '/posts/" + postId + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }
        
    public IEnumerator Post_UpdatePost(string postId, string _caption)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts/" + postId + "' - Show a post");

        var request = new RestRequest("/posts/" + postId, Method.PUT);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        request.AddJsonBody(new { 
            caption = _caption
        });

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

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator Post_GetPostLikes(string postId, string page = "")
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts/" + postId + "/likes?page=" + page + "' - Show all users who like post");

        var request = new RestRequest("/posts/" + postId + "/likes?page=" + page, Method.GET);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts/" + postId + "/likes?page=" + page + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            yield return m_threadJob.WaitFor();
            VReelJSON.Model_Users result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Users>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_usersJSONResult = result;
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/posts/" + postId + "/likes?page=" + page + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator Post_GetPostComments(string postId, string page = "")
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts/" + postId + "/likes?comments=" + page + "' - Show all comments on a post");

        var request = new RestRequest("/posts/" + postId + "/comments?page=" + page, Method.GET);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/posts/" + postId + "/comments?page=" + page + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            yield return m_threadJob.WaitFor();
            VReelJSON.Model_Comments result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Comments>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_commentsJSONResult = result;
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/posts/" + postId + "/comments?page=" + page + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    //--------------------------------------------
    // User

    public IEnumerator User_GetUser(string userId)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/users/" + userId + "' - Get user details");

        var request = new RestRequest("/users/" + userId, Method.GET);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/users/" + userId + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            yield return m_threadJob.WaitFor();
            VReelJSON.Model_User result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_User>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_userJSONResult = result;
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/users/" + userId + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator User_GetUserPosts(string userId, string page = "")
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/users/" + userId + "/posts?page=" + page + "' - Get user details");

        var request = new RestRequest("/users/" + userId + "/posts?page=" + page, Method.GET);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/users/" + userId + "/posts?page=" + page + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
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
            ShowErrors(response, "GET to '/users/" + userId + "/posts?page=" + page + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator User_GetUserFollowers(string userId, string page = "")
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/users/" + userId + "/followers?page=" + page + "' - Get users that follow this user");

        var request = new RestRequest("/users/" + userId + "/followers?page=" + page, Method.GET);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/users/" + userId + "/followers?page=" + page + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            yield return m_threadJob.WaitFor();
            VReelJSON.Model_Users result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Users>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_usersJSONResult = result;
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/users/" + userId + "/followers?page=" + page + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator User_GetUserFollowing(string userId, string page = "")
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/users/" + userId + "/following?page=" + page + "' - Get users that this user follows");

        var request = new RestRequest("/users/" + userId + "/following?page=" + page, Method.GET);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/users/" + userId + "/following?page=" + page + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            yield return m_threadJob.WaitFor();
            VReelJSON.Model_Users result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Users>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_usersJSONResult = result;
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/users/" + userId + "/following?page=" + page + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator User_SearchForUsers(string user)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/search/users/" + user + "' - Search for user");

        var request = new RestRequest("/search/users/" + user, Method.GET);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/search/users/" + user + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            yield return m_threadJob.WaitFor();
            VReelJSON.Model_Users result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Users>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_usersJSONResult = result;
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/search/users/" + user + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    //--------------------------------------------
    // Comment

    public IEnumerator Comment_CreateComment(string postId, string _text)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/posts/" + postId + "/comments' - Create New Comment");

        var request = new RestRequest("/posts/" + postId + "/comments", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        request.AddJsonBody(new { 
            text = _text
        });

        yield return m_threadJob.WaitFor();
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/posts/" + postId + "/comments' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            yield return m_threadJob.WaitFor();
            VReelJSON.Model_Comment result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Comment>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_commentJSONResult = result;
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/posts/" + postId + "/comments'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator Comment_UpdateComment(string commentId, string _text)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> PUT to '/comments/" + commentId + "' - Update a Comment");

        var request = new RestRequest("/comments/" + commentId, Method.PUT);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        request.AddJsonBody(new { 
            text = _text
        });

        yield return m_threadJob.WaitFor();
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> PUT to '/comments/" + commentId + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "PUT to '/comments/" + commentId);
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator Comment_DeleteComment(string commentId)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/comments/" + commentId + "' - Delete a Comment");

        var request = new RestRequest("/comments/" + commentId, Method.DELETE);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/comments/" + commentId + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "DELETE to '/comments/" + commentId);
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    //--------------------------------------------
    // HashTag

    public IEnumerator HashTag_GetHashTagPosts(string hashTag, string page = "")
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/hash_tags/" + hashTag + "/posts?page=" + page + "' - Search for posts by hash_tag");

        var request = new RestRequest("/hash_tags/" + hashTag + "/posts?page=" + page, Method.GET);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/hash_tags/" + hashTag + "/posts?page=" + page + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
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
            ShowErrors(response, "GET to '/hash_tags/" + hashTag + "/posts?page=" + page + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator HashTag_SearchForHashTags(string hashTag)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/search/hash_tags/" + hashTag + "' - Search for hash tags");

        var request = new RestRequest("/search/hash_tags/" + hashTag, Method.GET);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/search/hash_tags/" + hashTag + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            yield return m_threadJob.WaitFor();
            VReelJSON.Model_Tags result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Tags>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_tagsJSONResult = result;
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/search/hash_tags/" + hashTag + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    //--------------------------------------------
    // Follow

    public IEnumerator Follow_FollowUser(string userId)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/follow/" + userId + "' - Follow a user");

        var request = new RestRequest("/follow/" + userId, Method.POST);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/follow/" + userId + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/follow/" + userId + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator Follow_UnfollowUser(string userId)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/follow/" + userId + "' - Unfollow a user");

        var request = new RestRequest("/follow/" + userId, Method.DELETE);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/follow/" + userId + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "DELETE to '/follow/" + userId + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    // ----- CURRENTLY UNUSED ----- //
    public IEnumerator Follow_Followers(string page = "")
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/followers?page=" + page + "' - Get a list of your followers");

        var request = new RestRequest("/followers?page=" + page, Method.GET);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/followers?page=" + page + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            yield return m_threadJob.WaitFor();
            VReelJSON.Model_Users result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Users>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_usersJSONResult = result;
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/followers?page=" + page + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    // ----- CURRENTLY UNUSED ----- //
    public IEnumerator Follow_Following(string page = "")
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/following?page=" + page + "' - Get a list of users you follow");

        var request = new RestRequest("/following?page=" + page, Method.GET);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/following?page=" + page + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            yield return m_threadJob.WaitFor();
            VReelJSON.Model_Users result = null;
            m_threadJob.Start( () => 
                result = RestSharp.SimpleJson.DeserializeObject<VReelJSON.Model_Users>(response.Content)
            );
            yield return m_threadJob.WaitFor();

            m_usersJSONResult = result;
        }
        else // Error Handling
        {            
            ShowErrors(response, "GET to '/following?page=" + page + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    //--------------------------------------------
    // Timeline

    public IEnumerator Timeline_GetPublicTimeline(string page = "")
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/public_timeline?page=" + page + "' - Get a timeline of all posts in the system");

        var request = new RestRequest("/public_timeline?page=" + page, Method.GET);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/public_timeline?page=" + page + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
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
            ShowErrors(response, "GET to '/public_timeline?page=" + page + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator Timeline_GetPersonalTimeline(string page = "")
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/timeline?page=" + page + "' - Get a page of posts");

        var request = new RestRequest("/timeline?page=" + page, Method.GET);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> GET to '/timeline?page=" + page + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
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
            ShowErrors(response, "GET to '/timeline?page=" + page + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    //--------------------------------------------
    // Like

    public IEnumerator Like_LikePost(string postId)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/like/" + postId + "' - Like a post");

        var request = new RestRequest("/like/" + postId, Method.POST);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/like/" + postId + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/like/" + postId + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    public IEnumerator Like_UnlikePost(string postId)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/like/" + postId + "' - Unlike a post");

        var request = new RestRequest("/like/" + postId, Method.DELETE);
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

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> DELETE to '/like/" + postId + "' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "DELETE to '/like/" + postId + "'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    //--------------------------------------------
    // Flag

    public IEnumerator Flag_FlagPost(string postId, string _reason)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/posts/" + postId + "/flags' - Flag a Post");

        var request = new RestRequest("/posts/" + postId + "/flags", Method.POST);
        request.AddHeader("vreel-application-id", m_applicationID);
        request.AddHeader("client", m_user.GetClient());
        request.AddHeader("uid", m_user.GetUID());
        request.AddHeader("access-token", m_user.GetAcceessToken());

        request.AddJsonBody(new { 
            reason = _reason
        });

        yield return m_threadJob.WaitFor();
        float timeBeforeRequest = Time.realtimeSinceStartup;
        IRestResponse response = new RestResponse();
        m_threadJob.Start( () => 
            response = m_vreelClient.Execute(request)
        );
        yield return m_threadJob.WaitFor();
        float timeAfterRequest = Time.realtimeSinceStartup;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: API -> POST to '/posts/" + postId + "/flags' - Response: " + response.Content);

        m_lastStatusCode = response.StatusCode;
        if (IsSuccessCode(m_lastStatusCode))
        {
            // Empty
        }
        else // Error Handling
        {            
            ShowErrors(response, "POST to '/posts/" + postId + "/flags'");
        }

        UpdateAccessToken(response);

        if (Debug.isDebugBuild) LogRequest(request, response, (timeAfterRequest - timeBeforeRequest));
    }

    // **************************
    // Private/Helper functions
    // **************************

    private string GetBackEndURL()
    {
        if (m_user.GetBackEndEnvironment() == User.BackEndEnvironment.kProduction)
        {
            return m_vreelProductionURL;
        }
        else if (m_user.GetBackEndEnvironment() == User.BackEndEnvironment.kStaging)
        {
            return m_vreelStagingURL;
        }
        else
        {
            return m_vreelDevelopmentURL;
        }
    }

    private string GetApplicationID()
    {
        if (m_user.GetBackEndEnvironment() == User.BackEndEnvironment.kProduction)
        {
            return m_vreelProductionApplicationID;
        }
        else if (m_user.GetBackEndEnvironment() == User.BackEndEnvironment.kStaging)
        {
            return m_vreelStagingApplicationID;
        }
        else
        {
            return m_vreelDevelopmentApplicationID;
        }
    }

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

            method = request.Method.ToString(), // ToString() here to have the method as a nice string otherwise it will just show the enum value
            uri = m_vreelClient.BuildUri(request), // This will generate the actual Uri used in the request
        };

        var responseToLog = new
        {
            statusCode = response.StatusCode,
            content = response.Content,
            headers = response.Headers,
            responseUri = response.ResponseUri, // The Uri that actually responded (could be different from the requestUri if a redirection occurred)
            errorMessage = response.ErrorMessage,
        };

        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Request completed in {0} ms, Request: {1}, Response: {2}",
            durationMs, 
            RestSharp.SimpleJson.SerializeObject(requestToLog),
            RestSharp.SimpleJson.SerializeObject(responseToLog)));
    }
}