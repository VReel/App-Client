using UnityEngine;
using System;

// This class holds a set of helper functions to be used throughout the code
public static class Helper
{
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