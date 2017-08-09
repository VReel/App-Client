using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;

namespace RockVR.Video
{
    /// <summary>
    /// <c>VideoCaptureProCtrl</c> component, manage and record gameplay from specific camera.
    /// Work with <c>VideoCapturePro</c> component to generate gameplay videos.
    /// </summary>
    public class VideoCaptureProCtrl : VideoCaptureCtrlBase
    {
        /// <summary>
        /// Initial instance and init variable.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            // For easy access the CameraCaptures var.
            if (videoCaptures == null)
                videoCaptures = new VideoCapturePro[0];
            // Create default root folder if not created.
            if (!Directory.Exists(PathConfig.saveFolder))
            {
                Directory.CreateDirectory(PathConfig.saveFolder);
            }
            status = StatusType.NOT_START;
        }

        /// <summary>
        /// Initialize the attributes of the capture session and start capture.
        /// </summary>
        public override void StartCapture()
        {
            if (status != StatusType.NOT_START &&
                status != StatusType.FINISH)
            {
                Debug.LogWarning("[VideoCaptureProCtrl::StartCapture] Previous " +
                                 " capture not finish yet!");
                return;
            }
            // Filter out disabled capture component.
            List<VideoCapturePro> validCaptures = new List<VideoCapturePro>();
            if (validCaptures != null && videoCaptures.Length > 0)
            {
                foreach (VideoCapturePro videoCapture in videoCaptures)
                {
                    if (videoCapture != null && videoCapture.gameObject.activeSelf)
                    {
                        validCaptures.Add(videoCapture);
                    }
                }
            }
            videoCaptures = validCaptures.ToArray();
            for (int i = 0; i < videoCaptures.Length; i++)
            {
                VideoCapturePro videoCapture = (VideoCapturePro)videoCaptures[i];
                if (videoCapture == null || !videoCapture.gameObject.activeSelf)
                {
                    continue;
                }
                videoCapture.StartCapture();
            }
            status = StatusType.STARTED;
        }

        /// <summary>
        /// Stop video capture process and check FINISH status.
        /// </summary>
        public override void StopCapture()
        {
            if (status != StatusType.STARTED)
            {
                Debug.LogWarning("[VideoCaptureProCtrl::StopCapture] capture session " +
                                 "not start yet!");
                return;
            }
            foreach (VideoCapturePro videoCapture in videoCaptures)
            {
                if (!videoCapture.gameObject.activeSelf)
                {
                    continue;
                }
                videoCapture.StopCapture();
            }
            status = StatusType.STOPPED;
            //StartCoroutine(SendUploadRequest());
            StartCoroutine(CheckCapturingFinish());
        }

        private IEnumerator CheckCapturingFinish()
        {
            while (true)
            {
                // At least wait 1 second.
                yield return new WaitForSeconds(1);
                bool capturing = false;
                foreach (VideoCapturePro videoCapture in videoCaptures)
                {
                    if (!videoCapture.gameObject.activeSelf)
                    {
                        continue;
                    }
                    if (videoCapture.capturingStart)
                    {
                        capturing = true;
                        break;
                    }
                }
                if (!capturing)
                {
                    status = StatusType.FINISH;
                    break;
                }
            }
        }

        private IEnumerator SendUploadRequest()
        {
            foreach (VideoCapturePro videoCapture in videoCaptures)
            {
                Debug.Log("[VideoCaptureProCtrl::SendUploadRequest] Waiting...");
                // delay seconds
                yield return new WaitForSeconds(1);
                Debug.Log("[VideoCaptureProCtrl::SendUploadRequest] Start: " + videoCapture.filePath);
                WWWForm form = new WWWForm();
                form.AddField("file", videoCapture.filePath);
                WWW www = new WWW("http://127.0.0.1:8001/upload", form);
                yield return www;
                if (www.error != null)
                {
                    Debug.LogWarning("[VideoCaptureProCtrl::SendUploadRequest] WWW with error: ");
                    Debug.LogWarning(www.error);
                }
                Debug.Log("[VideoCaptureProCtrl::SendUploadRequest] Success!");
            }
        }
    }
}