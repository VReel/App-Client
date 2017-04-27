using UnityEngine;
using System.Collections.Generic;   // List
using RestSharp.Deserializers;

// This class simply holds Model classes for helping in Deserialisation of the BackEndAPI!
// Top level Models have the prefix "Model_"

namespace VReelJSON
{    
    //--------------------------------------------
    // Generic

    public class GenericLinks
    {
        public string next { get; set; }
    }

    public class GenericMeta
    {
        public bool next_page { get; set; }
        public string next_page_id { get; set; }
    }

    public class GenericRelationship
    {
        public GenericRelationshipData data { get; set; }
    }

    public class GenericRelationshipData
    {
        public string id { get; set; }
        public string type { get; set; }       
    }

    //--------------------------------------------
    // Error

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
    // S3PresignedURL

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
    // Users

    public class Model_User
    {
        public UserData data { get; set; }
    }

    public class UserData
    {
        public string id { get; set; }
        public string type { get; set; }
        public UserAttributes attributes { get; set; }
    }

    public class UserAttributes
    {
        public string handle { get; set; }
        public string name { get; set; }
        public string thumbnail_url { get; set; }
        public int follower_count { get; set; }
        public int following_count { get; set; }
        public int post_count { get; set; }
        public string email { get; set; }
        public string profile { get; set; }
        public string original_url { get; set; }
        public bool follows_me { get; set; }
        public bool followed_by_me { get; set; }
    }

    //--------------------------------------------

    public class Model_Users
    {
        public List<UserData> data { get; set; }
        public GenericLinks links { get; set; }
        public GenericMeta meta { get; set; }
    }

    //--------------------------------------------
    // Tags

    public class Model_Tag
    {
        public TagData data { get; set; }
    }

    public class TagData
    {
        public string id { get; set; }
        public string type { get; set; }
        public TagAttributes attributes { get; set; }
    }

    public class TagAttributes
    {
        public string tag { get; set; }
    }       

    //--------------------------------------------

    public class Model_Tags
    {
        public List<TagData> data { get; set; }
    }

    //--------------------------------------------
    // Posts

    public class Model_Post
    {     
        public PostData data { get; set; }
    }

    public class PostData
    {
        public string id { get; set; }
        public string type { get; set; }
        public PostAttributes attributes { get; set; }
        public PostRelationships relationships { get; set; }
    }

    public class PostAttributes
    {
        public string thumbnail_url { get; set; }
        public string caption { get; set; }
        public int like_count { get; set; }
        public int comment_count { get; set; }
        public string created_at { get; set; }
        public bool edited { get; set; }
        public bool liked_by_me { get; set; }
        public string original_url { get; set; } // NOTE: This field is not included when in Model_Posts
    }     

    public class PostRelationships
    {
        public GenericRelationship user { get; set; }
    } 

    //--------------------------------------------

    public class Model_Posts
    {     
        public List<PostData> data { get; set; }
        public List<UserData> included { get; set; }
        public GenericLinks links { get; set; }
        public GenericMeta meta { get; set; }
    }
        
    //--------------------------------------------
    // Comments

    public class Model_Comment
    {     
        public CommentData data { get; set; }
    }

    public class CommentData
    {
        public string id { get; set; }
        public string type { get; set; }
        public CommentAttributes attributes { get; set; }
        public CommentRelationships relationships { get; set; }
    }

    public class CommentAttributes
    {
        public string text { get; set; }
        public bool edited { get; set; }
    }

    public class CommentRelationships
    {
        public GenericRelationship user { get; set; }
        public GenericRelationship post { get; set; }
    }       

    //--------------------------------------------

    public class Model_Comments
    {     
        public List<CommentData> data { get; set; }
        public List<UserData> included { get; set; }
        public GenericLinks links { get; set; }
        public GenericMeta meta { get; set; }
    }

    //--------------------------------------------
}