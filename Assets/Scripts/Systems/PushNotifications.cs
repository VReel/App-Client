using UnityEngine;
using System.Collections.Generic;

public class PushNotifications : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    // **************************
    // Public functions
    // **************************

    public void Start () 
    {
        // Enable line below to enable logging if you are having issues setting up OneSignal. (logLevel, visualLogLevel)
        // OneSignal.SetLogLevel(OneSignal.LOG_LEVEL.INFO, OneSignal.LOG_LEVEL.INFO);

        OneSignal.StartInit("764072f7-5054-4058-b5a6-f5bb724fead1")
            .HandleNotificationOpened(HandleNotificationOpened)
            .EndInit();

        // Call syncHashedEmail anywhere in your app if you have the user's email.
        // This improves the effectiveness of OneSignal's "best-time" notification scheduling feature.
        // OneSignal.syncHashedEmail(userEmail);

        if (Debug.isDebugBuild)
        {
            OneSignal.SetLogLevel(OneSignal.LOG_LEVEL.DEBUG, OneSignal.LOG_LEVEL.DEBUG);
        }

        OneSignal.IdsAvailable((userId, pushToken) => 
        {
            //TODO: Send "userId" over to back-end!
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: UserID: " + userId + " - PushToken: " + pushToken);
        });
    }

    // **************************
    // Private/Helper functions
    // **************************

    // Gets called when the player opens the notification.
    private static void HandleNotificationOpened(OSNotificationOpenedResult result) 
    {
    }
}