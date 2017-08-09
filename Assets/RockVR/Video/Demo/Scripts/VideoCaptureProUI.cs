using UnityEngine;
using System.Diagnostics;

namespace RockVR.Video.Demo
{
    public class VideoCaptureProUI : MonoBehaviour
    {
        private void Awake()
        {
            Application.runInBackground = true;
        }

        private void OnGUI()
        {
            if (VideoCaptureProCtrl.instance.status == VideoCaptureProCtrl.StatusType.NOT_START)
            {
                if (GUI.Button(new Rect(10, Screen.height - 60, 150, 50), "Start Capture"))
                {
                    VideoCaptureProCtrl.instance.StartCapture();
                }
            }
            else if (VideoCaptureProCtrl.instance.status == VideoCaptureProCtrl.StatusType.STARTED)
            {
                if (GUI.Button(new Rect(10, Screen.height - 60, 150, 50), "Stop Capture"))
                {
                    VideoCaptureProCtrl.instance.StopCapture();
                }
            }
            else if (VideoCaptureProCtrl.instance.status == VideoCaptureProCtrl.StatusType.STOPPED)
            {
                if (GUI.Button(new Rect(10, Screen.height - 60, 150, 50), "Processing"))
                {
                    // Waiting processing end.
                }
            }
            else if (VideoCaptureProCtrl.instance.status == VideoCaptureProCtrl.StatusType.FINISH)
            {
                if (GUI.Button(new Rect(10, Screen.height - 60, 150, 50), "View Video"))
                {
                    // Open video save directory.
                    Process.Start(PathConfig.saveFolder);
                }
            }
        }
    }
}