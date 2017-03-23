using UnityEngine;
using UnityEngine.UI;               //Text
using UnityEngine.Networking;       //UnityWebRequest
using System;                       //GC
using System.IO;                    //DirectoryInfo
using System.Collections;           //IEnumerator
using System.Collections.Generic;   //List
using System.Threading;             //Threading

using System.Drawing;

public class DeviceGallery : MonoBehaviour 
{    
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private GameObject m_errorMessage;
    [SerializeField] private GameObject m_noGalleryImagesText;   
    [SerializeField] private GameObject m_galleryMessage;
    [SerializeField] private GameObject m_staticLoadingIcon;

    private int m_currGalleryImageIndex = 0;
    private List<string> m_galleryImageFilePaths;
    private CoroutineQueue m_coroutineQueue;
    private AndroidJavaClass m_javaPluginClass;
    private ThreadJob m_threadJob;
    private BackEndAPI m_backEndAPI;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {        
        AndroidJNI.AttachCurrentThread();
        m_javaPluginClass = new AndroidJavaClass("io.vreel.vreel.JavaPlugin");

        m_galleryImageFilePaths = new List<string>();
        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        m_threadJob = new ThreadJob(this);

        m_backEndAPI = new BackEndAPI(this, m_errorMessage, m_user);
    }

    public void InvalidateGalleryImageLoading() // This function is called in order to stop any ongoing image loading 
    {
        m_currGalleryImageIndex = -1;
        if (m_coroutineQueue != null)
        {
            m_coroutineQueue.Clear();
        }
    }

    public bool IsGalleryIndexAtStart()
    {
        return m_currGalleryImageIndex <= 0;
    }

    public bool IsGalleryIndexAtEnd()
    {
        int numImageSpheres = m_imageSphereController.GetNumSpheres();
        int numFiles = m_galleryImageFilePaths.Count;
        return m_currGalleryImageIndex >= (numFiles - numImageSpheres);       
    }
        
    public void UploadImage()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: UploadImage() called");

        m_coroutineQueue.EnqueueAction(UploadImageInternal());
    }

    public void OpenAndroidGallery()
    {        
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenAndroidGallery() called");

        m_currGalleryImageIndex = 0;
        m_galleryImageFilePaths.Clear();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: About to call GetImagesPath function...");
        string imagesTopLevelDirectory = m_javaPluginClass.CallStatic<string>("GetAndroidImagesPath"); //string path = "/storage/emulated/0/DCIM/Gear 360/";
        m_imageSkybox.SetTopLevelDirectory(imagesTopLevelDirectory);
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Storing all FilePaths from directory: " + imagesTopLevelDirectory);

        m_coroutineQueue.EnqueueAction(StoreAllImageGalleryFilePaths(imagesTopLevelDirectory));

        m_currGalleryImageIndex = 0;
        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        m_imageSphereController.SetAllImageSpheresToLoading();
        m_coroutineQueue.EnqueueAction(LoadImages(m_currGalleryImageIndex, numImagesToLoad));
    }

    public void NextImages()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: NextImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_galleryImageFilePaths.Count;

        m_currGalleryImageIndex = Mathf.Clamp(m_currGalleryImageIndex + numImagesToLoad, 0, numFilePaths);

        m_coroutineQueue.Clear(); // Throw away previous operations
        m_coroutineQueue.EnqueueAction(LoadImages(m_currGalleryImageIndex, numImagesToLoad));
    }

    public void PreviousImages()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreviousImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_galleryImageFilePaths.Count;

        m_currGalleryImageIndex = Mathf.Clamp(m_currGalleryImageIndex - numImagesToLoad, 0, numFilePaths);

        m_coroutineQueue.Clear(); // Throw away previous operations
        m_coroutineQueue.EnqueueAction(LoadImages(m_currGalleryImageIndex, numImagesToLoad));
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator UploadImageInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_staticLoadingIcon.SetActive(true);

        // 1) Get Original Image byte array
        string originalImageFileName = m_imageSkybox.GetImageFilePath();
        byte[] originalImageByteArray = File.ReadAllBytes(originalImageFileName);
            // File.ReadAllBytes(System.IO.Directory.GetCurrentDirectory() + "/Assets/Berlin_Original.jpg"); 
        //var stream = new FileStream(originalFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        yield return new WaitForEndOfFrame();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Step 1 Complete");

        // 2) Create Thumbnail - TODO Get CreateThumbnail() to work!
        const int kStandardThumbnailWidth = 320;
        byte[] thumbnailImageByteArray = CreateThumbnail(originalImageFileName, kStandardThumbnailWidth);
            //File.ReadAllBytes(System.IO.Directory.GetCurrentDirectory() + "/Assets/Berlin_Thumbnail.jpg"); 
        yield return new WaitForEndOfFrame();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Step 2 Complete");

        // 3) Get PresignedURL
        yield return m_backEndAPI.S3_PresignedURL();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Step 3 Complete");

        // 4) Upload Original
        yield return m_backEndAPI.UploadObject(
            m_backEndAPI.GetS3PresignedURLResult().data.attributes.original.url.ToString(),
            originalImageByteArray
        );

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Step 4 Complete");

        // 5) Upload Thumbnail
        yield return m_backEndAPI.UploadObject(
            m_backEndAPI.GetS3PresignedURLResult().data.attributes.thumbnail.url.ToString(),
            thumbnailImageByteArray
        );

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Step 5 Complete");

        // 6) Register Post as Created
        yield return m_backEndAPI.Posts_Create(
            m_backEndAPI.GetS3PresignedURLResult().data.attributes.thumbnail.key.ToString(), 
            m_backEndAPI.GetS3PresignedURLResult().data.attributes.original.key.ToString()
        );

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Step 6 Complete");

        // 7) If there has been a successful upload -> Inform user that image has been uploaded      
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Uploaded image: " + originalImageFileName);
        Text galleryTextComponent = m_galleryMessage.GetComponentInChildren<Text>();
        if (galleryTextComponent != null)
        {
            galleryTextComponent.text = "Succesful Upload!";
            galleryTextComponent.color = UnityEngine.Color.black;
        }
        m_galleryMessage.SetActive(true);


        m_staticLoadingIcon.SetActive(false);
    }

    private IEnumerator StoreAllImageGalleryFilePaths(string imagesTopLevelDirectory)
    {                
        // 1) Find all files that could potentially be 360 images.
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling GetAllFileNamesRecursively()");
        List<string> files = new List<string>();
        bool isDebugBuild = Debug.isDebugBuild;
        Text galleryTextComponent = m_galleryMessage.GetComponentInChildren<Text>();
        m_threadJob.Start( () => 
            files = GetAllFileNamesRecursively(imagesTopLevelDirectory, isDebugBuild, galleryTextComponent)
        );
        yield return m_threadJob.WaitFor();

        // TODO: Add CalcAspectRatio() to C++ Plugin, which should then allow me to thread this function out...
        //      the only reason its not currently threaded is because of the Android Plugin needing to be called...
        // 2) Add the files that are actually 360 images to "m_galleryImageFilePaths"
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling FilterAndAdd360Images()");
        const int kNumFilesPerIteration = 1;
        int numFilesSearched = 0;
        foreach (string filePath in files)
        { 
            if (Is360Image(filePath))
            {   
                m_galleryImageFilePaths.Add(filePath);
            }

            numFilesSearched++;
            if (numFilesSearched % kNumFilesPerIteration == 0)
            {                
                yield return new WaitForEndOfFrame();
            }
        }

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Searched the directory " + imagesTopLevelDirectory + ", through " + numFilesSearched +
            " files, and found " + m_galleryImageFilePaths.Count + " 360-image files!");

        // 3) Sort files in order of newest first, so that users see their most recent gallery images!
        yield return new WaitForEndOfFrame();
        bool ranSuccessfully = false;
        m_threadJob.Start( () => 
            ranSuccessfully = SortGalleryImageFilePaths()           
        );
        yield return m_threadJob.WaitFor();

        //GC.Collect();

        bool noImagesInGallery = m_galleryImageFilePaths.Count <= 0;
        m_noGalleryImagesText.SetActive(noImagesInGallery); // If the user has yet take any 360-images then show them the NoGalleryImagesText!
    }
    
    private List<string> GetAllFileNamesRecursively(string baseDirectory, bool isDebugBuild, Text galleryTextComponent)
    {
        // We iterate over all files in the given top level directory, recursively searching through all the subdirectories
        var files = new List<string>();
        FileAttributes undesiredAttributes = (FileAttributes.Hidden | FileAttributes.System | FileAttributes.Temporary);

        try
        {
            foreach(string filePath in System.IO.Directory.GetFiles(baseDirectory, "*", SearchOption.TopDirectoryOnly))
            {            
                FileAttributes checkedFileAttributes = File.GetAttributes(filePath) & undesiredAttributes;
                if (checkedFileAttributes == 0)
                {
                    files.Add(filePath);
                }
            }
        }
        catch 
        {
            if (isDebugBuild) Debug.Log("------- VREEL: Call to GetFiles() failed for: " + baseDirectory);

            // Report Failure in Gallery
            if (galleryTextComponent != null)
            {
                galleryTextComponent.text = "Reading files Failed!\n Check permissions!";
                galleryTextComponent.color = UnityEngine.Color.red;
            }
            m_galleryMessage.SetActive(true);
        }

        try
        {
            foreach(string dirName in System.IO.Directory.GetDirectories(baseDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                FileAttributes checkedFolderAttributes = (new DirectoryInfo(dirName).Attributes) & undesiredAttributes;
                if (checkedFolderAttributes == 0)
                {
                    files.AddRange(GetAllFileNamesRecursively(dirName, isDebugBuild, galleryTextComponent));
                }
            }
        }
        catch 
        {
            if (isDebugBuild) Debug.Log("------- VREEL: Call to GetDirectories() failed for: " + baseDirectory);

            // Report Failure in Gallery
            if (galleryTextComponent != null)
            {
                galleryTextComponent.text = "Reading files Failed!\n Check permissions!";
                galleryTextComponent.color = UnityEngine.Color.red;
            }
            m_galleryMessage.SetActive(true);
        }

        return files;
    }
        
    private static readonly List<string> s_imageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
    private bool Is360Image(string filePath)
    {            
        // Check that it has an image file extension 
        if (!s_imageExtensions.Contains(Path.GetExtension(filePath).ToUpperInvariant()))
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: File: " + filePath + " is not an image!");
            return false;
        }

        // NOTE: My current rudimentatry implementation of this function is very simply to check if the aspect ratio is 2:1!
        // This works the majority of the time because all 360 images have a 2:1 ratio,
        // and its not a standard aspect ratio for any other type of image!

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: About to call Aspect Ratio function for file: " + filePath);
        float aspectRatio = m_javaPluginClass.CallStatic<float>("CalcAspectRatio", filePath);
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Aspect Ratio: " + aspectRatio);

        const float kDesiredAspectRatio = 2.0f;
        bool isImage360 = Mathf.Abs(aspectRatio - kDesiredAspectRatio) < Mathf.Epsilon;
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Image: " + filePath + " is 360: " + isImage360);

        return isImage360;
    }        

    private bool SortGalleryImageFilePaths()
    {
        m_galleryImageFilePaths.Sort(delegate(string file1, string file2)
        {
            return File.GetCreationTime(file2).CompareTo(File.GetCreationTime(file1));
        });

        return true;
    }

    private IEnumerator LoadImages(int startingGalleryImageIndex, int numImagesToLoad)
    {
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Loading {0} images beginning at index {1}. There are {2} images in the gallery!", 
            numImagesToLoad, startingGalleryImageIndex, m_galleryImageFilePaths.Count));

        Resources.UnloadUnusedAssets();

        bool imageRequestStillValid = 
            (m_currGalleryImageIndex != -1) && 
            (m_currGalleryImageIndex <= startingGalleryImageIndex) &&  
            (startingGalleryImageIndex < m_currGalleryImageIndex + numImagesToLoad); // Request no longer valid as user has moved on from this page

        string logString = string.Format("------- VREEL: Checking validity returned '{0}' when checking that {1} <= {2} < {1}+{3}", imageRequestStillValid, startingGalleryImageIndex, m_currGalleryImageIndex, numImagesToLoad); 
        if (Debug.isDebugBuild) Debug.Log(logString);
        if (!imageRequestStillValid)
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: LoadImages() with index = " + startingGalleryImageIndex + " has failed");
            yield break;
        }
            
        int currGalleryImageIndex = startingGalleryImageIndex;
        for (int sphereIndex = 0; sphereIndex < numImagesToLoad; sphereIndex++, currGalleryImageIndex++)
        {
            if (currGalleryImageIndex < m_galleryImageFilePaths.Count)
            {                       
                string filePath = m_galleryImageFilePaths[currGalleryImageIndex];

                bool filePathStillValid = filePath.CompareTo(m_imageSkybox.GetImageFilePath()) != 0; // If file-path is the same then ignore request
                if (Debug.isDebugBuild) Debug.Log("------- VREEL: Checking that filePath has changed has returned = " + filePathStillValid);
                if (filePathStillValid)
                {
                    m_coroutineQueue.EnqueueAction(LoadImageInternalPlugin(filePath, sphereIndex));
                }
            }
            else
            {
                m_imageSphereController.HideSphereAtIndex(sphereIndex);
            }
        }

        Resources.UnloadUnusedAssets();
        yield return null;
    }

    private IEnumerator LoadImageInternalPlugin(string filePath, int sphereIndex)
    {   
        yield return m_imageSphereController.LoadImageFromPath(sphereIndex, filePath);
    }

    private IEnumerator LoadImageInternalUnity(string filePath, int sphereIndex)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadPicturesInternalUnity() from filePath: " + filePath);

        WWW www = new WWW("file://" + filePath);
        yield return www;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadImageIntoTexture()");
        Texture2D myNewTexture2D = new Texture2D(2,2);
        www.LoadImageIntoTexture(myNewTexture2D);
        yield return new WaitForEndOfFrame();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished LoadImageIntoTexture()");

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling SetImageAndFilePath()");
        m_imageSphereController.SetImageAndFilePathAtIndex(sphereIndex, myNewTexture2D, filePath, -1);
        yield return new WaitForEndOfFrame();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished SetImageAndFilePath()");

        Resources.UnloadUnusedAssets();
    }

    //TODO: GET THIS FUNCTION WORKING SOMEHOW!
    public byte[] CreateThumbnail(string originalImagePath, int thumbnailWidth)
    {
        MemoryStream outputStream = new MemoryStream();


        System.Drawing.Image image = System.Drawing.Image.FromFile(originalImagePath);
        System.Drawing.Image thumbnail = image.GetThumbnailImage(thumbnailWidth, thumbnailWidth/2, ()=>false, IntPtr.Zero);
        thumbnail.Save(outputStream, System.Drawing.Imaging.ImageFormat.Jpeg);


        /*
        Bitmap sourceBitmap = new Bitmap(originalImagePath);
        float heightRatio = sourceBitmap.Height / sourceBitmap.Width;
        SizeF newSize = new SizeF(thumbnailWidth, thumbnailWidth * heightRatio);
        Bitmap targetBitmap = new Bitmap((int) newSize.Width,(int) newSize.Height);

        using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(targetBitmap))
        {
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            graphics.DrawImage(sourceBitmap, 0, 0, newSize.Width, newSize.Height);

            targetBitmap.Save(outputStream, System.Drawing.Imaging.ImageFormat.Jpeg);
        }
        */

        return outputStream.ToArray();
    }
}