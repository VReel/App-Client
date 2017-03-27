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

    public class Model_Posts
    {     
        public List<PostsData> data { get; set; }
        public PostsLinks links { get; set; }
        public PostsMeta meta { get; set; }
    }

    public class PostsData
    {
        public string id { get; set; }
        public string type { get; set; }
        public PostsAttributes attributes { get; set; }
    }

    public class PostsAttributes
    {
        public string thumbnail_url { get; set; }
        public string caption { get; set; }
        public string created_at { get; set; }
        public bool edited { get; set; }
    }       

    public class PostsLinks
    {
        public string next { get; set; }
    }

    public class PostsMeta
    {
        public bool next_page { get; set; }
        public string next_page_id { get; set; }
    }

    //--------------------------------------------

    public class Model_Post
    {     
        public PostData data { get; set; }
    }

    public class PostData
    {
        public string id { get; set; }
        public string type { get; set; }
        public PostAttributes attributes { get; set; }
    }

    public class PostAttributes
    {
        public string thumbnail_url { get; set; }
        public string original_url { get; set; }
        public string caption { get; set; }
        public string created_at { get; set; }
        public bool edited { get; set; }
    }       

    //--------------------------------------------
}