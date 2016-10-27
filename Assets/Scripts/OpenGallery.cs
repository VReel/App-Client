using UnityEngine;
using System.IO;                     //DirectoryInfo
using System.Collections.Generic;    //List

public class OpenGallery : MonoBehaviour 
{
    public GameObject[] m_imageSpheres;

    private int m_currPictureIndex = 0;         // Using the word "Picture" to represent images that are stored on the device
    private List<string> m_pictureFilePaths;

    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;

    public void Start()
    {
        m_pictureFilePaths = new List<string>();
    }

    public void OnMouseDown()
    {
        OpenAndroidGallery();
    }

    public void OpenAndroidGallery()
    {       
        // This is only Gear 360 images! I need to figure out how to find all 360 images regardless of where they live in the device
        m_currPictureIndex = 0;
        m_pictureFilePaths.Clear();
        string path = "/storage/emulated/0/DCIM/Gear 360/";

        Debug.Log("------- VREEL: Storing all FilePaths from directory: " + path);
        int index = 0;
        foreach (string filePath in System.IO.Directory.GetFiles(path))
        { 
            m_pictureFilePaths.Add(filePath);
            index++;
        }

        int numImageSpheres = m_imageSpheres.GetLength(0);
        LoadPictures(m_currPictureIndex, numImageSpheres);

        Debug.Log("------- VREEL: OpenAndroidGallery() called");
    }

    public void NextPictures()
    {
        int numImageSpheres = m_imageSpheres.GetLength(0);
        int numFilePaths = m_pictureFilePaths.Count;

        m_currPictureIndex = Mathf.Clamp(m_currPictureIndex + numImageSpheres, 0, numFilePaths);
        LoadPictures(m_currPictureIndex, numImageSpheres);

        Debug.Log("------- VREEL: NextPictures() called");
    }

    public void PreviousPictures()
    {
        int numImageSpheres = m_imageSpheres.GetLength(0);
        int numFilePaths = m_pictureFilePaths.Count;

        m_currPictureIndex = Mathf.Clamp(m_currPictureIndex - numImageSpheres, 0, numFilePaths);
        LoadPictures(m_currPictureIndex, numImageSpheres);

        Debug.Log("------- VREEL: PreviousPictures() called");
    }

    private void OnEnable ()
    {
        m_menuButton.OnButtonSelected += OnButtonSelected;
    }

    private void OnDisable ()
    {
        m_menuButton.OnButtonSelected -= OnButtonSelected;
    }        

    private void OnButtonSelected(VRStandardAssets.Menu.MenuButton button)
    {
        OpenAndroidGallery();
    }        

    private void LoadPictures(int startingPictureIndex, int numImages)
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
                byte[] fileByteData = File.ReadAllBytes(filePath); // make sure to have Write Access: External (SDCard)

                Debug.Log("------- VREEL: Loaded from filePath: " + filePath);

                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                texture.LoadImage(fileByteData);

                Debug.Log("------- VREEL: Loaded data into texture");

                m_imageSpheres[sphereIndex].GetComponent<MeshRenderer>().material.mainTexture = texture;

                Debug.Log("------- VREEL: Set texture on ImageSphere");
            }
        }
    }
}