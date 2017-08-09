using UnityEngine;
using UnityEditor;

namespace RockVR.Video.Editor
{
    /// <summary>
    /// <c>VideoCapturePro</c> component editor.
    /// </summary>
    [CustomEditor(typeof(VideoCapturePro))]
    public class VideoCaptureProEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            VideoCapturePro videoCapture = (VideoCapturePro)target;

            videoCapture.mode = (VideoCapturePro.ModeType)EditorGUILayout.EnumPopup("Mode", videoCapture.mode);

            if (videoCapture.mode == VideoCapturePro.ModeType.LIVE_STREAMING)
            {
                videoCapture.streamingAddress = EditorGUILayout.TextField("Streaming Server Address", videoCapture.streamingAddress);
            }
            videoCapture.format = (VideoCapturePro.FormatType)EditorGUILayout.EnumPopup("Format", videoCapture.format);

            if (videoCapture.format == VideoCapturePro.FormatType.NORMAL)
            {
                if (videoCapture.isDedicated)
                {
                    videoCapture.frameSize = (VideoCapturePro.FrameSizeType)EditorGUILayout.EnumPopup("Frame Size", videoCapture.frameSize);
                }
                videoCapture.stereoFormat = (VideoCapture.StereoType)EditorGUILayout.EnumPopup("Stereo Format", videoCapture.stereoFormat);
                if (videoCapture.stereoFormat != VideoCapture.StereoType.NONE)
                {
                    videoCapture.interPupillaryDistance = EditorGUILayout.FloatField("Inter Pupillary Distance", videoCapture.interPupillaryDistance);
                    videoCapture.stereoPackMaterial = (Material)EditorGUILayout.ObjectField("Stereoscopic Material", videoCapture.stereoPackMaterial, typeof(Material));
                }
            }
            else if (videoCapture.format == VideoCapturePro.FormatType.PANORAMA)
            {
                videoCapture.sceneCamera = (Camera)EditorGUILayout.ObjectField("Capture Camera", videoCapture.sceneCamera, typeof(Camera));
                videoCapture.panoramaProjection = (VideoCapturePro.PanoramaProjectionType)EditorGUILayout.EnumPopup("Projection Type", videoCapture.panoramaProjection);
                if (videoCapture.panoramaProjection == VideoCapturePro.PanoramaProjectionType.EQUIRECTANGULAR)
                {
                    videoCapture.frameSize = (VideoCapturePro.FrameSizeType)EditorGUILayout.EnumPopup("Frame Size", videoCapture.frameSize);
                    videoCapture.sphereOffset = EditorGUILayout.Vector2Field("Offset Spherical Coordinates", videoCapture.sphereOffset);
                    videoCapture.sphereScale = EditorGUILayout.Vector2Field("Offset Spherical Coordinates", videoCapture.sphereScale);
                }
                videoCapture._cubemapSize = (VideoCapturePro.CubemapSizeType)EditorGUILayout.EnumPopup("Cubemap Size", videoCapture._cubemapSize);
                videoCapture.convertMaterial = (Material)EditorGUILayout.ObjectField("Convert Material", videoCapture.convertMaterial, typeof(Material));
                videoCapture.outputCubemapMaterial = (Material)EditorGUILayout.ObjectField("Output Cubemap Material", videoCapture.outputCubemapMaterial, typeof(Material));
                videoCapture.downSampleMaterial = (Material)EditorGUILayout.ObjectField("Down Sample Material", videoCapture.downSampleMaterial, typeof(Material));
            }
            videoCapture._antiAliasing = (VideoCapturePro.AntiAliasingType)EditorGUILayout.EnumPopup("Anti Aliasing", videoCapture._antiAliasing);
            videoCapture._targetFramerate = (VideoCapturePro.TargetFramerateType)EditorGUILayout.EnumPopup("Target FrameRate", videoCapture._targetFramerate);
            videoCapture.isDedicated = EditorGUILayout.Toggle("Dedicated Camera", videoCapture.isDedicated);
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}