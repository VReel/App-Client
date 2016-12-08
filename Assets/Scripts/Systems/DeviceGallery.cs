using UnityEngine;
using UnityEngine.Networking;        //UnityWebRequest
using System;                        //GC
using System.IO;                     //DirectoryInfo
//using System.Linq;                   //where, select
using System.Collections;            //IEnumerator
using System.Collections.Generic;    //List
using System.Threading;              //Threading

public class DeviceGallery : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private ImageSphereController m_imageSphereController;

    private int m_currGalleryPictureIndex = 0;         // Using the word "Picture" to represent images that are stored on the device
    private List<string> m_pictureFilePaths;
    private CoroutineQueue m_coroutineQueue;

    AndroidJavaClass m_galleryJavaClass;

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        AndroidJNI.AttachCurrentThread();
        m_galleryJavaClass = new AndroidJavaClass("io.vreel.vreel.VReelAndroidGallery");

        m_pictureFilePaths = new List<string>();
        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();
    }

    public void InvalidateGalleryPictureLoading() // This function is called in order to stop any ongoing picture loading 
    {
        m_currGalleryPictureIndex = -1;
    }

    public bool IsGalleryIndexAtStart()
    {
        return m_currGalleryPictureIndex <= 0;
    }

    public bool IsGalleryIndexAtEnd()
    {
        int numImageSpheres = m_imageSphereController.GetNumSpheres();
        int numFiles = m_pictureFilePaths.Count;
        return m_currGalleryPictureIndex >= (numFiles - numImageSpheres);       
    }

    public void OpenAndroidGallery()
    {
        Debug.Log("------- VREEL: OpenAndroidGallery() called");

        m_currGalleryPictureIndex = 0;
        m_pictureFilePaths.Clear();

        Debug.Log("------- VREEL: About to call GetImagesPath function...");
        string imagesTopLevelDirectory = m_galleryJavaClass.CallStatic<string>("GetAndroidImagesPath"); //string path = "/storage/emulated/0/DCIM/Gear 360/";
        Debug.Log("------- VREEL: Storing all FilePaths from directory: " + imagesTopLevelDirectory);

        m_coroutineQueue.EnqueueAction(StoreAllImageFilePaths(imagesTopLevelDirectory));

        m_currGalleryPictureIndex = 0;
        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        m_coroutineQueue.EnqueueAction(LoadPictures(m_currGalleryPictureIndex, numImagesToLoad));
    }

    public void NextPictures()
    {
        Debug.Log("------- VREEL: NextPictures() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_pictureFilePaths.Count;

        m_currGalleryPictureIndex = Mathf.Clamp(m_currGalleryPictureIndex + numImagesToLoad, 0, numFilePaths);

        m_coroutineQueue.Clear(); // Throw away previous operations
        m_coroutineQueue.EnqueueAction(LoadPictures(m_currGalleryPictureIndex, numImagesToLoad));
    }

    public void PreviousPictures()
    {
        Debug.Log("------- VREEL: PreviousPictures() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_pictureFilePaths.Count;

        m_currGalleryPictureIndex = Mathf.Clamp(m_currGalleryPictureIndex - numImagesToLoad, 0, numFilePaths);

        m_coroutineQueue.Clear(); // Throw away previous operations
        m_coroutineQueue.EnqueueAction(LoadPictures(m_currGalleryPictureIndex, numImagesToLoad));
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator StoreAllImageFilePaths(string imagesTopLevelDirectory)
    {        
        // TODO: Make sure this whole function doesn't block at all - 
        //       Steps 1 and 2 can sort of be merged to reduce iterations per frame

        // 1) Find all files that could potentially be 360 images.
        var files = GetAllFileNamesRecursively(imagesTopLevelDirectory);
        yield return new WaitForEndOfFrame();

        // 2) Add the files that are actually 360 images to "m_pictureFilePaths"
        // In order to reduce the block on the main thread we only search over kNumFilesPerIteration
        const int kNumFilesPerIteration = 100;
        int numFilesSearched = 0;
        foreach (string filePath in files)
        { 
            if (Is360Image(filePath))
            {   
                m_pictureFilePaths.Add(filePath);
            }

            numFilesSearched++;
            if (numFilesSearched % kNumFilesPerIteration == 0)
            {
                GC.Collect();
                yield return new WaitForEndOfFrame();
            }
        }
            
        Debug.Log("------- VREEL: Searched the directory " + imagesTopLevelDirectory + ", through " + numFilesSearched +
            " files, and found " + m_pictureFilePaths.Count + " 360-image files!");

        // 3) Sort files in order of newest first, so that users see their most recent gallery images!
        yield return new WaitForEndOfFrame();
        m_pictureFilePaths.Sort(delegate(string file1, string file2)
        {
            return File.GetCreationTime(file2).CompareTo(File.GetCreationTime(file1));
        });
    }

    private List<string> GetAllFileNamesRecursively(string baseDirectory)
    {
        // We iterate over all files in the given top level directory, recursively searching through all the subdirectories
        var files = new List<string>();
        FileAttributes undesiredAttributes = (FileAttributes.Hidden | FileAttributes.System | FileAttributes.Temporary);

        foreach(string filePath in System.IO.Directory.GetFiles(baseDirectory, "*", SearchOption.TopDirectoryOnly))
        {            
            FileAttributes checkedFileAttributes = File.GetAttributes(filePath) & undesiredAttributes;
            if (checkedFileAttributes == 0)
            {
                files.Add(filePath);
            }
        }

        foreach(string dirName in System.IO.Directory.GetDirectories(baseDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            FileAttributes checkedFolderAttributes = (new DirectoryInfo(dirName).Attributes) & undesiredAttributes;
            if (checkedFolderAttributes == 0)
            {
                files.AddRange(GetAllFileNamesRecursively(dirName));
            }
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
        float aspectRatio = m_galleryJavaClass.CallStatic<float>("CalcAspectRatio", filePath);
        Debug.Log("------- VREEL: Aspect Ratio: " + aspectRatio);

        const float kDesiredAspectRatio = 2.0f;
        bool isImage360 = Mathf.Abs(aspectRatio - kDesiredAspectRatio) < Mathf.Epsilon;
        Debug.Log("------- VREEL: Image: " + filePath + " is 360: " + isImage360);

        return isImage360;
    }

    private IEnumerator LoadPictures(int startingPictureIndex, int numImagesToLoad)
    {
        Debug.Log(string.Format("------- VREEL: Loading {0} pictures beginning at index {1}. There are {2} pictures in the gallery!", 
            numImagesToLoad, startingPictureIndex, m_pictureFilePaths.Count));

        Resources.UnloadUnusedAssets();

        int currPictureIndex = startingPictureIndex;
        for (int sphereIndex = 0; sphereIndex < numImagesToLoad; sphereIndex++, currPictureIndex++)
        {
            if (currPictureIndex < m_pictureFilePaths.Count)
            {                   
                string filePath = m_pictureFilePaths[currPictureIndex];
                m_coroutineQueue.EnqueueAction(LoadPicturesInternal(filePath, sphereIndex, currPictureIndex, numImagesToLoad));
                m_coroutineQueue.EnqueueWait(2.0f);
            }
            else
            {
                m_imageSphereController.HideSphereAtIndex(sphereIndex);
            }
        }

        Resources.UnloadUnusedAssets();
        yield return null;
    }

    private IEnumerator LoadPicturesInternal(string filePath, int sphereIndex, int pictureIndex, int numImages)
    {                           
        bool pictureRequestStillValid = 
            (m_currGalleryPictureIndex != -1) && 
            (m_currGalleryPictureIndex <= pictureIndex) &&  
            (pictureIndex < m_currGalleryPictureIndex + numImages); // Request no longer valid as user has moved on from this page
        
        string logString02 = string.Format("------- VREEL: Checking validity returned '{0}' when checking that {1} <= {2} < {1}+{3}", pictureRequestStillValid, m_currGalleryPictureIndex, pictureIndex, numImages); 
        Debug.Log(logString02);
        if (!pictureRequestStillValid)
        {
            Debug.Log("------- VREEL: Gallery request stopped because user has moved off that page: " + filePath);
            yield break;
        }

        Debug.Log("------- VREEL: Loading from filePath: " + filePath);

        //private System.Threading.Thread m_thread;

        // NOTE: When I keep calling www.texture it will crash due to multiple allocations!

        WWW www = new WWW("file://" + filePath);
        //string path = "/storage/emulated/0/DCIM/Gear 360/SAM_100_0093.jpg";
        //WWW www = new WWW("file://" + path);
        //string url = "http://lookingglass.services/wp-content/uploads/2016/06/360panorama.jpg"; // 950KB
        //string url = "https://upload.wikimedia.org/wikipedia/commons/6/6f/Helvellyn_Striding_Edge_360_Panorama,_Lake_District_-_June_09.jpg"; // 9.3MB
        //WWW www = new WWW(url);
        yield return www;

        //UnityWebRequest uwr = new UnityWebRequest(url);
        /*
        UnityWebRequest uwr = new UnityWebRequest("file://" + filePath);
        DownloadHandlerTexture textureDownloadHandler = new DownloadHandlerTexture(true);
        uwr.downloadHandler = textureDownloadHandler;
        yield return uwr.Send();
        if(uwr.isError) 
        {
            Debug.Log("------- VREEL: Error on loading texture");
            yield break;
        }

        Texture2D t = textureDownloadHandler.texture;
        Debug.Log("------- VREEL: Downloaded texture of size height x width: " + t.height + " x " + t.width);
        */

        //ResourceRequest request = Resources.LoadAsync(filePath);
        //yield return request;
        //m_imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(request.asset as Texture2D, filePath);

        //byte[] fileByteData = File.ReadAllBytes(filePath); // make sure to have Write Access: External (SDCard)
        //Texture2D texture = new Texture2D(2, 2);
        //texture.LoadImage(fileByteData);

        //coroutineQueue.EnqueueAction(LoadPicturesInternal2(www.texture, filePath, sphereIndex));
        //coroutineQueue.EnqueueWait(2.0f);

        //Debug.Log("------- VREEL: Loaded data into texture");

        // TODO: Make the copying of www.texture into this function call not block! 
        //Debug.Log("------- VREEL: Calling SetImageAndFilePath which will block on copying texture of size height x width: " + www.texture.height + " x " + www.texture.width);
        //m_imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(ref www, filePath);
        m_imageSphereController.SetImageAndFilePathAtIndex(sphereIndex, www.texture, filePath);

        /*
        SelectImage currImageSphere = m_imageSpheres[sphereIndex].GetComponent<SelectImage>();

        System.Threading.Thread tempThread = new Thread(() => 
            currImageSphere.SetImageAndFilePath(www.texture, filePath));

        tempThread.Start();
        yield return tempThread.Join();
        */

        //Debug.Log("------- VREEL: Set texture on ImageSphere");

        Resources.UnloadUnusedAssets();
    }

    private IEnumerator LoadPicturesInternal2(Texture2D source, string filePath, int sphereIndex)
    {
        int textureWidth = source.width;
        int textureHeight = source.height;

        Debug.Log("------- VREEL: Downloaded texture is being copied, Width x Height= " 
            + textureWidth + " x " + textureHeight + " ; Size in pixels = " 
            + textureWidth * textureHeight );

        Texture2D myTexture = new Texture2D(textureWidth, textureHeight, source.format, false);
        yield return myTexture;

        const int kNumIterationsPerFrame = 400000;
        int iterationCounter = 0;
        Color tempSourceColor = Color.black;

        Debug.Log("------- VREEL: Entering LoadPicturesInternal2 loop");
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {                
                //Debug.Log("------- VREEL: GetPixel(" + x + "," + y + ")");
                tempSourceColor = source.GetPixel(x, y);

                //Debug.Log("------- VREEL: SetPixel(" + x + "," + y + ")");
                myTexture.SetPixel(x, y, tempSourceColor);

                iterationCounter++;

                if (iterationCounter % kNumIterationsPerFrame == 0)
                {
                    Debug.Log("------- VREEL: Yielding LoadPicturesInternal2 at Iteration number: " 
                        + iterationCounter + " Pixel: (" + x + "," + y + ")");
                    yield return new WaitForEndOfFrame(); 
                }
            }
        }

        //Apply changes to the Texture
        myTexture.Apply();

        m_imageSphereController.SetImageAndFilePathAtIndex(sphereIndex, myTexture, filePath);

        Resources.UnloadUnusedAssets();
    }
}