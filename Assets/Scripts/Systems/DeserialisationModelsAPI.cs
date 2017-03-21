using UnityEngine;
using System.Collections.Generic;   // List

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

    //--------------------------------------------
}