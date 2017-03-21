using UnityEngine;
using System.Collections.Generic;   // List
using RestSharp.Deserializers;

// This class simply holds Model classes for helping in Deserialisation of the BackEndAPI!
// Top level Models have the prefix "Model_"

namespace VReelJSON
{    
    public class Model_Error
    {
        public List<ErrorData> errors { get; set; }
    }

    public class ErrorData
    {
        public ErrorSource source { get; set; }
        public string detail { get; set; }
    }

    public class ErrorSource
    {        
        public string pointer { get; set; }
    }

    //--------------------------------------------

    public class Model_S3PresignedURL
    {
        public S3PresignedURLData data { get; set; }
    }

    public class S3PresignedURLData
    {
        public string type { get; set; }
        public S3PresignedURLAttributes attributes { get; set; }
    }

    public class S3PresignedURLAttributes
    {
        public KeyAndURL original { get; set; }
        public KeyAndURL thumbnail { get; set; }
    }

    public class KeyAndURL
    {
        public string key { get; set; }
        public string url { get; set; }
    }

    //--------------------------------------------

    public class Model_SignIn
    {
        public SignInData data { get; set; }
    }

    public class SignInData
    {
        public string id { get; set; }
        public string type { get; set; }
        public SignInAttributes attributes { get; set; }
    }

    public class SignInAttributes
    {
        public string email { get; set; }
        public string handle { get; set; }
        public string name { get; set; }
        public string profile { get; set; }
    }       

    //--------------------------------------------

    public class Model_Profile
    {
        [DeserializeAs(Name = "data")]
        public ProfileData data { get; set; }
    }

    public class ProfileData
    {
        [DeserializeAs(Name = "id")]
        public string id { get; set; }

        [DeserializeAs(Name = "type")]
        public string type { get; set; }

        [DeserializeAs(Name = "attributes")]
        public ProfileAttributes attributes { get; set; }
    }

    public class ProfileAttributes
    {
        [DeserializeAs(Name = "thumbnail_url")]
        public string thumbnail_url { get; set; }

        [DeserializeAs(Name = "caption")]
        public string caption { get; set; }

        [DeserializeAs(Name = "created_at")]
        public string created_at { get; set; }

        [DeserializeAs(Name = "edited")]
        public string edited { get; set; }
    }       

    //--------------------------------------------
}