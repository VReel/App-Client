using UnityEngine;
using System;
using System.Collections.Generic;   // List

// This class holds a set of helper functions and values that get used in multiple classes
public static class Helper
{
    // **************************
    // Member Variables
    // **************************

    public const int kMaxImageWidth = 2^12; // 4096
    public const int kStandardThumbnailWidth = 2^9; // 512
    public const int kMaxCaptionOrDescriptionLength = 200; //NOTE: In API its 500 but in UI its currently 200
    public const int kSkyboxSphereIndex = -1;
    public const int kProfilePageSphereIndex = -2;
    public const int kMenuBarProfileSphereIndex = -3;

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

    public static string GetHandleFromIDAndUserData(List<VReelJSON.UserData> userData, string userId)
    {
        for (int i = 0; i < userData.Count; i++)
        {
            if (userData[i].id.CompareTo(userId) == 0)
            {
                return userData[i].attributes.handle;
            }
        }

        return "HANDLE_ERROR";
    }
}