using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;
using System.Threading;

namespace RockVR.Video
{
    /// <summary>
    /// <c>VideoCapturePro</c> component capture video with hardware accelerate.
    /// </summary>
    public class VideoCapturePro : VideoCaptureBase
    {
        /// <summary>
        /// The texture holding the video frame data.
        /// </summary>
        private RenderTexture renderTexture;
        /// <summary>
        /// The texture holding panorama video frame data.
        /// </summary>
        private RenderTexture cubemapTexture;
        private RenderTexture outputTexture;  // equirect or cubemap ends up here
        private RenderTexture externalTexture;
        /// <summary>
        /// Panorama capture variables.
        /// </summary>
        [Tooltip("Offset spherical coordinates (shift equirect)")]
        public Vector2 sphereOffset = Vector2.zero;
        [Tooltip("Scale spherical coordinates (flip equirect, usually just 1 or -1)")]
        public Vector2 sphereScale = Vector2.one;
        [Tooltip("Reference to camera that renders the scene")]
        public Camera sceneCamera;
        /// <summary>
        /// Rotate camera for cubemap lookup.
        /// </summary>
        private bool includeCameraRotation = false;
        /// <summary>
        /// Panorama capture materials.
        /// </summary>
        public Material convertMaterial;
        public Material outputCubemapMaterial;
        public Material downSampleMaterial;
        /// <summary>
        /// Video capture control logic.
        /// </summary>
        public bool capturingStart = false;
        private bool capturingStop = false;
        private bool needToStopCapturing = false;
        /// <summary>
        /// Keep last width, height setup.
        /// </summary>
        private int lastWidth = 0, lastHeight = 0;
        /// <summary>
        /// Event for managing thread.
        /// </summary>
        private ManualResetEvent flushThreadSig;
        private ManualResetEvent liveThreadSig;
        private ManualResetEvent threadShutdown;
        private Thread flushThread;
        private Thread audioThread;
        private Thread liveThread;

        private float fpsTimer = 0.0f;

        private bool flushReady = false;
        private float flushTimer = 0.0f;
        private float flushCycle = 5.0f;
        /// <summary>
        /// Start capture video.
        /// </summary>
        public override void StartCapture()
        {
            if (!isDedicated)
            {
                if (format != FormatType.NORMAL)
                {
                    Debug.LogWarning(
                        "[VideoCapturePro::StartCapture] The pamorama video only " +
                        "support dedicated camera capture!");
                    return;
                }
            }
            if (!File.Exists(PathConfig.ffmpegPath))
            {
                Debug.LogError(
                    "[VideoCapturePro::StartCapture] FFmpeg not found, please add " +
                    "ffmpeg executable before start capture!"
                );
                return;
            }
            filePath = PathConfig.saveFolder + StringUtils.GetH264FileName(StringUtils.GetRandomString(5));
            if (isLiveStreaming)
            {
                if (!StringUtils.IsRtmpAddress(streamingAddress))
                {
                    Debug.LogWarning(
                       "[VideoCapturePro::StartCapture] Video live streaming " +
                       "require rtmp server address setup!"
                    );
                    return;
                }
            }
            if (!capturingStart)
            {
                if (!SetOutputSize())
                {
                    Debug.LogFormat("[VideoCapturePro::StartCapture] Failed due to invalid resolution: {0} x {1}", frameWidth, frameHeight);
                    return;
                }
                if (isLiveStreaming)
                {
                    Debug.LogFormat("[VideoCapturePro::StartCapture] Starting {0} x {1}: {2}", frameWidth, frameHeight, streamingAddress);
                }
                else
                {
                    Debug.LogFormat("[VideoCapturePro::StartCapture] Starting {0} x {1}: {2}", frameWidth, frameHeight, filePath);
                }
            }
            else
            {
                Debug.LogWarning("[VideoCapturePro::StartCapture] Previous capture not finish yet!");
                return;
            }

            capturingStart = true;
            capturingStop = false;
            needToStopCapturing = false;

            flushTimer = 0.0f;
            fpsTimer = 0.0f;

            if (flushThreadSig == null)
            {
                flushThreadSig = new ManualResetEvent(true);
            }
            if (liveThreadSig == null)
            {
                liveThreadSig = new ManualResetEvent(false);
            }
            if (threadShutdown == null)
            {
                threadShutdown = new ManualResetEvent(false);
            }
            if (flushThread == null)
            {
                flushThread = new Thread(MuxingThreadFunction);
                flushThread.Start();
            }
            if (audioThread == null)
            {
                audioThread = new Thread(AudioThreadFunction);
                audioThread.Start();
            }
            if (isLiveStreaming && liveThread == null)
            {
                liveThread = new Thread(LiveThreadFunction);
                liveThread.Start();
            }
        }
        /// <summary>
        /// Stop capture video.
        /// </summary>
        public override void StopCapture()
        {
            needToStopCapturing = true;
        }

        #region Unity Lifecycle
        /// <summary>
        /// Called before any Start functions and also just after a prefab is instantiated.
        /// </summary>
        private new void Awake()
        {
            base.Awake();
            capturingStart = false;
            capturingStop = false;

            if (isPanorama)
            {
                // create cubemap render texture
                cubemapTexture = new RenderTexture(cubemapSize, cubemapSize, 0);
#if UNITY_5_4_OR_NEWER
                cubemapTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;
#else
                cubemapTexture.isCubemap = true;
#endif
                cubemapTexture.hideFlags = HideFlags.HideAndDontSave;

                // create render texture for equirectangular image
                SetOutputSize();

                convertMaterial.hideFlags = HideFlags.DontSave;
                outputCubemapMaterial.hideFlags = HideFlags.DontSave;
                downSampleMaterial.hideFlags = HideFlags.DontSave;

                renderTexture = new RenderTexture(frameWidth, frameHeight, 24);
                captureCamera.targetTexture = renderTexture;
            }
            else
            {
                // create render texture for 2d image
                SetOutputSize();
            }
        }
        /// <summary>
        /// Called once per frame.
        /// </summary>
        private void Update()
        {
            if (needToStopCapturing)
            {
                // Stop encoding 
                capturingStop = true;
            }

            if (capturingStart)
            {
                flushReady = false;
            }

            flushTimer += Time.deltaTime;
            fpsTimer += Time.deltaTime;

            if (fpsTimer >= deltaFrameTime)
            {
                fpsTimer -= deltaFrameTime;
                if (capturingStart)
                {
                    if (isPanorama)
                    {
                        if (sceneCamera)
                        {
                            sceneCamera.transform.position = transform.position;
                            sceneCamera.RenderToCubemap(cubemapTexture); // render cubemap
                        }
                        GPUCaptureLib_StartEncoding(
                            externalTexture.GetNativeTexturePtr(),
                            filePath,
                            isLiveStreaming,
                            targetFramerate,
                            false);
                    }
                    else
                    {
                        if (stereoFormat != StereoType.NONE)
                        {
                            SetStereoVideoFormat(renderTexture);
                            GPUCaptureLib_StartEncoding(
                                finalTargetTexture.GetNativeTexturePtr(),
                                filePath,
                                isLiveStreaming,
                                targetFramerate,
                                true);
                        }
                        else
                        {
                            GPUCaptureLib_StartEncoding(
                                renderTexture.GetNativeTexturePtr(),
                                filePath,
                                isLiveStreaming,
                                targetFramerate,
                                true);
                        }
                    }

                    if (flushTimer > flushCycle && isLiveStreaming)
                    {
                        // [Live] flush input buffers based on flush cycle value
                        flushTimer = 0.0f;
                        GPUCaptureLib_StopEncoding();
                        flushReady = true;
                    }
                    else if (capturingStop && !isLiveStreaming)
                    {
                        // flush input buffers when got stop input
                        GPUCaptureLib_StopEncoding();
                        flushReady = true;
                    }
                }

                // Muxing
                if (flushReady && !isLiveStreaming)
                {
                    // Flush inputs and Stop encoding
                    flushReady = false;
                    capturingStart = false;
                    flushThreadSig.Set();
                }
                else if (flushReady && isLiveStreaming)
                {
                    // Restart encoding after flush
                    flushReady = false;
                    if (capturingStop && !needToStopCapturing)
                    {
                        capturingStop = false;
                    }
                    if (capturingStart && needToStopCapturing)
                    {
                        capturingStart = false;
                    }
                    flushThreadSig.Set();
                    // Signal live stream thread
                    liveThreadSig.Set();
                }
            }
        }
        /// <summary>
        /// OnRenderImage is called after all rendering is complete to render image.
        /// </summary>
        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (capturingStart && format == FormatType.NORMAL)
            {
                Graphics.Blit(src, dest);
                Graphics.SetRenderTarget(renderTexture);
                Graphics.Blit(src, blitMaterial);
                Graphics.SetRenderTarget(null);
                return;
            }
            if (!capturingStart || !isPanorama)
            {
                Graphics.Blit(src, dest);
                return;
            }

            if (panoramaProjection == PanoramaProjectionType.CUBEMAP)
            {
                DisplayCubeMap(dest);
            }
            else if (panoramaProjection == PanoramaProjectionType.EQUIRECTANGULAR)
            {
                DisplayEquirect(dest);
            }
        }
        /// <summary>
        /// This function is called when the MonoBehaviour will be destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (isPanorama)
            {
                DestroyImmediate(cubemapTexture);
                DestroyImmediate(outputTexture);
                DestroyImmediate(externalTexture);
            }
            if (finalTargetTexture != null)
            {
                RenderTexture.ReleaseTemporary(finalTargetTexture);
                finalTargetTexture = null;
            }
            if (renderTexture != null && !isDedicated)
            {
                RenderTexture.ReleaseTemporary(renderTexture);
                renderTexture = null;
            }
            else if (renderTexture != null && isDedicated)
            {
                RenderTexture.Destroy(renderTexture);
            }
        }
        /// <summary>
        /// Sent to all game objects before the application is quit.
        /// </summary>
        private void OnApplicationQuit()
        {
            if (capturingStart)
            {
                GPUCaptureLib_StopEncoding();
                flushThreadSig.Set();
                threadShutdown.Set();
            }

            if (flushThread != null)
            {
                flushThread.Abort();
            }

            if (audioThread != null)
            {
                audioThread.Abort();
            }

            if (liveThread != null)
            {
                liveThread.Abort();
            }
        }
        #endregion

        #region Video Capture Core
        private void LiveThreadFunction()
        {
            while (true)
            {
                liveThreadSig.WaitOne(Timeout.Infinite);
                GPUCaptureLib_StartLiveStream(streamingAddress, PathConfig.ffmpegPath, targetFramerate);
                liveThreadSig.Reset();
                if (needToStopCapturing)
                {
                    GPUCaptureLib_StopLiveStream();
                }
            }
        }

        private void MuxingThreadFunction()
        {
            while (true)
            {
                flushThreadSig.WaitOne(Timeout.Infinite);
                GPUCaptureLib_MuxingData();
                flushThreadSig.Reset();
            }
        }

        private void AudioThreadFunction()
        {
            while (true)
            {
                if (needToStopCapturing)
                {
                    flushThreadSig.Reset();
                }
                else if (capturingStart && !needToStopCapturing)
                {
                    GPUCaptureLib_AudioEncoding();
                }
                Thread.Sleep(10);
            }
        }

        private bool SetOutputSize()
        {
            if (frameWidth == 0 || frameHeight == 0)
            {
                Debug.LogWarning("[VideoCapturePro::SetOutputSize] The width and height shouldn't be zero.");
                return false;
            }
            if (!isPanorama)
            {
                if (frameHeight > frameWidth)
                {
                    if (!MathUtils.CheckPowerOfTwo(frameWidth))
                    {
                        Debug.LogWarning(
                            "[VideoCapturePro::SetOutputSize] The width should be power " +
                            "of two in height > width case.");
                        return false;
                    }
                }
            }
            if (frameWidth == lastWidth && frameHeight == lastHeight)
            {
                return true;
            }

            lastWidth = frameWidth;
            lastHeight = frameHeight;

            if (isPanorama)
            {
                if (outputTexture != null)
                {
                    Destroy(outputTexture);
                }

                outputTexture = new RenderTexture(frameWidth, frameHeight, 0);
                outputTexture.hideFlags = HideFlags.HideAndDontSave;

                if (externalTexture != null)
                {
                    Destroy(externalTexture);
                }

                externalTexture = new RenderTexture(frameWidth, frameHeight, 0);
                externalTexture.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                if (stereoFormat != StereoType.NONE)
                {
                    // Init stereo video material.
                    if (stereoPackMaterial == null)
                    {
                        Debug.LogError("[VideoCaptureBase::Awake] The stereoPackMaterial is not set!");
                        return false;
                    }
                    stereoPackMaterial.hideFlags = HideFlags.HideAndDontSave;
                    stereoPackMaterial.DisableKeyword("STEREOPACK_TOP");
                    stereoPackMaterial.DisableKeyword("STEREOPACK_BOTTOM");
                    stereoPackMaterial.DisableKeyword("STEREOPACK_LEFT");
                    stereoPackMaterial.DisableKeyword("STEREOPACK_RIGHT");
                    // Init stereo target texture.
                    if (stereoTargetTexture != null)
                    {
                        Destroy(stereoTargetTexture);
                    }
                    stereoTargetTexture = new RenderTexture(frameWidth, frameHeight, 24);
                    stereoTargetTexture.Create();
                    // Init final target texture.
                    if (finalTargetTexture != null)
                    {
                        finalTargetTexture.DiscardContents();
                        RenderTexture.ReleaseTemporary(finalTargetTexture);
                        finalTargetTexture = null;
                    }
                    if (finalTargetTexture == null)
                    {
                        finalTargetTexture = RenderTexture.GetTemporary(frameWidth, frameHeight, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);
                    }
                }
                // Init the copy material use hidden shader.
                blitMaterial = new Material(Shader.Find("Hidden/BlitCopy"));
                blitMaterial.hideFlags = HideFlags.HideAndDontSave;
                if (isDedicated)
                {
                    renderTexture = new RenderTexture(frameWidth, frameHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                    // Make sure the rendertexture is created.
                    renderTexture.Create();
                    captureCamera.targetTexture = renderTexture;
                }
                else
                {
                    if (renderTexture != null)
                    {
                        renderTexture.DiscardContents();
                        RenderTexture.ReleaseTemporary(renderTexture);
                        renderTexture = null;
                    }
                    if (renderTexture == null)
                    {
                        renderTexture = RenderTexture.GetTemporary(frameWidth, frameHeight, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);
                    }
                }
            }
            return true;
        }

        // Take screenshot
        public void Screenshot()
        {
            if (isPanorama)
            {
                if (!capturingStart && sceneCamera)
                {
                    sceneCamera.transform.position = transform.position;
                    sceneCamera.RenderToCubemap(cubemapTexture); // render cubemap
                }
            }
            if (!SetOutputSize()) return;
            StartCoroutine(CaptureScreenshot());
        }

        private IEnumerator CaptureScreenshot()
        {
            // yield a frame to re-render into the rendertexture
            yield return new WaitForEndOfFrame();
            string screenshotPath = PathConfig.saveFolder + StringUtils.GetJpgFileName(StringUtils.GetRandomString(5));
            Debug.LogFormat("[VideoCapturePro::Screenshot] Saved {0} x {1} screenshot: {2}", frameWidth, frameHeight, screenshotPath);
            if (isPanorama)
            {
                GPUCaptureLib_SaveScreenShot(externalTexture.GetNativeTexturePtr(), screenshotPath, false);
            }
            else
            {
                GPUCaptureLib_SaveScreenShot(renderTexture.GetNativeTexturePtr(), screenshotPath, true);
            }
        }
        #endregion

        #region Panorama Capture Core
        private void RenderCubeFace(CubemapFace face, float x, float y, float w, float h)
        {
            // texture coordinates for displaying each cube map face
            Vector3[] faceTexCoords =
            {
            // +x
            new Vector3(1, 1, 1),
            new Vector3(1, -1, 1),
            new Vector3(1, -1, -1),
            new Vector3(1, 1, -1),
            // -x
            new Vector3(-1, 1, -1),
            new Vector3(-1, -1, -1),
            new Vector3(-1, -1, 1),
            new Vector3(-1, 1, 1),

            // -y
            new Vector3(-1, -1, 1),
            new Vector3(-1, -1, -1),
            new Vector3(1, -1, -1),
            new Vector3(1, -1, 1),
            // +y // flipped with -y for fb live
            new Vector3(-1, 1, -1),
            new Vector3(-1, 1, 1),
            new Vector3(1, 1, 1),
            new Vector3(1, 1, -1),

            // +z
            new Vector3(-1, 1, 1),
            new Vector3(-1, -1, 1),
            new Vector3(1, -1, 1),
            new Vector3(1, 1, 1),
            // -z
            new Vector3(1, 1, -1),
            new Vector3(1, -1, -1),
            new Vector3(-1, -1, -1),
            new Vector3(-1, 1, -1),
            };

            GL.PushMatrix();
            GL.LoadOrtho();
            GL.LoadIdentity();

            int i = (int)face;

            GL.Begin(GL.QUADS);
            GL.TexCoord(faceTexCoords[i * 4]); GL.Vertex3(x, y, 0);
            GL.TexCoord(faceTexCoords[i * 4 + 1]); GL.Vertex3(x, y + h, 0);
            GL.TexCoord(faceTexCoords[i * 4 + 2]); GL.Vertex3(x + w, y + h, 0);
            GL.TexCoord(faceTexCoords[i * 4 + 3]); GL.Vertex3(x + w, y, 0);
            GL.End();

            GL.PopMatrix();
        }

        private void SetMaterialParameters(Material material)
        {
            // convert to equirectangular
            material.SetTexture("_CubeTex", cubemapTexture);
            material.SetVector("_SphereScale", sphereScale);
            material.SetVector("_SphereOffset", sphereOffset);

            if (includeCameraRotation)
            {
                // cubemaps are always rendered along axes, so we do rotation by rotating the cubemap lookup
                material.SetMatrix("_CubeTransform", Matrix4x4.TRS(Vector3.zero, transform.rotation, Vector3.one));
            }
            else
            {
                material.SetMatrix("_CubeTransform", Matrix4x4.identity);
            }
        }

        private void DisplayCubeMap(RenderTexture dest)
        {
            SetMaterialParameters(outputCubemapMaterial);
            outputCubemapMaterial.SetPass(0);

            Graphics.SetRenderTarget(outputTexture);

            float s = 1.0f / 3.0f;
            RenderCubeFace(CubemapFace.PositiveX, 0.0f, 0.5f, s, 0.5f);
            RenderCubeFace(CubemapFace.NegativeX, s, 0.5f, s, 0.5f);
            RenderCubeFace(CubemapFace.PositiveY, s * 2.0f, 0.5f, s, 0.5f);

            RenderCubeFace(CubemapFace.NegativeY, 0.0f, 0.0f, s, 0.5f);
            RenderCubeFace(CubemapFace.PositiveZ, s, 0.0f, s, 0.5f);
            RenderCubeFace(CubemapFace.NegativeZ, s * 2.0f, 0.0f, s, 0.5f);

            Graphics.SetRenderTarget(null);
            Graphics.Blit(outputTexture, externalTexture);
            Graphics.Blit(outputTexture, dest);
        }

        private void DisplayEquirect(RenderTexture dest)
        {
            SetMaterialParameters(convertMaterial);
            Graphics.Blit(null, externalTexture, convertMaterial);
            Graphics.Blit(externalTexture, dest);
        }
        #endregion

        #region Dll Import
        [DllImport("GPUCaptureLib", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern void GPUCaptureLib_StartEncoding(IntPtr texture, string path, bool isLive, int fps, bool needFlipping);
        [DllImport("GPUCaptureLib", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern void GPUCaptureLib_AudioEncoding();
        [DllImport("GPUCaptureLib", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern void GPUCaptureLib_StopEncoding();
        [DllImport("GPUCaptureLib", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern void GPUCaptureLib_MuxingData();
        [DllImport("GPUCaptureLib", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern void GPUCaptureLib_StartLiveStream(string url, string ffpath, int fps);
        [DllImport("GPUCaptureLib", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern void GPUCaptureLib_StopLiveStream();
        [DllImport("GPUCaptureLib", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern void GPUCaptureLib_SaveScreenShot(IntPtr texture, string path, bool needFlipping);
        #endregion
    }
}