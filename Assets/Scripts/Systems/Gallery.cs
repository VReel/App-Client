using UnityEngine;
using UnityEngine.UI;               //Text
using UnityEngine.Networking;       //UnityWebRequest
using System;                       //GC
using System.IO;                    //DirectoryInfo
using System.Collections;           //IEnumerator
using System.Collections.Generic;   //List
using System.Threading;             //Threading

//using System.Drawing;

public class Gallery : MonoBehaviour 
{    
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private ProfileDetails m_profileDetails;
    [SerializeField] private ImageLoader m_imageLoader;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private LoadingIcon m_loadingIcon;
    [SerializeField] private GameObject m_uploadButton;
    [SerializeField] private GameObject m_noGalleryImagesText;
    [SerializeField] private GameObject m_captionNewText;
    [SerializeField] private GameObject m_uploadConfirmation;

    private string m_imagesTopLevelDirectory;
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

        m_backEndAPI = new BackEndAPI(this, m_user.GetErrorMessage(), m_user);

        m_uploadConfirmation.SetActive(false);
    }

    public void InvalidateWork() // This function is called in order to stop any ongoing work
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

    public void PreUpload()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreUpload() called on post: " + m_imageSkybox.GetImageIdentifier());

        m_uploadConfirmation.SetActive(true);
        m_uploadButton.SetActive(false);
        m_menuController.SetImagesAndMenuBarActive(false);
        m_appDirector.SetOverlayShowing(true);
    }

    public void CancelUpload()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: CancelUpload() called");

        m_uploadConfirmation.SetActive(false);
        m_uploadButton.SetActive(true);
        m_menuController.SetImagesAndMenuBarActive(true);
        m_appDirector.SetOverlayShowing(false);
    }
        
    public void UploadImage()
    {     
        m_coroutineQueue.EnqueueAction(UploadImageInternal(m_imageSkybox.GetImageIdentifier()));
    }

    public void UploadProfileImage()
    {        
        m_coroutineQueue.EnqueueAction(UploadImageInternal(m_imageSkybox.GetImageIdentifier(), true));
    }

    public void OpenAndroidGallery()
    {
        m_coroutineQueue.EnqueueAction(OpenAndroidGalleryInternal());
    }

    private IEnumerator OpenAndroidGalleryInternal()
    {        
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenAndroidGallery() called");

        m_currGalleryImageIndex = 0;
        m_galleryImageFilePaths.Clear();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: About to call GetImagesPath function...");
        m_imagesTopLevelDirectory = m_javaPluginClass.CallStatic<string>("GetAndroidImagesPath"); //string path = "/storage/emulated/0/DCIM/Gear 360/";

        m_imageSphereController.SetTopLevelDirectory(m_imagesTopLevelDirectory);
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Storing all FilePaths from directory: " + m_imagesTopLevelDirectory);

        yield return StoreAllImageGalleryFilePaths(m_imagesTopLevelDirectory);

        m_currGalleryImageIndex = 0;
        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        m_imageSphereController.SetAllImageSpheresToLoading();

        yield return LoadImages(m_currGalleryImageIndex, numImagesToLoad);
    }

    public void NextImages()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: NextImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_galleryImageFilePaths.Count;

        m_currGalleryImageIndex = Mathf.Clamp(m_currGalleryImageIndex + numImagesToLoad, 0, numFilePaths);

        m_imageLoader.InvalidateLoading();
        m_coroutineQueue.Clear(); // Throw away previous operations
        m_coroutineQueue.EnqueueAction(LoadImages(m_currGalleryImageIndex, numImagesToLoad));
    }

    public void PreviousImages()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreviousImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_galleryImageFilePaths.Count;

        m_currGalleryImageIndex = Mathf.Clamp(m_currGalleryImageIndex - numImagesToLoad, 0, numFilePaths);

        m_imageLoader.InvalidateLoading();
        m_coroutineQueue.Clear(); // Throw away previous operations
        m_coroutineQueue.EnqueueAction(LoadImages(m_currGalleryImageIndex, numImagesToLoad));
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator UploadImageInternal(string filePath, bool profilePic = false) //NOTE: Ensured that this function cannot be stopped midway because the LoadingIcon blocks UI
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_loadingIcon.Display();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Running UploadImageInternal() for image with path: " + filePath);

        // 1) Get PresignedURL
        yield return m_backEndAPI.S3_PresignedURL();

        // 2) Create Thumbnail from Original image
        string originalImageFilePath = filePath;
        string tempThumbnailPath = m_imagesTopLevelDirectory + "/tempThumbnailImage.png";
        bool successfullyCreatedThumbnail = false;
        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            bool isDebugBuild = Debug.isDebugBuild;
            m_threadJob.Start( () => 
                successfullyCreatedThumbnail = CreateThumbnail(originalImageFilePath, tempThumbnailPath, isDebugBuild)
            );
            yield return m_threadJob.WaitFor();       
        }

        // 3) Upload Original
        if (m_backEndAPI.IsLastAPICallSuccessful() && successfullyCreatedThumbnail)
        {
            yield return m_backEndAPI.UploadObject(
                m_backEndAPI.GetS3PresignedURLResult().data.attributes.original.url.ToString(),
                originalImageFilePath
            );
        }

        // 4) Upload Thumbnail
        if (m_backEndAPI.IsLastAPICallSuccessful() && successfullyCreatedThumbnail)
        {
            yield return m_backEndAPI.UploadObject(
                m_backEndAPI.GetS3PresignedURLResult().data.attributes.thumbnail.url.ToString(),
                tempThumbnailPath
            );
        }

        // 5) Register Post as Created
        if (m_backEndAPI.IsLastAPICallSuccessful() && successfullyCreatedThumbnail)
        {            
            string captionText = m_captionNewText.GetComponentInChildren<Text>().text;
            Helper.TruncateString(ref captionText, Helper.kMaxCaptionOrDescriptionLength);

            if (profilePic)
            {
                yield return m_backEndAPI.Register_UpdateProfileImage(
                    m_backEndAPI.GetS3PresignedURLResult().data.attributes.thumbnail.key.ToString(), 
                    m_backEndAPI.GetS3PresignedURLResult().data.attributes.original.key.ToString()
                );                    
            }
            else 
            {
                yield return m_backEndAPI.Post_CreatePost(
                    m_backEndAPI.GetS3PresignedURLResult().data.attributes.thumbnail.key.ToString(), 
                    m_backEndAPI.GetS3PresignedURLResult().data.attributes.original.key.ToString(),
                    captionText
                );
            }
        }

        //6) Delete temporary thumbnail file
        if (successfullyCreatedThumbnail)
        {
            File.Delete(tempThumbnailPath);
        }
        yield return null;

        // 7) If there has been a successful upload -> Inform user that image has been uploaded      
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Uploaded image: " + originalImageFilePath + ", with Success: " + (m_backEndAPI.IsLastAPICallSuccessful() && successfullyCreatedThumbnail) );
        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            //TODO: SHOW A SUCCESS MESSAGE!
        }
        else
        {   
            //TODO: SHOW A FAILURE MESSAGE! - I Think this will happen naturally...
        }

        m_uploadConfirmation.SetActive(false);
        m_loadingIcon.Hide();
        m_menuController.SetImagesAndMenuBarActive(true);

        if (profilePic)
        {
            m_profileDetails.SetMenuBarProfileDetails();
        }
    }

    // TODO: Only download 10 images at a time!
    private IEnumerator StoreAllImageGalleryFilePaths(string imagesTopLevelDirectory)
    {  
        m_loadingIcon.Display();

        // 1) Find all files that could potentially be 360 images.
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling GetAllFileNamesRecursively()");
        List<string> files = new List<string>();
        bool isDebugBuild = Debug.isDebugBuild;
        m_threadJob.Start( () => 
            files = GetAllFileNamesRecursively(imagesTopLevelDirectory, isDebugBuild)
        );
        yield return m_threadJob.WaitFor();

        // 2) Add the files that are actually 360 images to "m_galleryImageFilePaths"
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling FilterAndAdd360Images()");
        int numFilesSearched = 0;
        m_threadJob.Start( () => 
            numFilesSearched = FilterAndAdd360Images(files, isDebugBuild)           
        );
        yield return m_threadJob.WaitFor();
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Searched the directory " + imagesTopLevelDirectory + ", through " + numFilesSearched +
            " files, and found " + m_galleryImageFilePaths.Count + " 360-image files!");

        // 3) Sort files in order of newest first, so that users see their most recent gallery images!
        bool ranSuccessfully = false;
        m_threadJob.Start( () => 
            ranSuccessfully = SortGalleryImageFilePaths()           
        );
        yield return m_threadJob.WaitFor();

        m_loadingIcon.Hide();

        bool noImagesInGallery = m_galleryImageFilePaths.Count <= 0;
        m_noGalleryImagesText.SetActive(noImagesInGallery); // If the user has yet take any 360-images then show them the NoGalleryImagesText!
    }
    
    private List<string> GetAllFileNamesRecursively(string baseDirectory, bool isDebugBuild)
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

            // TODO: REPORT FAILURE IN GALLERY
        }

        try
        {
            foreach(string dirName in System.IO.Directory.GetDirectories(baseDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                FileAttributes checkedFolderAttributes = (new DirectoryInfo(dirName).Attributes) & undesiredAttributes;
                if (checkedFolderAttributes == 0)
                {
                    files.AddRange(GetAllFileNamesRecursively(dirName, isDebugBuild));
                }
            }
        }
        catch 
        {
            if (isDebugBuild) Debug.Log("------- VREEL: Call to GetDirectories() failed for: " + baseDirectory);

            // TODO: REPORT FAILURE IN GALLERY
        }

        return files;
    }

    private int FilterAndAdd360Images(List<string> files, bool isDebugBuild)
    {
        AndroidJNI.AttachCurrentThread();

        int numFilesSearched = 0;
        foreach (string filePath in files)
        { 
            if (Is360Image(filePath, isDebugBuild))
            {   
                m_galleryImageFilePaths.Add(filePath);
            }
            numFilesSearched++;
        }

        AndroidJNI.DetachCurrentThread();

        return numFilesSearched;
    }
        
    private static readonly List<string> s_imageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
    private bool Is360Image(string filePath, bool isDebugBuild)
    {            
        // Check that it has an image file extension 
        if (!s_imageExtensions.Contains(Path.GetExtension(filePath).ToUpperInvariant()))
        {
            if (isDebugBuild) Debug.Log("------- VREEL: File: " + filePath + " is not an image!");
            return false;
        }

        // NOTE: My current rudimentatry implementation of this function is very simply to check if the aspect ratio is 2:1!
        // This works the majority of the time because all 360 images have a 2:1 ratio,
        // and its not a standard aspect ratio for any other type of image!

        if (isDebugBuild) Debug.Log("------- VREEL: About to call Aspect Ratio function for file: " + filePath);
        float aspectRatio = m_javaPluginClass.CallStatic<float>("CalcAspectRatio", filePath);
        if (isDebugBuild) Debug.Log("------- VREEL: Aspect Ratio: " + aspectRatio);

        const float kDesiredAspectRatio = 2.0f;
        bool isImage360 = Mathf.Abs(aspectRatio - kDesiredAspectRatio) < Mathf.Epsilon;
        if (isDebugBuild) Debug.Log("------- VREEL: Image: " + filePath + " is 360: " + isImage360);

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

        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Checking validity returned '{0}' when checking that {1} <= {2} < {1}+{3}", imageRequestStillValid, startingGalleryImageIndex, m_currGalleryImageIndex, numImagesToLoad));
        if (!imageRequestStillValid)
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: LoadImages() with index = " + startingGalleryImageIndex + " has failed");
            yield break;
        }
            
        int currGalleryImageIndex = startingGalleryImageIndex;
        for (int sphereIndex = 0; sphereIndex < numImagesToLoad; sphereIndex++, currGalleryImageIndex++)
        {
            yield return null; // Space out the work that's going on... 
            
            if (currGalleryImageIndex < m_galleryImageFilePaths.Count)
            {                      
                string filePath = m_galleryImageFilePaths[currGalleryImageIndex];

                bool identifierValid = filePath.CompareTo(m_imageSphereController.GetIdentifierAtIndex(sphereIndex)) != 0; // If file-path is the same then ignore request
                if (Debug.isDebugBuild) Debug.Log("------- VREEL: Checking that filePath has changed has returned = " + identifierValid);
                if (identifierValid)
                {
                    bool showLoading = sphereIndex == 0; // The first one in the gallery should do some loading to let the user know things are happening
                    m_coroutineQueue.EnqueueAction(LoadImageInternalPlugin(filePath, sphereIndex, showLoading));
                    m_imageSphereController.SetMetadataToEmptyAtIndex(sphereIndex);
                }
            }
            else
            {
                m_imageSphereController.HideSphereAtIndex(sphereIndex);
            }
        }

    }

    private IEnumerator LoadImageInternalPlugin(string filePath, int sphereIndex,bool showLoading)
    {   
        m_imageLoader.LoadImageFromPathIntoImageSphere(m_imageSphereController, sphereIndex, filePath, showLoading);
        yield break;
    }

    /*
    private IEnumerator LoadImageInternalUnity(string filePath, int sphereIndex)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadPicturesInternalUnity() from filePath: " + filePath);

        WWW www = new WWW("file://" + filePath);
        yield return www;

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling LoadImageIntoTexture()");
        Texture2D myNewTexture2D = new Texture2D(2,2);
        www.LoadImageIntoTexture(myNewTexture2D);
        yield return null;
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished LoadImageIntoTexture()");

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Calling SetImageAndFilePath()");
        m_imageSphereController.SetImageAtIndex(sphereIndex, myNewTexture2D, filePath, -1, true);
        yield return null;
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished SetImageAndFilePath()");

        Resources.UnloadUnusedAssets();
    }
    */

    private bool CreateThumbnail(string originalImagePath, string thumbnailImagePath, bool debugOn)
    {
        if (debugOn) Debug.Log("------- VREEL: Called CreateThumbnail() with Thumbnail Path: " + thumbnailImagePath);
        AndroidJNI.AttachCurrentThread();

        bool success = m_javaPluginClass.CallStatic<bool>("CreateThumbnail", originalImagePath, thumbnailImagePath, Helper.kStandardThumbnailWidth);

        AndroidJNI.DetachCurrentThread();
        if (debugOn) Debug.Log("------- VREEL: Call CreateThumbnail() returned with: " + success);

        return success;
    }        
}