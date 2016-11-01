using UnityEngine;
using System.IO;                     //DirectoryInfo
using System.Collections;            //IEnumerator
using System.Collections.Generic;    //List
using System.Threading;              //Threading

//TODO: Get Asynchronous loading of Images to work somehow!

public class DeviceGallery : MonoBehaviour 
{
    public GameObject[] m_imageSpheres;

    private int m_currPictureIndex = 0;         // Using the word "Picture" to represent images that are stored on the device
    private List<string> m_pictureFilePaths;
    private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
    //private System.Threading.Thread m_thread;

    public void Start()
    {
        m_pictureFilePaths = new List<string>();
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

        StartCoroutine(LoadPictures(m_currPictureIndex, numImageSpheres));
        //m_thread = new Thread(() => LoadPictures(m_currPictureIndex, numImageSpheres));
        //m_thread.Start();
    }

    public void NextPictures()
    {
        Debug.Log("------- VREEL: NextPictures() called");

        int numImageSpheres = m_imageSpheres.GetLength(0);
        int numFilePaths = m_pictureFilePaths.Count;

        m_currPictureIndex = Mathf.Clamp(m_currPictureIndex + numImageSpheres, 0, numFilePaths);

        StartCoroutine(LoadPictures(m_currPictureIndex, numImageSpheres));       
    }

    public void PreviousPictures()
    {
        Debug.Log("------- VREEL: PreviousPictures() called");

        int numImageSpheres = m_imageSpheres.GetLength(0);
        int numFilePaths = m_pictureFilePaths.Count;

        m_currPictureIndex = Mathf.Clamp(m_currPictureIndex - numImageSpheres, 0, numFilePaths);

        StartCoroutine(LoadPictures(m_currPictureIndex, numImageSpheres));
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
    }

    private IEnumerator LoadPictures(int startingPictureIndex, int numImages)
    {
        Debug.Log(string.Format("------- VREEL: Loading {0} pictures beginning at index {1}. There are {2} pictures in the gallery!", 
            numImages, startingPictureIndex, m_pictureFilePaths.Count));

        int currPictureIndex = startingPictureIndex;
        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, currPictureIndex++)
        {
            if (currPictureIndex < m_pictureFilePaths.Count)
            {   
                Debug.Log("------- VREEL: Loop iteration: " + sphereIndex);
                string filePath = m_pictureFilePaths[currPictureIndex];
                StartCoroutine(LoadPicturesInternal(filePath, sphereIndex));
                Resources.UnloadUnusedAssets();
                yield return new WaitForSeconds(2.0f); // HACK to deal with the lack of asynchronous image loading...
            }
        }    
    }

    private IEnumerator LoadPicturesInternal(string filePath, int sphereIndex)
    {        
        Debug.Log("------- VREEL: Loading from filePath: " + filePath);

        WWW www = new WWW("file://" + filePath);
        yield return www;

        //byte[] fileByteData = File.ReadAllBytes(filePath); // make sure to have Write Access: External (SDCard)
        //Texture2D texture = new Texture2D(2, 2);
        //texture.LoadImage(fileByteData);

        Debug.Log("------- VREEL: Loaded data into texture");

        m_imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndPath(www.texture, filePath);

        Debug.Log("------- VREEL: Set texture on ImageSphere");
    }
}