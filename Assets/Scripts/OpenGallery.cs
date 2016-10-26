using UnityEngine;
using System.Collections;
using System.IO;

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

    /*
     * EXAMPLE OF USING INTENTS!
     
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

    public void OpenAndroidGallery()
    {
        GameObject imageSphere = (GameObject) Instantiate(m_imageSphere, new Vector3(-0.5f, 0.0f, 3.0f), Quaternion.identity);       
        Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);

        string path = "/storage/emulated/0/DCIM/Gear 360/SAM_100_0091.jpg"; // Hardcoded path
        byte[] fileByteData = File.ReadAllBytes(path); // make sure to have Write Access: External (SDCard)
        texture.LoadImage(fileByteData);

        imageSphere.GetComponent<MeshRenderer>().material.mainTexture = texture;

        Debug.Log("------- VREEL: Open Gallery called");
    }
}