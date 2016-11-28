using UnityEngine;
using System.Collections;            //IEnumerator
using System.Collections.Generic;   // List
using System.IO;                    // Stream
using Amazon;                       // UnityInitializer
using Amazon.CognitoIdentity;       // CognitoAWSCredentials
using Amazon.S3;                    // AmazonS3Client
using Amazon.S3.Model;              // ListBucketsRequest
/*
using Amazon.S3.Util;
using Amazon.Runtime;
*/

public class AWSS3Client : MonoBehaviour 
{   
    public string m_s3BucketName = null;
    public GameObject[] m_imageSpheres;

    [SerializeField] private ImageSkybox m_imageSkybox;
    [SerializeField] private UserLogin m_userLogin;

    private AmazonS3Client m_s3Client = null;
    private CognitoAWSCredentials m_credentials = null;

    private int m_currS3ImageIndex = 0;
    private List<string> m_s3ImageFilePaths;
    private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
    private CoroutineQueue coroutineQueue;

    void Start() 
	{
		UnityInitializer.AttachToGameObject(this.gameObject);

        m_credentials = new CognitoAWSCredentials (
            "eu-west-1:bb57e466-72ed-408d-8c84-301d0bae1a9f", // Identity Pool ID
            RegionEndpoint.EUWest1 // Region
        );
        m_s3Client = new AmazonS3Client (m_credentials, RegionEndpoint.EUWest1);

        m_s3ImageFilePaths = new List<string>();

        coroutineQueue = new CoroutineQueue( this );
        coroutineQueue.StartLoop();
	}

    public CognitoAWSCredentials GetCredentials()
    {
        return m_credentials; //TODO: Some sort of validity check!
    }

    public bool IsIndexAtStart()
    {
        return m_currS3ImageIndex == 0;
    }

    public bool IsIndexAtEnd()
    {
        int numImageSpheres = m_imageSpheres.GetLength(0);
        int numFiles = m_s3ImageFilePaths.Count;
        return m_currS3ImageIndex >= (numFiles - numImageSpheres);       
    }

    public void UploadImage()
    {
        string fileName = m_userLogin.GetUserID() + "/" + m_imageSkybox.GetImageFilePath();

        Debug.Log("------- VREEL: UploadImage with FileName: " + fileName);

        var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        string key = "Images/" + Path.GetFileName(fileName);

        Debug.Log("------- VREEL: Creating request object");
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
                string logString = string.Format("------- VREEL: object {0} posted to bucket {1}", responseObj.Request.Key, responseObj.Request.Bucket);
                Debug.Log(logString);
            }
            else
            {
                Debug.Log("------- VREEL: Exception while posting the result object");
                Debug.Log("------- VREEL: Receieved error: " + responseObj.Response.HttpStatusCode.ToString());
            }
        });
    }

    public void DownloadAllImages()
    {
        Debug.Log("------- VREEL: Fetching all the Objects from" + m_s3BucketName + m_userLogin.GetUserID() + "/");

        var request = new ListObjectsRequest()
        {
            BucketName = m_s3BucketName,
            Prefix =  m_userLogin.GetUserID()
        };

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
                        Debug.Log("------- VREEL: Saved FileName: " + s3object.Key);
                    }
                });

                m_s3ImageFilePaths.Reverse(); // Reversing to have the images appear in the order of newest first

                DownloadAllImagesInternal();
            }
            else
            {
                Debug.Log("------- VREEL: Got an Exception calling 'ListObjectsAsync()'");
            }
        });
    }       

    public void NextImages()
    {
        Debug.Log("------- VREEL: NextPictures() called");

        int numImageSpheres = m_imageSpheres.GetLength(0);
        int numFilePaths = m_s3ImageFilePaths.Count;

        m_currS3ImageIndex = Mathf.Clamp(m_currS3ImageIndex + numImageSpheres, 0, numFilePaths);

        coroutineQueue.Clear(); // Ensure we stop loading something that we may be loading
        DownloadAllImagesInternal();
    }

    public void PreviousImages()
    {
        Debug.Log("------- VREEL: PreviousPictures() called");

        int numImageSpheres = m_imageSpheres.GetLength(0);
        int numFilePaths = m_s3ImageFilePaths.Count;

        m_currS3ImageIndex = Mathf.Clamp(m_currS3ImageIndex - numImageSpheres, 0, numFilePaths);

        coroutineQueue.Clear(); // Ensure we stop loading something that we may be loading
        DownloadAllImagesInternal();
    }

    private void DownloadAllImagesInternal()
    {
        int numImageSpheres = m_imageSpheres.GetLength(0);
        DownloadImagesAndSetSpheres(m_currS3ImageIndex, numImageSpheres);
    }

    private void DownloadImagesAndSetSpheres(int startingPictureIndex, int numImages)
    {
        Debug.Log(string.Format("------- VREEL: Downloading {0} pictures beginning at index {1}. There are {2} pictures in this S3 folder!", 
            numImages, startingPictureIndex, m_s3ImageFilePaths.Count));

        Resources.UnloadUnusedAssets();

        int currPictureIndex = startingPictureIndex;
        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, currPictureIndex++)
        {
            if (currPictureIndex < m_s3ImageFilePaths.Count)
            {   
                Debug.Log("------- VREEL: Loop iteration: " + sphereIndex);
                string filePath = m_s3ImageFilePaths[currPictureIndex];
                DownloadImage(filePath, sphereIndex, currPictureIndex, numImages);
            }
            else
            {
                m_imageSpheres[sphereIndex].GetComponent<SelectImage>().Hide();
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
                bool requestStillValid = (m_currS3ImageIndex <= pictureIndex) &&  (pictureIndex < m_currS3ImageIndex + numImages); // Request no longer valid as user pressed Next or Previous arrows
                string logString02 = string.Format("------- VREEL: Checking validity returned '{0}' when checking that {1} <= {2} < {1}+{3}", requestStillValid, m_currS3ImageIndex, pictureIndex, numImages); 
                Debug.Log(logString02);
                if (requestStillValid)
                {
                    coroutineQueue.EnqueueAction(ConvertStreamAndSetImage(response, sphereIndex, fullFilePath));
                    coroutineQueue.EnqueueWait(2.0f);

                    Debug.Log("------- VREEL: Successfully downloaded and set " + fullFilePath);
                }
                else
                {
                    Debug.Log("------- VREEL: Downloaded item successfully but was thrown away because user has moved to previous or next: " + fullFilePath);
                }

                Resources.UnloadUnusedAssets();
            }
            else
            {
                Debug.Log("------- VREEL: Got an Exception calling GetObjectAsync() for: " + fullFilePath);
                Debug.Log("------- VREEL: Exception was: " + s3ResponseObj.Exception);
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
        m_imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(myBinary, fullFilePath);
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