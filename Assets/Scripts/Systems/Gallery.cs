using UnityEngine;
using UnityEngine.UI;               //Text
using UnityEngine.Networking;       //UnityWebRequest
using System;                       //GC
using System.IO;                    //DirectoryInfo
using System.Collections;           //IEnumerator
using System.Collections.Generic;   //List
using System.Threading;             //Threading

using UnityEngine.Serialization;

public class Gallery : MonoBehaviour 
{    
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private User m_user;
    [SerializeField] private Profile m_profile;
    [SerializeField] private LoginFlow m_loginFlow;
    [SerializeField] private ImageFlow m_imageFlow;
    [SerializeField] private ImageLoader m_imageLoader;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private LoadingIcon m_loadingIcon;
    [SerializeField] private KeyBoard m_keyboard;
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
        
    public void UploadImage()
    {     
        m_keyboard.AcceptText();
        m_coroutineQueue.EnqueueAction(UploadImageInternal(m_imageSkybox.GetImageIdentifier()));
    }

    public void UploadProfileImage()
    {        
        m_keyboard.AcceptText();
        m_coroutineQueue.EnqueueAction(UploadImageInternal(m_imageSkybox.GetImageIdentifier(), true));
    }

    public void OpenAndroidGallery()
    {
        m_coroutineQueue.EnqueueAction(OpenAndroidGalleryInternal());
    }

    public void LoadImageOriginal(string filePath)
    {
        LoadImageInternalPlugin(filePath, Helper.kSkyboxSphereIndex, true, Helper.kMaxImageWidth);
    }

    public void NextImages()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: NextImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_galleryImageFilePaths.Count;

        m_currGalleryImageIndex = Mathf.Clamp(m_currGalleryImageIndex + numImagesToLoad, 0, numFilePaths);

        m_imageLoader.InvalidateLoading();
        m_coroutineQueue.Clear(); // Throw away previous operations
        m_coroutineQueue.EnqueueAction(LoadImageThumbnails(m_currGalleryImageIndex, numImagesToLoad));
    }

    public void PreviousImages()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreviousImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_galleryImageFilePaths.Count;

        m_currGalleryImageIndex = Mathf.Clamp(m_currGalleryImageIndex - numImagesToLoad, 0, numFilePaths);

        m_imageLoader.InvalidateLoading();
        m_coroutineQueue.Clear(); // Throw away previous operations
        m_coroutineQueue.EnqueueAction(LoadImageThumbnails(m_currGalleryImageIndex, numImagesToLoad));
    }

    // **************************
    // Private/Helper functions
    // **************************

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

        yield return LoadImageThumbnails(m_currGalleryImageIndex, numImagesToLoad);
    }

    private IEnumerator UploadImageInternal(string filePath, bool profilePic = false) //NOTE: Ensured that this function cannot be stopped midway because the LoadingIcon blocks UI
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_loadingIcon.Display();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Running UploadImageInternal() for image with path: " + filePath);

        //0) Delete temporary file's if they somehow still exist...
        string originalImageFilePath = filePath;
        string tempThumbnailPath = m_imagesTopLevelDirectory + "/tempThumbnailImage.png";
        string tempOriginalPath = m_imagesTopLevelDirectory + "/tempOriginalImage.png";
        if (File.Exists(tempThumbnailPath))
        {
            File.Delete(tempThumbnailPath);
        }

        if (File.Exists(tempOriginalPath))
        {
            File.Delete(tempOriginalPath);
        }

        // 1) Get PresignedURL
        yield return m_backEndAPI.S3_PresignedURL();

        // 2) Create Thumbnail from Original image
        bool successfullyCreatedThumbnail = false;
        bool successfullyCreatedMaxResolutionImage = false;
        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            bool isDebugBuild = Debug.isDebugBuild;

            m_threadJob.Start( () => 
                successfullyCreatedThumbnail = 
                    CreateSmallerImageWithResolution(originalImageFilePath, tempThumbnailPath, Helper.kThumbnailWidth, isDebugBuild)
            );
            yield return m_threadJob.WaitFor();    

            m_threadJob.Start( () => 
                successfullyCreatedMaxResolutionImage = 
                    CreateSmallerImageWithResolution(originalImageFilePath, tempOriginalPath, Helper.kMaxImageWidth, isDebugBuild)
            );
            yield return m_threadJob.WaitFor(); 
        }

        // 3) Upload Original
        if (m_backEndAPI.IsLastAPICallSuccessful() && successfullyCreatedThumbnail && successfullyCreatedMaxResolutionImage)
        {
            yield return m_backEndAPI.UploadObject(
                m_backEndAPI.GetS3PresignedURLResult().data.attributes.original.url.ToString(),
                originalImageFilePath
            );
        }

        // 4) Upload Thumbnail
        if (m_backEndAPI.IsLastAPICallSuccessful() && successfullyCreatedThumbnail && successfullyCreatedMaxResolutionImage)
        {
            yield return m_backEndAPI.UploadObject(
                m_backEndAPI.GetS3PresignedURLResult().data.attributes.thumbnail.url.ToString(),
                tempThumbnailPath
            );
        }

        // 5) Register Post as Created
        if (m_backEndAPI.IsLastAPICallSuccessful() && successfullyCreatedThumbnail && successfullyCreatedMaxResolutionImage)
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

        if (successfullyCreatedMaxResolutionImage)
        {
            File.Delete(tempOriginalPath);
        }

        yield return null;

        // 7) If there has been a successful upload -> Inform user that image has been uploaded      
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Uploaded image: " + originalImageFilePath + ", with Success: " + (m_backEndAPI.IsLastAPICallSuccessful() && successfullyCreatedThumbnail && successfullyCreatedMaxResolutionImage) );
        if (m_backEndAPI.IsLastAPICallSuccessful())
        {
            //TODO: SHOW A SUCCESS MESSAGE!
        }

        m_imageFlow.Close();
        m_loadingIcon.Hide();

        if (profilePic)
        {
            m_profile.SetMenuBarProfileDetails();
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

            var errorText = m_user.GetErrorMessage().GetComponentInChildren<Text>();
            errorText.text = "We've run into an error looking for your 360-images, check you've given us the correct permissions and try again! =)";
            m_user.GetErrorMessage().SetActive(true);
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

            var errorText = m_user.GetErrorMessage().GetComponentInChildren<Text>();
            errorText.text = "We've run into an error looking for your 360-images, check you've given us the correct permissions and try again! =)";
            m_user.GetErrorMessage().SetActive(true);
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

    private IEnumerator LoadImageThumbnails(int startingGalleryImageIndex, int numImagesToLoad)
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
                    LoadImageInternalPlugin(filePath, sphereIndex, showLoading, Helper.kThumbnailWidth);
                    m_imageSphereController.SetMetadataToEmptyAtIndex(sphereIndex);
                }
            }
            else
            {
                m_imageSphereController.HideSphereAtIndex(sphereIndex);
            }
        }

    }

    private void LoadImageInternalPlugin(string filePath, int sphereIndex, bool showLoading, int maxImageWidth)
    {   
        m_imageLoader.LoadImageFromPathIntoImageSphere(m_imageSphereController, sphereIndex, filePath, showLoading, maxImageWidth);
    }        

    private bool CreateSmallerImageWithResolution(string originalImagePath, string newImagePath, int newResolutionWidth, bool debugOn)
    {
        if (debugOn) Debug.Log("------- VREEL: Called CreateSmallerImageWithResolution() with new Image Path: " + newImagePath);
        AndroidJNI.AttachCurrentThread();

        bool success = false;

        float currentWidth = m_javaPluginClass.CallStatic<float>("CalcWidth", originalImagePath);
        if (currentWidth > newResolutionWidth)
        {
            success = m_javaPluginClass.CallStatic<bool>("CreateSmallerImageWithResolution", originalImagePath, newImagePath, newResolutionWidth);
        }
        else
        {            
            File.Copy(originalImagePath, newImagePath);
            success = true;
        }

        AndroidJNI.DetachCurrentThread();
        if (debugOn) Debug.Log("------- VREEL: Call CreateSmallerImageWithResolution() returned with: " + success);

        return success;
    }        
}