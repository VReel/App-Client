using UnityEngine;
using System.IO;                     //DirectoryInfo
using System.Collections;            //IEnumerator
using System.Collections.Generic;    //List
using System.Threading;              //Threading

public class DeviceGallery : MonoBehaviour 
{
    public GameObject[] m_imageSpheres;

    private int m_currPictureIndex = 0;         // Using the word "Picture" to represent images that are stored on the device
    private List<string> m_pictureFilePaths;
    private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
    //private System.Threading.Thread m_thread;
    private CoroutineQueue coroutineQueue;

    public void Start()
    {
        m_pictureFilePaths = new List<string>();
        coroutineQueue = new CoroutineQueue( this );
        coroutineQueue.StartLoop();
    }

    public bool IsIndexAtStart()
    {
        return m_currPictureIndex == 0;
    }

    public bool IsIndexAtEnd()
    {
        int numImageSpheres = m_imageSpheres.GetLength(0);
        int numFiles = m_pictureFilePaths.Count;
        return m_currPictureIndex >= (numFiles - numImageSpheres);       
    }

    public void OpenAndroidGallery()
    {
        Debug.Log("------- VREEL: OpenAndroidGallery() called");

        // This is only Gear 360 images! I need to figure out how to find all 360 images regardless of where they live in the device
        m_currPictureIndex = 0;
        m_pictureFilePaths.Clear();
        string path = "/storage/emulated/0/DCIM/Gear 360/";

        Debug.Log("------- VREEL: Storing all FilePaths from directory: " + path);

        StoreAllFilePaths(path);

        int numImageSpheres = m_imageSpheres.GetLength(0);
        LoadPictures(m_currPictureIndex, numImageSpheres);
    }

    public void NextPictures()
    {
        Debug.Log("------- VREEL: NextPictures() called");

        int numImageSpheres = m_imageSpheres.GetLength(0);
        int numFilePaths = m_pictureFilePaths.Count;

        m_currPictureIndex = Mathf.Clamp(m_currPictureIndex + numImageSpheres, 0, numFilePaths);

        coroutineQueue.Clear(); // Ensure we stop loading somethign that we may be loading
        LoadPictures(m_currPictureIndex, numImageSpheres);
    }

    public void PreviousPictures()
    {
        Debug.Log("------- VREEL: PreviousPictures() called");

        int numImageSpheres = m_imageSpheres.GetLength(0);
        int numFilePaths = m_pictureFilePaths.Count;

        m_currPictureIndex = Mathf.Clamp(m_currPictureIndex - numImageSpheres, 0, numFilePaths);

        coroutineQueue.Clear(); // Ensure we stop loading something that we may be loading
        LoadPictures(m_currPictureIndex, numImageSpheres);
    }

    private void StoreAllFilePaths(string path)
    {
        foreach (string filePath in System.IO.Directory.GetFiles(path))
        { 
            if (ImageExtensions.Contains(Path.GetExtension(filePath).ToUpperInvariant())) // Check that the file is indeed an image
            {                
                m_pictureFilePaths.Add(filePath);
            }
        }

        m_pictureFilePaths.Reverse(); // Reversing to have the pictures appear in the order of newest first
    }

    private void LoadPictures(int startingPictureIndex, int numImages)
    {
        Debug.Log(string.Format("------- VREEL: Loading {0} pictures beginning at index {1}. There are {2} pictures in the gallery!", 
            numImages, startingPictureIndex, m_pictureFilePaths.Count));

        Resources.UnloadUnusedAssets();

        int currPictureIndex = startingPictureIndex;
        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, currPictureIndex++)
        {
            if (currPictureIndex < m_pictureFilePaths.Count)
            {   
                Debug.Log("------- VREEL: Loop iteration: " + sphereIndex);
                string filePath = m_pictureFilePaths[currPictureIndex];
                coroutineQueue.EnqueueAction(LoadPicturesInternal(filePath, sphereIndex));
                coroutineQueue.EnqueueWait(2.0f);
            }
            else
            {
                m_imageSpheres[sphereIndex].GetComponent<SelectImage>().Hide();
            }
        }

        Resources.UnloadUnusedAssets();
    }

    private IEnumerator LoadPicturesInternal(string filePath, int sphereIndex)
    {        
        Debug.Log("------- VREEL: Loading from filePath: " + filePath);

        WWW www = new WWW("file://" + filePath);
        yield return www;

        //ResourceRequest request = Resources.LoadAsync(filePath);
        //yield return request;
        //m_imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(request.asset as Texture2D, filePath);

        //byte[] fileByteData = File.ReadAllBytes(filePath); // make sure to have Write Access: External (SDCard)
        //Texture2D texture = new Texture2D(2, 2);
        //texture.LoadImage(fileByteData);

        Debug.Log("------- VREEL: Loaded data into texture");

        // TODO: Make the copying of www.texture into this function call not block! - below is coming to 77.6MB of TextureData! (Don't think this is correct)... 
        Debug.Log("------- VREEL: Calling SetImageAndFilePath which will block on copying texture of size " + www.texture.GetRawTextureData().Length);
        m_imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(www.texture, filePath);

        /*
        SelectImage currImageSphere = m_imageSpheres[sphereIndex].GetComponent<SelectImage>();

        System.Threading.Thread tempThread = new Thread(() => 
            currImageSphere.SetImageAndFilePath(www.texture, filePath));

        tempThread.Start();
        yield return tempThread.Join();
        */

        Debug.Log("------- VREEL: Set texture on ImageSphere");

        Resources.UnloadUnusedAssets();
    }
}