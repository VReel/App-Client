using UnityEngine;
using Amazon;
using Amazon.MobileAnalytics.MobileAnalyticsManager;
using Amazon.CognitoIdentity;

public class Analytics : MonoBehaviour 
{
    private MobileAnalyticsManager m_analyticsManager;

    void Start() 
    {        
        // TODO: Move Cognito Identity Pool to be in EUWest1 region!
        // TODO: Add custom events for number of images viewed, number of images uploaded, etc.

        var credentials = new CognitoAWSCredentials(
            "us-east-1:76e86965-28da-4906-bf7d-ed48c4e50477", // Amazon Cognito Identity Pool ID
            RegionEndpoint.USEast1 // Cognito Identity Region
        ); 
        
        m_analyticsManager = MobileAnalyticsManager.GetOrCreateInstance(
            "410c9ef3e9d94a74afaa2d5bb96426f9", // Amazon Mobile Analytics App ID
            credentials,
            RegionEndpoint.USEast1 // Cognito Identity Region
        ); 
    }

    // You want this session management code only in one game object
    // that persists through the game life cycles using “DontDestroyOnLoad (transform.gameObject);”
    void OnApplicationFocus(bool focus) 
    {
        if (m_analyticsManager != null) 
        {
            if (focus) 
            {
                m_analyticsManager.ResumeSession();
            } 
            else 
            {    
                m_analyticsManager.PauseSession();
            }
        }
    }
}