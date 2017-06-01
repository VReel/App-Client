using UnityEngine;
using System.Collections.Generic;

public class PushNotifications : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    public string m_oneSignalPlayerID {get; set;}
    public string m_oneSignalPushToken {get; set;}

    // **************************
    // Public functions
    // **************************

    public void Start () 
    {
        // Enable line below to enable logging if you are having issues setting up OneSignal. (logLevel, visualLogLevel)
        // OneSignal.SetLogLevel(OneSignal.LOG_LEVEL.INFO, OneSignal.LOG_LEVEL.INFO);

        OneSignal.StartInit("764072f7-5054-4058-b5a6-f5bb724fead1")
            .HandleNotificationOpened(HandleNotificationOpened)
            .HandleNotificationReceived(HandleNotificationReceived)
            .InFocusDisplaying(OneSignal.OSInFocusDisplayOption.Notification)
            .EndInit();        

        OneSignal.IdsAvailable((userId, pushToken) => 
        {
            m_oneSignalPlayerID = userId;
            m_oneSignalPushToken = pushToken;

            if (Debug.isDebugBuild) Debug.Log("------- VREEL: UserID: " + userId + " - PushToken: " + pushToken);
        });

        // Call syncHashedEmail anywhere in your app if you have the user's email.
        // This improves the effectiveness of OneSignal's "best-time" notification scheduling feature.
        // OneSignal.syncHashedEmail(userEmail);

        if (Debug.isDebugBuild)
        {
            OneSignal.SetLogLevel(OneSignal.LOG_LEVEL.DEBUG, OneSignal.LOG_LEVEL.DEBUG);
        }
    }

    // **************************
    // Private/Helper functions
    // **************************

    // Gets called when the player receives the notification.
    private static void HandleNotificationReceived(OSNotification result) 
    {
    }

    // Gets called when the player opens the notification.
    private static void HandleNotificationOpened(OSNotificationOpenedResult result) 
    {
        
    }
}