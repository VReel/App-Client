using UnityEngine;
using UnityEngine.UI;               // Text
using System;                       // Datetime
using System.Collections;           // IEnumerator
using System.Collections.Generic;   // List
using System.IO;                    // Stream
using System.Net;                   // HttpWebRequest

public class Profile : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private GameObject m_errorMessage;
    [SerializeField] private GameObject m_staticLoadingIcon;
    [SerializeField] private GameObject m_newUserText;   
    [SerializeField] private GameObject m_galleryMessage;
    [SerializeField] private User m_user;

    private BackEndAPI m_backEndAPI;
    private List<string> m_postThumbnailURLs;
    private int m_currThumbnailURLIndex = -1;
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start() 
	{
        m_postThumbnailURLs = new List<string>();

        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        m_backEndAPI = new BackEndAPI(this, m_errorMessage, m_user);

        m_staticLoadingIcon.SetActive(false);
	}

    public void Update()
    {        
        if (m_appDirector.GetState() == AppDirector.AppState.kLogin)
        {
            if (m_user.IsLoggedIn())
            {
                m_appDirector.RequestProfileState();
            }
        }
        else if (m_appDirector.GetState() != AppDirector.AppState.kLogin)
        {
            if (!m_user.IsLoggedIn())
            {
                m_appDirector.RequestLoginState();
            }
        }
    }

    public void LogOut()
    {
        m_coroutineQueue.EnqueueAction(LogoutInternal());
    }
        
    public void InvalidateThumbnailLoading() // This function is called in order to stop any ongoing image loading 
    {        
        m_currThumbnailURLIndex = -1;
        if (m_coroutineQueue != null)
        {
            m_coroutineQueue.Clear();
        }
    }
        
    public bool IsThumbnailURLIndexAtStart()
    {
        return m_currThumbnailURLIndex <= 0;
    }

    public bool IsThumbnailURLIndexAtEnd()
    {
        int numImageSpheres = m_imageSphereController.GetNumSpheres();
        int numFiles = m_postThumbnailURLs.Count; // m_s3ImageFilePaths.Count;
        return m_currThumbnailURLIndex >= (numFiles - numImageSpheres);       
    }
        
    /*
    public void UploadImage()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: UploadImage() called");

        m_coroutineQueue.EnqueueAction(UploadImageInternal());
    }
    */

    public void OpenProfile()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OpenProfile() called");

        m_imageSphereController.SetAllImageSpheresToLoading();
        m_coroutineQueue.EnqueueAction(StoreAllPostURLsAndSetSpheres());
        //m_coroutineQueue.EnqueueAction(StoreAllS3ImagePathsAndSetSpheres());
    }       

    public void NextImages()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: NextImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_postThumbnailURLs.Count; // m_s3ImageFilePaths.Count;

        m_currThumbnailURLIndex = Mathf.Clamp(m_currThumbnailURLIndex + numImagesToLoad, 0, numFilePaths);

        m_coroutineQueue.Clear(); // Ensure we stop loading something that we may be loading
        m_coroutineQueue.EnqueueAction(DownloadImagesAndSetSpheres());
    }

    public void PreviousImages()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreviousImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_postThumbnailURLs.Count; // m_s3ImageFilePaths.Count;

        m_currThumbnailURLIndex = Mathf.Clamp(m_currThumbnailURLIndex - numImagesToLoad, 0, numFilePaths);

        m_coroutineQueue.Clear(); // Ensure we stop loading something that we may be loading
        m_coroutineQueue.EnqueueAction(DownloadImagesAndSetSpheres());
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator LogoutInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LogOut() called");

        m_staticLoadingIcon.SetActive(true);

        yield return m_backEndAPI.Session_SignOut();

        m_staticLoadingIcon.SetActive(false);
    }

    /*
    private IEnumerator UploadImageInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();

        m_staticLoadingIcon.SetActive(true);

        string fileName =  m_imageSkybox.GetImageFilePath();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: UploadImage with FileName: " + fileName);

        string datePattern = "yyyy_MM_dd_hh_mm_ss";
        string date = DateTime.UtcNow.ToString(datePattern);
        string key = "key";//m_userLogin.GetCognitoUserID() + "/" + date + "_" + Path.GetFileName(fileName);

        var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Creating request object with key: " + key);
        var request = new PostObjectRequest()
        {
            Bucket = "",//m_s3BucketName,
            Key = key,
            InputStream = stream,
            CannedACL = S3CannedACL.Private
        };

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Making HTTP post call");

        m_s3Client.PostObjectAsync(request, (responseObj) =>
        {
            if (responseObj.Exception == null)
            {
                string logString = string.Format("------- VREEL: Uploaded {0} posted to bucket {1}", responseObj.Request.Key, responseObj.Request.Bucket);
                if (Debug.isDebugBuild) Debug.Log(logString);

                // Report Success in Gallery
                Text galleryTextComponent = m_galleryMessage.GetComponentInChildren<Text>();
                if (galleryTextComponent != null)
                {
                    galleryTextComponent.text = "Succesful Upload!";
                    galleryTextComponent.color = Color.black;
                }
                m_galleryMessage.SetActive(true);
            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log("------- VREEL: Exception while posting the result object");
                if (Debug.isDebugBuild) Debug.Log("------- VREEL: Receieved error: " + responseObj.Response.HttpStatusCode.ToString());

                // Report Failure in Gallery
                Text galleryTextComponent = m_galleryMessage.GetComponentInChildren<Text>();
                if (galleryTextComponent != null)
                {
                    galleryTextComponent.text = "Failed to Upload!\n Try again later!";
                    galleryTextComponent.color = Color.red;
                }
                m_galleryMessage.SetActive(true);
            }

            m_staticLoadingIcon.SetActive(false);
        });
    }
    */
        
    // TODO: Handle users who have more than 20 posts!
    private IEnumerator StoreAllPostURLsAndSetSpheres()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Getting all Posts for Logged in User");

        m_postThumbnailURLs.Clear();

        yield return m_backEndAPI.Posts_GetAll();

        VReelJSON.Model_Posts posts = m_backEndAPI.GetPostsJSONData();
        if (posts != null)
        {
            foreach (VReelJSON.PostData postData in posts.data)
            {                
                m_postThumbnailURLs.Add(postData.attributes.thumbnail_url.ToString());
            }
        }           

        m_currThumbnailURLIndex = 0; // set to a valid Index
        m_coroutineQueue.EnqueueAction(DownloadImagesAndSetSpheres());

        bool noImagesUploaded = m_postThumbnailURLs.Count <= 0;
        m_newUserText.SetActive(noImagesUploaded); // If the user has yet to upload any images then show them the New User Text!
    }

    private IEnumerator DownloadImagesAndSetSpheres()
    {
        yield return m_appDirector.VerifyInternetConnection();

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        DownloadImagesAndSetSpheresInternal(m_currThumbnailURLIndex, numImagesToLoad);
    }

    private void DownloadImagesAndSetSpheresInternal(int startingThumbnailURLIndex, int numImages)
    {
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Downloading {0} images beginning at index {1}. We've found {2} posts for the user!", numImages, startingThumbnailURLIndex, m_postThumbnailURLs.Count));

        Resources.UnloadUnusedAssets();

        int currThumbnailURLIndex = startingThumbnailURLIndex;
        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, currThumbnailURLIndex++)
        {
            if (currThumbnailURLIndex < m_postThumbnailURLs.Count)
            {                   
                string thumbnailURL = m_postThumbnailURLs[currThumbnailURLIndex];
                m_coroutineQueue.EnqueueAction(DownloadImage(thumbnailURL, sphereIndex, currThumbnailURLIndex, numImages));
            }
            else
            {
                m_imageSphereController.HideSphereAtIndex(sphereIndex);
            }
        }

        Resources.UnloadUnusedAssets();
    }

    private IEnumerator DownloadImage(string url, int sphereIndex, int thisThumbnailURLIndex, int numImages)
    {
        yield return m_appDirector.VerifyInternetConnection();
                   
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Downloading: " + url));

        HttpWebRequest http = (HttpWebRequest)WebRequest.Create(url);
        m_coroutineQueue.EnqueueAction(LoadImageInternalUnity(http.GetResponse(), sphereIndex, url));

        /*
        using (WebClient webClient = new WebClient()) 
        {
            byte [] data = webClient.DownloadData(url);
            using (MemoryStream mem = new MemoryStream(data)) 
            {
                using (var yourImage = Image.FromStream(mem)) 
                { 
                }
            }
        }
        */
    }

    private IEnumerator LoadImageInternalPlugin(Amazon.S3.Model.GetObjectResponse response, int sphereIndex, string fullFilePath)
    {        
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LoadImageInternal for " + fullFilePath);

        using (var stream = response.ResponseStream)
        {
            yield return m_imageSphereController.LoadImageFromStream(stream, sphereIndex, fullFilePath);
        }
    }
        
    private IEnumerator LoadImageInternalUnity(WebResponse response, int sphereIndex, string fullFilePath)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: ConvertStreamAndSetImage for " + fullFilePath);

        const int kNumIterationsPerFrame = 150;
        byte[] myBinary = null;
        using (var stream = response.GetResponseStream())
        {            
            using( MemoryStream ms = new MemoryStream() )
            {
                int iterations = 0;
                int byteCount = 0;
                do
                {
                    byte[] buf = new byte[1024];
                    byteCount = stream.Read(buf, 0, 1024);
                    ms.Write(buf, 0, byteCount);
                    iterations++;
                    if (iterations % kNumIterationsPerFrame == 0)
                    {                        
                        yield return new WaitForEndOfFrame();
                    }
                } 
                while(stream.CanRead && byteCount > 0);

                myBinary = ms.ToArray();
            }
        }

        // The following is generally coming out to around 6-7MB in size...
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished iterating, length of byte[] is " + myBinary.Length);

        Texture2D newImage = new Texture2D(2,2); 
        newImage.LoadImage(myBinary);
        m_imageSphereController.SetImageAndFilePathAtIndex(sphereIndex, newImage, fullFilePath, -1);
        yield return new WaitForEndOfFrame();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Finished Setting Image!");

        Resources.UnloadUnusedAssets();
    }
}