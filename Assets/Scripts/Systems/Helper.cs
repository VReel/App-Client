﻿using UnityEngine;
using System;
using System.Collections.Generic;   // List

// This class holds a set of helper functions and values that get used in multiple classes
public static class Helper
{
    // **************************
    // Member Variables
    // **************************

    public const bool kRGB565On = true;
    public const int kMaxImageWidth = 4096; // 2^12
    public const int kThumbnailWidth = 512; // 2^9
    public const int kMaxCaptionOrDescriptionLength = 200; //NOTE: In API its 500 but in UI its currently 200
    public const int kSkyboxSphereIndex = -1;
    public const int kProfilePageSphereIndex = -2;
    public const int kMenuBarProfileSphereIndex = -3;
    public const int kIgnoreImageIndex = -1; // This is for the ImageLoader to ignore the index passed in when deciding whether request is old

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