using UnityEngine;
using UnityEngine.UI;               // Text
using System;                       // Datetime
using System.Collections;           // IEnumerator
using System.Collections.Generic;   // List
using System.IO;                    // Stream
using Amazon;                       // UnityInitializer
using Amazon.CognitoIdentity;       // CognitoAWSCredentials
using Amazon.S3;                    // AmazonS3Client
using Amazon.S3.Model;              // ListBucketsRequest
using Amazon.Runtime;               // SessionAWSCredentials

public class Profile : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private GameObject m_errorMessage;

    private BackEndAPI m_backEndAPI;

    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private GameObject m_staticLoadingIcon;
    [SerializeField] private GameObject m_newUserText;   
    [SerializeField] private GameObject m_galleryMessage;
    [SerializeField] private User m_user;

    private AmazonS3Client m_s3Client = null;
    private CognitoAWSCredentials m_cognitoCredentials = null;
    private int m_currS3ImageFilePathIndex = -1;
    private List<string> m_s3ImageFilePaths;
    private CoroutineQueue m_coroutineQueue;

    //TODO DELETE
    //[SerializeField] private UserLogin m_userLogin;

    // **************************
    // Public functions
    // **************************

    public void Start() 
	{
		//UnityInitializer.AttachToGameObject(this.gameObject);

        m_s3ImageFilePaths = new List<string>();

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
        
    public void InvalidateS3ImageLoading() // This function is called in order to stop any ongoing image loading 
    {        
        m_currS3ImageFilePathIndex = -1;
        if (m_coroutineQueue != null)
        {
            m_coroutineQueue.Clear();
        }
    }
        
    public bool IsS3ImageIndexAtStart()
    {
        return m_currS3ImageFilePathIndex <= 0;
    }

    public bool IsS3ImageIndexAtEnd()
    {
        int numImageSpheres = m_imageSphereController.GetNumSpheres();
        int numFiles = m_s3ImageFilePaths.Count;
        return m_currS3ImageFilePathIndex >= (numFiles - numImageSpheres);       
    }

    public void ClearClient()
    {
        if (m_cognitoCredentials != null)
        {
            m_cognitoCredentials.Clear();
        }

        m_s3Client = null;
    }

    public bool IsS3ClientValid()
    {
        return m_s3Client != null;
    }

    public IEnumerator WaitForValidS3Client()
    {
        while (!IsS3ClientValid()) yield return null;
    }

    public void UploadImage()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: UploadImage() called");

        m_coroutineQueue.EnqueueAction(UploadImageInternal());
    }

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
        int numFilePaths = m_s3ImageFilePaths.Count;

        m_currS3ImageFilePathIndex = Mathf.Clamp(m_currS3ImageFilePathIndex + numImagesToLoad, 0, numFilePaths);

        m_coroutineQueue.Clear(); // Ensure we stop loading something that we may be loading
        m_coroutineQueue.EnqueueAction(DownloadImagesAndSetSpheres());
    }

    public void PreviousImages()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: PreviousImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_s3ImageFilePaths.Count;

        m_currS3ImageFilePathIndex = Mathf.Clamp(m_currS3ImageFilePathIndex - numImagesToLoad, 0, numFilePaths);

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

    private IEnumerator UploadImageInternal()
    {
        yield return m_appDirector.VerifyInternetConnection();
        yield return WaitForValidS3Client();

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

    private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
    private IEnumerator StoreAllPostURLsAndSetSpheres()
    {
        yield return m_appDirector.VerifyInternetConnection();

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Getting all Posts for Logged in User");

        yield return m_backEndAPI.Posts_GetAll();

        yield break;
    }

    private IEnumerator StoreAllS3ImagePathsAndSetSpheres()
    {
        yield return m_appDirector.VerifyInternetConnection();
        yield return WaitForValidS3Client();

        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: Fetching all the Objects from: " + "" + "/" + m_userLogin.GetCognitoUserID() + "/");

        var request = new ListObjectsRequest()
        {
            BucketName = "", //m_s3BucketName,
            Prefix = "prefix" //m_userLogin.GetCognitoUserID()
        };

        m_currS3ImageFilePathIndex = 0;
        m_s3Client.ListObjectsAsync(request, (responseObject) =>
        {            
            if (responseObject.Exception == null)
            {
                if (Debug.isDebugBuild) Debug.Log("------- VREEL: Got successful response from ListObjectsAsync(), storing file paths!");

                m_s3ImageFilePaths.Clear();

                responseObject.Response.S3Objects.ForEach((s3object) => //NOTE: Making this into a seperate function seemed more work than worth
                {
                    if (ImageExtensions.Contains(Path.GetExtension(s3object.Key).ToUpperInvariant())) // Check that the file is indeed an image
                    {   
                        m_s3ImageFilePaths.Add(s3object.Key);
                        if (Debug.isDebugBuild) Debug.Log("------- VREEL: Stored FileName: " + s3object.Key);
                    }
                });

                m_s3ImageFilePaths.Reverse(); // Reversing to have the images appear in the order of newest - this works because I store images with a timestamp!

                m_coroutineQueue.EnqueueAction(DownloadImagesAndSetSpheres());
            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log("------- VREEL: Got an Exception calling 'ListObjectsAsync()'");

                // Report Failure in Profile
                /*
                Text profileTextComponent = m_profileMessage.GetComponentInChildren<Text>();
                if (profileTextComponent != null)
                {
                    profileTextComponent.text = "Failed to get Images!\n Try again later!";
                    profileTextComponent.color = Color.red;
                }
                m_profileMessage.SetActive(true);
                */
            }

            bool noImagesUploaded = m_s3ImageFilePaths.Count <= 0;
            m_newUserText.SetActive(noImagesUploaded); // If the user has yet to upload any images then show them the New User Text!
        });
    }

    private IEnumerator DownloadImagesAndSetSpheres()
    {
        yield return m_appDirector.VerifyInternetConnection();
        yield return WaitForValidS3Client();

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        DownloadImagesAndSetSpheresInternal(m_currS3ImageFilePathIndex, numImagesToLoad);
    }

    private void DownloadImagesAndSetSpheresInternal(int startingS3ImageIndex, int numImages)
    {
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Downloading {0} images beginning at index {1}. There are {2} images in this S3 folder!", numImages, startingS3ImageIndex, m_s3ImageFilePaths.Count));

        Resources.UnloadUnusedAssets();

        int currS3ImageIndex = startingS3ImageIndex;
        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, currS3ImageIndex++)
        {
            if (currS3ImageIndex < m_s3ImageFilePaths.Count)
            {                   
                string filePath = m_s3ImageFilePaths[currS3ImageIndex];
                m_coroutineQueue.EnqueueAction(DownloadImage(filePath, sphereIndex, currS3ImageIndex, numImages));
            }
            else
            {
                m_imageSphereController.HideSphereAtIndex(sphereIndex);
            }
        }

        Resources.UnloadUnusedAssets();
    }

    private IEnumerator DownloadImage(string filePath, int sphereIndex, int thisS3ImageIndex, int numImages)
    {
        yield return m_appDirector.VerifyInternetConnection();
        yield return WaitForValidS3Client();

        string fullFilePath = filePath; //m_s3BucketName + filePath;           
        if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Downloading {0} from bucket {1}", filePath, ""));

        m_s3Client.GetObjectAsync("", filePath, (s3ResponseObj) => //m_s3BucketName
        {               
            var response = s3ResponseObj.Response;
            if (response != null && response.ResponseStream != null)
            {                   
                bool imageRequestStillValid = 
                    (m_currS3ImageFilePathIndex != -1) && 
                    (m_currS3ImageFilePathIndex <= thisS3ImageIndex) &&  
                    (thisS3ImageIndex < m_currS3ImageFilePathIndex + numImages) && // Request no longer valid as user has moved on from this page
                    (filePath.CompareTo(m_imageSkybox.GetImageFilePath()) != 0); // If file-path is the same then ignore request
                
                if (Debug.isDebugBuild) Debug.Log(string.Format("------- VREEL: Checking validity returned '{0}' when checking that {1} <= {2} < {1}+{3}", imageRequestStillValid, m_currS3ImageFilePathIndex, thisS3ImageIndex, numImages) );
                if (imageRequestStillValid)
                {
                    m_coroutineQueue.EnqueueAction(LoadImageInternalPlugin(response, sphereIndex, fullFilePath));

                    if (Debug.isDebugBuild) Debug.Log("------- VREEL: Successfully downloaded and requested to set " + fullFilePath);
                }
                else
                {
                    if (Debug.isDebugBuild) Debug.Log("------- VREEL: Downloaded item successfully but was thrown away because user has moved off that page: " + fullFilePath);
                }

                Resources.UnloadUnusedAssets();
            }
            else
            {
                if (Debug.isDebugBuild) Debug.Log("------- VREEL: Got an Exception calling GetObjectAsync() for: " + fullFilePath);
                if (Debug.isDebugBuild) Debug.Log("------- VREEL: Exception was: " + s3ResponseObj.Exception.ToString());

                // Report Failure in Profile
                /*
                Text profileTextComponent = m_profileMessage.GetComponentInChildren<Text>();
                if (profileTextComponent != null)
                {
                    profileTextComponent.text = "Failed getting Image!\n Re-open Profile!";
                    profileTextComponent.color = Color.red;
                }
                m_profileMessage.SetActive(true);
                */
            }
        });
    }

    private IEnumerator LoadImageInternalPlugin(Amazon.S3.Model.GetObjectResponse response, int sphereIndex, string fullFilePath)
    {        
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LoadImageInternal for " + fullFilePath);

        using (var stream = response.ResponseStream)
        {
            yield return m_imageSphereController.LoadImageFromStream(stream, sphereIndex, fullFilePath);
        }
    }
        
    private IEnumerator LoadImageInternalUnity(Amazon.S3.Model.GetObjectResponse response, int sphereIndex, string fullFilePath)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: ConvertStreamAndSetImage for " + fullFilePath);

        const int kNumIterationsPerFrame = 150;
        byte[] myBinary = null;
        using (var stream = response.ResponseStream)
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