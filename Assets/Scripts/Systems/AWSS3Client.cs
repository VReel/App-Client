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

public class AWSS3Client : MonoBehaviour 
{   
    // **************************
    // Member Variables
    // **************************

    public string m_s3BucketName = null;

    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private UserLogin m_userLogin;
    [SerializeField] private GameObject m_staticLoadingIcon;
    [SerializeField] private GameObject m_newUserText;
    [SerializeField] private GameObject m_profileMessage;
    [SerializeField] private GameObject m_galleryMessage;

    private AmazonS3Client m_s3Client = null;
    private CognitoAWSCredentials m_cognitoCredentials = null;
    private int m_currS3ImageFilePathIndex = -1;
    private List<string> m_s3ImageFilePaths;
    private CoroutineQueue m_coroutineQueue;
    private ThreadJob m_threadJob;
    private CppPlugin m_cppPlugin;

    private const float kImageRequestDelay = 2.0f;

    // **************************
    // Public functions
    // **************************

    public void Start() 
	{
		UnityInitializer.AttachToGameObject(this.gameObject);

        m_s3ImageFilePaths = new List<string>();

        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        m_threadJob = new ThreadJob(this);
        m_cppPlugin = new CppPlugin(this);

        m_staticLoadingIcon.SetActive(false);
	}

    public void InitS3ClientFB(string fbAccessToken)
    {
        Debug.Log("------- VREEL: InitS3ClientFB() called!");

        m_cognitoCredentials = new CognitoAWSCredentials(
            "366575334313", // AWS Account ID
            "eu-west-1:bb57e466-72ed-408d-8c84-301d0bae1a9f", // Identity Pool ID
            "arn:aws:iam::366575334313:role/Cognito_VReelMainUnauth_Role", // unAuthRoleArn
            "arn:aws:iam::366575334313:role/Cognito_VReelMainAuth_Role", // authRoleArn
            RegionEndpoint.EUWest1 // Region
        );   

        m_cognitoCredentials.ClearIdentityCache();

        m_cognitoCredentials.AddLogin("graph.facebook.com", fbAccessToken);

        m_cognitoCredentials.GetIdentityIdAsync((idResponseObj) =>
        {
            Debug.Log("------- VREEL: GetIdentityIdAsync() lambda called!");

            if (idResponseObj.Exception != null)
            {
                Debug.Log("------- VREEL: Exception while calling GetIdentityIdAsync()");
                Debug.Log("------- VREEL: Receieved error: " + idResponseObj.Exception.ToString());
                return;
            }

            string identityId = idResponseObj.Response;
            m_userLogin.SetCognitoUserID(identityId);

            m_cognitoCredentials.GetCredentialsAsync((credentialsResponseObj) =>
            {
                Debug.Log("------- VREEL: GetCredentialsAsync() lambda called!");

                if (credentialsResponseObj.Exception != null)
                {
                    Debug.Log("------- VREEL: Exception while calling GetCredentialsAsync()");
                    Debug.Log("------- VREEL: Receieved error: " + credentialsResponseObj.Exception.ToString());
                    return;
                }

                SessionAWSCredentials sessionCreds = new SessionAWSCredentials(
                    credentialsResponseObj.Response.AccessKey, 
                    credentialsResponseObj.Response.SecretKey, 
                    credentialsResponseObj.Response.Token
                );

                AmazonS3Config s3Config = new AmazonS3Config();
                s3Config.RegionEndpoint = RegionEndpoint.EUWest1;

                m_s3Client = new AmazonS3Client(sessionCreds, s3Config);
            });
        });
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

    public void UploadImage()
    {
        Debug.Log("------- VREEL: UploadImage() called");

        m_coroutineQueue.EnqueueAction(UploadImageInternal());
    }

    public void OpenProfile()
    {
        Debug.Log("------- VREEL: OpenProfile() called");

        m_imageSphereController.SetAllImageSpheresToLoading();
        m_coroutineQueue.EnqueueAction(StoreAllS3ImagePathsAndSetSpheres());
    }       

    public void NextImages()
    {
        Debug.Log("------- VREEL: NextImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_s3ImageFilePaths.Count;

        m_currS3ImageFilePathIndex = Mathf.Clamp(m_currS3ImageFilePathIndex + numImagesToLoad, 0, numFilePaths);

        m_coroutineQueue.Clear(); // Ensure we stop loading something that we may be loading
        m_coroutineQueue.EnqueueAction(DownloadImagesAndSetSpheres());
    }

    public void PreviousImages()
    {
        Debug.Log("------- VREEL: PreviousImages() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_s3ImageFilePaths.Count;

        m_currS3ImageFilePathIndex = Mathf.Clamp(m_currS3ImageFilePathIndex - numImagesToLoad, 0, numFilePaths);

        m_coroutineQueue.Clear(); // Ensure we stop loading something that we may be loading
        m_coroutineQueue.EnqueueAction(DownloadImagesAndSetSpheres());
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator UploadImageInternal()
    {
        while (!IsS3ClientValid()) // We do this to ensure that this function only runs if there's a valid client
        {
            yield return new WaitForEndOfFrame(); //This will essentially block this coroutine queue, without blocking the main thread
        }

        m_staticLoadingIcon.SetActive(true);

        string fileName =  m_imageSkybox.GetImageFilePath();

        Debug.Log("------- VREEL: UploadImage with FileName: " + fileName);

        string datePattern = "yyyy_MM_dd_hh_mm_ss";
        string date = DateTime.UtcNow.ToString(datePattern);
        string key = m_userLogin.GetCognitoUserID() + "/" + date + "_" + Path.GetFileName(fileName);

        var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

        Debug.Log("------- VREEL: Creating request object with key: " + key);
        var request = new PostObjectRequest()
        {
            Bucket = m_s3BucketName,
            Key = key,
            InputStream = stream,
            CannedACL = S3CannedACL.Private
        };

        Debug.Log("------- VREEL: Making HTTP post call");

        m_s3Client.PostObjectAsync(request, (responseObj) =>
        {
            if (responseObj.Exception == null)
            {
                string logString = string.Format("------- VREEL: Uploaded {0} posted to bucket {1}", responseObj.Request.Key, responseObj.Request.Bucket);
                Debug.Log(logString);

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
                Debug.Log("------- VREEL: Exception while posting the result object");
                Debug.Log("------- VREEL: Receieved error: " + responseObj.Response.HttpStatusCode.ToString());

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
    private IEnumerator StoreAllS3ImagePathsAndSetSpheres()
    {
        while (!IsS3ClientValid()) // We do this to ensure that this function only runs if there's a valid client
        {
            Debug.Log("------- VREEL: S3 Client not constructed yet");
            yield return new WaitForEndOfFrame(); //This will essentially block this coroutine queue, without blocking the main thread
        }

        Debug.Log("------- VREEL: Fetching all the Objects from: " + m_s3BucketName + "/" + m_userLogin.GetCognitoUserID() + "/");

        var request = new ListObjectsRequest()
        {
            BucketName = m_s3BucketName,
            Prefix =  m_userLogin.GetCognitoUserID()
        };

        m_currS3ImageFilePathIndex = 0;
        m_s3Client.ListObjectsAsync(request, (responseObject) =>
        {            
            if (responseObject.Exception == null)
            {
                Debug.Log("------- VREEL: Got successful response from ListObjectsAsync(), storing file paths!");

                m_s3ImageFilePaths.Clear();

                responseObject.Response.S3Objects.ForEach((s3object) => //NOTE: Making this into a seperate function seemed more work than worth
                {
                    if (ImageExtensions.Contains(Path.GetExtension(s3object.Key).ToUpperInvariant())) // Check that the file is indeed an image
                    {   
                        m_s3ImageFilePaths.Add(s3object.Key);
                        Debug.Log("------- VREEL: Stored FileName: " + s3object.Key);
                    }
                });

                m_s3ImageFilePaths.Reverse(); // Reversing to have the images appear in the order of newest - this works because I store images with a timestamp!

                m_coroutineQueue.EnqueueAction(DownloadImagesAndSetSpheres());
            }
            else
            {
                Debug.Log("------- VREEL: Got an Exception calling 'ListObjectsAsync()'");

                // Report Failure in Profile
                Text profileTextComponent = m_profileMessage.GetComponentInChildren<Text>();
                if (profileTextComponent != null)
                {
                    profileTextComponent.text = "Failed to get Images!\n Try again later!";
                    profileTextComponent.color = Color.red;
                }
                m_profileMessage.SetActive(true);
            }

            bool noImagesUploaded = m_s3ImageFilePaths.Count <= 0;
            m_newUserText.SetActive(noImagesUploaded); // If the user has yet to upload any images then show them the New User Text!
        });
    }

    private IEnumerator DownloadImagesAndSetSpheres()
    {
        while (!IsS3ClientValid()) // We do this to ensure that this function only runs if there's a valid client
        {
            Debug.Log("------- VREEL: S3 Client not constructed yet");
            yield return new WaitForEndOfFrame(); //This will essentially block this coroutine queue, without blocking the main thread
        }

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        DownloadImagesAndSetSpheresInternal(m_currS3ImageFilePathIndex, numImagesToLoad);
    }

    private void DownloadImagesAndSetSpheresInternal(int startingS3ImageIndex, int numImages)
    {
        Debug.Log(string.Format("------- VREEL: Downloading {0} images beginning at index {1}. There are {2} images in this S3 folder!", 
            numImages, startingS3ImageIndex, m_s3ImageFilePaths.Count));

        Resources.UnloadUnusedAssets();

        int currS3ImageIndex = startingS3ImageIndex;
        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, currS3ImageIndex++)
        {
            if (currS3ImageIndex < m_s3ImageFilePaths.Count)
            {                   
                string filePath = m_s3ImageFilePaths[currS3ImageIndex];
                DownloadImage(filePath, sphereIndex, currS3ImageIndex, numImages);
            }
            else
            {
                m_imageSphereController.HideSphereAtIndex(sphereIndex);
            }
        }

        Resources.UnloadUnusedAssets();
    }

    private void DownloadImage(string filePath, int sphereIndex, int thisS3ImageIndex, int numImages)
    {
        string fullFilePath = m_s3BucketName + filePath;
        string logString01 = string.Format("------- VREEL: Downloading {0} from bucket {1}", filePath, m_s3BucketName);       
        Debug.Log(logString01);

        m_s3Client.GetObjectAsync(m_s3BucketName, filePath, (s3ResponseObj) =>
        {               
            var response = s3ResponseObj.Response;
            if (response != null && response.ResponseStream != null)
            {                   
                bool imageRequestStillValid = 
                    (m_currS3ImageFilePathIndex != -1) && 
                    (m_currS3ImageFilePathIndex <= thisS3ImageIndex) &&  
                    (thisS3ImageIndex < m_currS3ImageFilePathIndex + numImages) && // Request no longer valid as user has moved on from this page
                    (filePath.CompareTo(m_imageSkybox.GetImageFilePath()) != 0); // If file-path is the same then ignore request
                
                string logString02 = string.Format("------- VREEL: Checking validity returned '{0}' when checking that {1} <= {2} < {1}+{3}", imageRequestStillValid, m_currS3ImageFilePathIndex, thisS3ImageIndex, numImages); 
                Debug.Log(logString02);
                if (imageRequestStillValid)
                {
                    m_coroutineQueue.EnqueueAction(LoadImageInternalUnity(response, sphereIndex, fullFilePath));
                    m_coroutineQueue.EnqueueWait(kImageRequestDelay);

                    Debug.Log("------- VREEL: Successfully downloaded and requested to set " + fullFilePath);
                }
                else
                {
                    Debug.Log("------- VREEL: Downloaded item successfully but was thrown away because user has moved off that page: " + fullFilePath);
                }

                Resources.UnloadUnusedAssets();
            }
            else
            {
                Debug.Log("------- VREEL: Got an Exception calling GetObjectAsync() for: " + fullFilePath);
                Debug.Log("------- VREEL: Exception was: " + s3ResponseObj.Exception.ToString());

                // Report Failure in Profile
                Text profileTextComponent = m_profileMessage.GetComponentInChildren<Text>();
                if (profileTextComponent != null)
                {
                    profileTextComponent.text = "Failed getting Image!\n Re-open Profile!";
                    profileTextComponent.color = Color.red;
                }
                m_profileMessage.SetActive(true);
            }
        });
    }

    private IEnumerator LoadImageInternalPlugin(Amazon.S3.Model.GetObjectResponse response, int sphereIndex, string fullFilePath)
    {        
        Debug.Log("------- VREEL: LoadImageInternal for " + fullFilePath);

        using (var stream = response.ResponseStream)
        {
            yield return m_cppPlugin.LoadImageFromStream(m_threadJob, stream, m_imageSphereController, sphereIndex, fullFilePath);
        }
    }
        
    private IEnumerator LoadImageInternalUnity(Amazon.S3.Model.GetObjectResponse response, int sphereIndex, string fullFilePath)
    {
        Debug.Log("------- VREEL: ConvertStreamAndSetImage for " + fullFilePath);

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
        Debug.Log("------- VREEL: Finished iterating, length of byte[] is " + myBinary.Length);

        Texture2D newImage = new Texture2D(2,2); 
        newImage.LoadImage(myBinary);
        m_imageSphereController.SetImageAndFilePathAtIndex(sphereIndex, newImage, fullFilePath);
        yield return new WaitForEndOfFrame();

        Debug.Log("------- VREEL: Finished Setting Image!");

        Resources.UnloadUnusedAssets();
    }
}