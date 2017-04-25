using UnityEngine;
using System;

// This class holds a set of helper functions to be used throughout the code
public static class Helper
{
    // **************************
    // Member Variables
    // **************************

    public const int kStandardThumbnailWidth = 640;
    public const int kMaxCaptionOrDescriptionLength = 200; //NOTE: In API its 500 but in UI its currently 200
    public const int kSkyboxSphereIndex = -1;
    public const int kProfileSphereIndex = -2;

    // **************************
    // Public functions
    // **************************

    public static void TruncateString(ref string value, int maxLength)
    {
        if (!string.IsNullOrEmpty(value) && value.Length > maxLength ) 
        {
            value.Substring(0, maxLength); 
        }
    }
}