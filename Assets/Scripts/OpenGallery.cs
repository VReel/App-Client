using UnityEngine;
using System.Collections;

public class OpenGallery : MonoBehaviour 
{
    [SerializeField] private GameObject m_imageSphere;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;

    public void OnMouseDown()
    {
        OpenAndroidGallery();
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

    public void OpenAndroidGallery()
    {
/*
        // Taken from http://answers.unity3d.com/questions/537476/open-gallery-android.html

        // Intent intent = new Intent();
        AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
        AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");

        // intent.setAction(Intent.ACTION_VIEW);
        intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_VIEW"));

        // intent.setData(Uri.parse("content://media/internal/images/media"));
        AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
        AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("parse", "content://media/internal/images/media"); //parse of the url's file
        intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uriObject); //call putExtra with the uri object of the file
        intentObject.Call<AndroidJavaObject>("setType", "image/jpeg"); //set the type of file

        // startActivity(intent);
        AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        currentActivity.Call("startActivity", intentObject);
*/

        StartCoroutine(CreateSphereAndLoadTexture_1());

        Debug.Log("------- VREEL: Open Gallery called");

    }

    IEnumerator CreateSphereAndLoadTexture_1()
    {
        //DirectoryInfo dir = new DirectoryInfo(Application.persistentDataPath);
        //FileInfo[] images = dir.GetFiles("*.png");

        //FileInfo firstFileInfo = images[0];
        //foreach (FileInfo f in images) 

        GameObject imageSphere = (GameObject) Instantiate(m_imageSphere, new Vector3(-1.5f, 0.0f, 3.0f), Quaternion.identity);
        Debug.Log("------- VREEL: Instantiated image sphere");

        //string path = "/data/media/0/DCIM/Gear 360/SAM_100_0002.jpg";
        string path = "/storage/emulated/0/DCIM/Camera/20161019_142127.jpg";
        WWW www = new WWW("file://" + path);
        Debug.Log("------- VREEL: Attempting to open hardcoded file://" + path);

        yield return www;
        Debug.Log("------- VREEL: Yield returned");

        if (string.IsNullOrEmpty(www.error))
        {            
            imageSphere.GetComponent<Material>().mainTexture = www.texture;
            Debug.Log("------- VREEL: No Error, texture changed successfully");
        }
        else
        {
            Debug.Log("------- VREEL: Error returned = " + www.error);
        }

    }
}