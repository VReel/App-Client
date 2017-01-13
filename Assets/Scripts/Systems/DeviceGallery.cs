using UnityEngine;
using UnityEngine.UI;               //Text
using UnityEngine.Networking;       //UnityWebRequest
using System;                       //GC
using System.IO;                    //DirectoryInfo
using System.Collections;           //IEnumerator
using System.Collections.Generic;   //List
using System.Threading;             //Threading

public class DeviceGallery : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private UserLogin m_userLogin;
    [SerializeField] private GameObject m_noGalleryImagesText;
    [SerializeField] private GameObject m_galleryMessage;

    private int m_currGalleryImageIndex = 0;
    private List<string> m_galleryImageFilePaths;
    private CoroutineQueue m_coroutineQueue;
    private AndroidJavaClass m_javaPluginClass;
    private ThreadJob m_threadJob;

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

    public void OpenAndroidGallery()
    {        
        Debug.Log("------- VREEL: OpenAndroidGallery() called");

        m_currGalleryImageIndex = 0;
        m_galleryImageFilePaths.Clear();

        Debug.Log("------- VREEL: About to call GetImagesPath function...");
        string imagesTopLevelDirectory = m_javaPluginClass.CallStatic<string>("GetAndroidImagesPath"); //string path = "/storage/emulated/0/DCIM/Gear 360/";
        m_imageSkybox.SetTopLevelDirectory(imagesTopLevelDirectory);
        Debug.Log("------- VREEL: Storing all FilePaths from directory: " + imagesTopLevelDirectory);

        m_coroutineQueue.EnqueueAction(StoreAllImageGalleryFilePaths(imagesTopLevelDirectory));

        m_currGalleryImageIndex = 0;
        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        m_imageSphereController.SetAllImageSpheresToLoading();
        m_coroutineQueue.EnqueueAction(LoadImages(m_currGalleryImageIndex, numImagesToLoad));
    }

    public void NextImages()
    {
        Debug.Log("------- VREEL: NextImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_galleryImageFilePaths.Count;

        m_currGalleryImageIndex = Mathf.Clamp(m_currGalleryImageIndex + numImagesToLoad, 0, numFilePaths);

        m_coroutineQueue.Clear(); // Throw away previous operations
        m_coroutineQueue.EnqueueAction(LoadImages(m_currGalleryImageIndex, numImagesToLoad));
    }

    public void PreviousImages()
    {
        Debug.Log("------- VREEL: PreviousImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_galleryImageFilePaths.Count;

        m_currGalleryImageIndex = Mathf.Clamp(m_currGalleryImageIndex - numImagesToLoad, 0, numFilePaths);

        m_coroutineQueue.Clear(); // Throw away previous operations
        m_coroutineQueue.EnqueueAction(LoadImages(m_currGalleryImageIndex, numImagesToLoad));
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator StoreAllImageGalleryFilePaths(string imagesTopLevelDirectory)
    {                
        // 1) Find all files that could potentially be 360 images.
        Debug.Log("------- VREEL: Calling GetAllFileNamesRecursively()");
        List<string> files = new List<string>();
        m_threadJob.Start( () => 
            files = GetAllFileNamesRecursively(imagesTopLevelDirectory)
        );
        yield return m_threadJob.WaitFor();

        // TODO: Add CalcAspectRatio() to C++ Plugin, which should then allow me to thread this function out...
        //      the only reason its not currently threaded is because of the Android Plugin needing to be called...
        // 2) Add the files that are actually 360 images to "m_galleryImageFilePaths"
        Debug.Log("------- VREEL: Calling FilterAndAdd360Images()");
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

        Debug.Log("------- VREEL: Searched the directory " + imagesTopLevelDirectory + ", through " + numFilesSearched +
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
    
    private List<string> GetAllFileNamesRecursively(string baseDirectory)
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
            Debug.Log("------- VREEL: Call to GetFiles() failed for: " + baseDirectory);

            // Report Failure in Gallery
            Text galleryTextComponent = m_galleryMessage.GetComponentInChildren<Text>();
            if (galleryTextComponent != null)
            {
                galleryTextComponent.text = "Reading files Failed!\n Check permissions!";
                galleryTextComponent.color = Color.red;
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
                    files.AddRange(GetAllFileNamesRecursively(dirName));
                }
            }
        }
        catch 
        {
            Debug.Log("------- VREEL: Call to GetDirectories() failed for: " + baseDirectory);

            // Report Failure in Gallery
            Text galleryTextComponent = m_galleryMessage.GetComponentInChildren<Text>();
            if (galleryTextComponent != null)
            {
                galleryTextComponent.text = "Reading files Failed!\n Check permissions!";
                galleryTextComponent.color = Color.red;
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
            Debug.Log("------- VREEL: File: " + filePath + " is not an image!");
            return false;
        }

        // NOTE: My current rudimentatry implementation of this function is very simply to check if the aspect ratio is 2:1!
        // This works the majority of the time because all 360 images have a 2:1 ratio,
        // and its not a standard aspect ratio for any other type of image!

        Debug.Log("------- VREEL: About to call Aspect Ratio function for file: " + filePath);
        float aspectRatio = m_javaPluginClass.CallStatic<float>("CalcAspectRatio", filePath);
        Debug.Log("------- VREEL: Aspect Ratio: " + aspectRatio);

        const float kDesiredAspectRatio = 2.0f;
        bool isImage360 = Mathf.Abs(aspectRatio - kDesiredAspectRatio) < Mathf.Epsilon;
        Debug.Log("------- VREEL: Image: " + filePath + " is 360: " + isImage360);

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
        Debug.Log(string.Format("------- VREEL: Loading {0} images beginning at index {1}. There are {2} images in the gallery!", 
            numImagesToLoad, startingGalleryImageIndex, m_galleryImageFilePaths.Count));

        Resources.UnloadUnusedAssets();

        bool imageRequestStillValid = 
            (m_currGalleryImageIndex != -1) && 
            (m_currGalleryImageIndex <= startingGalleryImageIndex) &&  
            (startingGalleryImageIndex < m_currGalleryImageIndex + numImagesToLoad); // Request no longer valid as user has moved on from this page

        string logString = string.Format("------- VREEL: Checking validity returned '{0}' when checking that {1} <= {2} < {1}+{3}", imageRequestStillValid, startingGalleryImageIndex, m_currGalleryImageIndex, numImagesToLoad); 
        Debug.Log(logString);
        if (!imageRequestStillValid)
        {
            Debug.Log("------- VREEL: LoadImages() with index = " + startingGalleryImageIndex + " has failed");
            yield break;
        }
            
        int currGalleryImageIndex = startingGalleryImageIndex;
        for (int sphereIndex = 0; sphereIndex < numImagesToLoad; sphereIndex++, currGalleryImageIndex++)
        {
            if (currGalleryImageIndex < m_galleryImageFilePaths.Count)
            {                       
                string filePath = m_galleryImageFilePaths[currGalleryImageIndex];

                bool filePathStillValid = filePath.CompareTo(m_imageSkybox.GetImageFilePath()) != 0; // If file-path is the same then ignore request
                Debug.Log("------- VREEL: Checking that filePath has changed has returned = " + filePathStillValid);
                if (filePathStillValid)
                {
                    m_coroutineQueue.EnqueueAction(LoadImageInternalPlugin(filePath, sphereIndex));
                    m_coroutineQueue.EnqueueWait(2.0f);
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
        Debug.Log("------- VREEL: Calling LoadPicturesInternalUnity() from filePath: " + filePath);

        WWW www = new WWW("file://" + filePath);
        yield return www;

        Debug.Log("------- VREEL: Calling LoadImageIntoTexture()");
        Texture2D myNewTexture2D = new Texture2D(2,2);
        www.LoadImageIntoTexture(myNewTexture2D);
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: Finished LoadImageIntoTexture()");

        Debug.Log("------- VREEL: Calling SetImageAndFilePath()");
        m_imageSphereController.SetImageAndFilePathAtIndex(sphereIndex, myNewTexture2D, filePath, -1);
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: Finished SetImageAndFilePath()");

        Resources.UnloadUnusedAssets();
    }
}