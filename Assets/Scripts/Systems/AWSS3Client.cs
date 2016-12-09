using UnityEngine;
using System;                       // Datetime
using System.Collections;           // IEnumerator
using System.Collections.Generic;   // List
using System.IO;                    // Stream
using Amazon;                       // UnityInitializer
using Amazon.CognitoIdentity;       // CognitoAWSCredentials
using Amazon.S3;                    // AmazonS3Client
using Amazon.S3.Model;              // ListBucketsRequest
using Amazon.Runtime;               // SessionAWSCredentials
/*
using Amazon.S3.Util;
*/

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

    private AmazonS3Client m_s3Client = null;
    private CognitoAWSCredentials m_cognitoCredentials = null;
    private int m_currS3ImageIndex = -1;
    private List<string> m_s3ImageFilePaths;
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Start() 
	{
		UnityInitializer.AttachToGameObject(this.gameObject);

        m_s3ImageFilePaths = new List<string>();

        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();
	}

    public void InitS3ClientFB(string fbAccessToken)
    {
        m_cognitoCredentials = new CognitoAWSCredentials(
            "366575334313", // AWS Account ID
            "eu-west-1:bb57e466-72ed-408d-8c84-301d0bae1a9f", // Identity Pool ID
            "arn:aws:iam::366575334313:role/Cognito_VReelMainUnauth_Role", // unAuthRoleArn
            "arn:aws:iam::366575334313:role/Cognito_VReelMainAuth_Role", // authRoleArn
            RegionEndpoint.EUWest1 // Region
        );   

        m_cognitoCredentials.AddLogin("graph.facebook.com", fbAccessToken);

        m_cognitoCredentials.GetIdentityIdAsync((idResponseObj) =>
        {
            if (idResponseObj.Exception != null)
            {
                Debug.Log("------- VREEL: Exception while assuming IAM role for S3 bucket");
                Debug.Log("------- VREEL: Receieved error: " + idResponseObj.Exception.ToString());
                return;
            }

            string identityId = idResponseObj.Response;
            m_userLogin.SetCognitoUserID(identityId);

            m_cognitoCredentials.GetCredentialsAsync((credentialsResponseObj) =>
            {
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
        
    //TODO Make use of word picture and image consistent!\
    //TODO Improve function names... especially the 4 Download functions at the bottom...
    public void InvalidateS3ImageLoading() // This function is called in order to stop any ongoing picture loading 
    {
        m_currS3ImageIndex = -1;
    }
        
    public bool IsS3ImageIndexAtStart()
    {
        return m_currS3ImageIndex <= 0;
    }

    public bool IsS3ImageIndexAtEnd()
    {
        int numImageSpheres = m_imageSphereController.GetNumSpheres();
        int numFiles = m_s3ImageFilePaths.Count;
        return m_currS3ImageIndex >= (numFiles - numImageSpheres);       
    }

    public bool IsS3ClientValid()
    {
        return m_s3Client != null;
    }

    public void UploadImage()
    {
        m_coroutineQueue.EnqueueAction(UploadImageInternal());
    }

    public void DownloadAllImages()
    {
        m_coroutineQueue.EnqueueAction(DownloadAllImagesInternal());
    }       

    public void NextImages()
    {
        Debug.Log("------- VREEL: NextPictures() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_s3ImageFilePaths.Count;

        m_currS3ImageIndex = Mathf.Clamp(m_currS3ImageIndex + numImagesToLoad, 0, numFilePaths);

        m_coroutineQueue.Clear(); // Ensure we stop loading something that we may be loading
        DownloadImagesAndSetSpheres();
    }

    public void PreviousImages()
    {
        Debug.Log("------- VREEL: PreviousPictures() called");

        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        int numFilePaths = m_s3ImageFilePaths.Count;

        m_currS3ImageIndex = Mathf.Clamp(m_currS3ImageIndex - numImagesToLoad, 0, numFilePaths);

        m_coroutineQueue.Clear(); // Ensure we stop loading something that we may be loading
        DownloadImagesAndSetSpheres();
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
            }
            else
            {
                Debug.Log("------- VREEL: Exception while posting the result object");
                Debug.Log("------- VREEL: Receieved error: " + responseObj.Response.HttpStatusCode.ToString());
            }

            m_staticLoadingIcon.SetActive(false);
        });
    }

    private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
    private IEnumerator DownloadAllImagesInternal()
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

        m_currS3ImageIndex = 0;
        m_s3Client.ListObjectsAsync(request, (responseObject) =>
        {
            if (responseObject.Exception == null)
            {
                Debug.Log("------- VREEL: Got Response, printing now!");

                responseObject.Response.S3Objects.ForEach((s3object) => //NOTE: Making this into a seperate function seemed more work than worth
                {
                    if (ImageExtensions.Contains(Path.GetExtension(s3object.Key).ToUpperInvariant())) // Check that the file is indeed an image
                    {   
                        m_s3ImageFilePaths.Add(s3object.Key);
                        Debug.Log("------- VREEL: Stored FileName: " + s3object.Key);
                    }
                });

                m_s3ImageFilePaths.Reverse(); // Reversing to have the images appear in the order of newest - this works because I store images with a timestamp!

                DownloadImagesAndSetSpheres();
            }
            else
            {
                Debug.Log("------- VREEL: Got an Exception calling 'ListObjectsAsync()'");
            }
        });
    }

    private void DownloadImagesAndSetSpheres()
    {
        int numImagesToLoad = m_imageSphereController.GetNumSpheres();
        DownloadImagesAndSetSpheresInternal(m_currS3ImageIndex, numImagesToLoad);
    }

    private void DownloadImagesAndSetSpheresInternal(int startingPictureIndex, int numImages)
    {
        Debug.Log(string.Format("------- VREEL: Downloading {0} pictures beginning at index {1}. There are {2} pictures in this S3 folder!", 
            numImages, startingPictureIndex, m_s3ImageFilePaths.Count));

        Resources.UnloadUnusedAssets();

        int currPictureIndex = startingPictureIndex;
        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, currPictureIndex++)
        {
            if (currPictureIndex < m_s3ImageFilePaths.Count)
            {                   
                string filePath = m_s3ImageFilePaths[currPictureIndex];
                DownloadImage(filePath, sphereIndex, currPictureIndex, numImages);
            }
            else
            {
                m_imageSphereController.HideSphereAtIndex(sphereIndex);
            }
        }

        Resources.UnloadUnusedAssets();
    }

    private void DownloadImage(string filePath, int sphereIndex, int pictureIndex, int numImages)
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
                    (m_currS3ImageIndex != -1) && 
                    (m_currS3ImageIndex <= pictureIndex) &&  
                    (pictureIndex < m_currS3ImageIndex + numImages); // Request no longer valid as user has moved on from this page
                
                string logString02 = string.Format("------- VREEL: Checking validity returned '{0}' when checking that {1} <= {2} < {1}+{3}", imageRequestStillValid, m_currS3ImageIndex, pictureIndex, numImages); 
                Debug.Log(logString02);
                if (imageRequestStillValid)
                {
                    m_coroutineQueue.EnqueueAction(ConvertStreamAndSetImage(response, sphereIndex, fullFilePath));
                    m_coroutineQueue.EnqueueWait(2.0f);

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
            }
        });
    }

    private IEnumerator ConvertStreamAndSetImage(Amazon.S3.Model.GetObjectResponse response, int sphereIndex, string fullFilePath)
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

        // TODO: Make copying texture not block!
        Texture2D newImage = new Texture2D(2,2); 
        newImage.LoadImage(myBinary);
        m_imageSphereController.SetImageAndFilePathAtIndex(sphereIndex, newImage, fullFilePath);
        yield return new WaitForEndOfFrame();

        Debug.Log("------- VREEL: Finished Setting Image!");

        Resources.UnloadUnusedAssets();
    }

    // TODO Understand this function properly...
    private byte[] ToByteArray(Stream stream)
    {
        byte[] b = null;
        using( MemoryStream ms = new MemoryStream() )
        {
            int count = 0;
            do
            {
                byte[] buf = new byte[1024];
                count = stream.Read(buf, 0, 1024);
                ms.Write(buf, 0, count);
            } 
            while(stream.CanRead && count > 0);

            b = ms.ToArray();
        }
        return b;
    }        
}