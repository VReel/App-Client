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

    private AmazonS3Client m_s3Client = null;
    private CognitoAWSCredentials m_credentials = null;

    private int m_currS3ImageIndex = 0;
    private List<string> m_s3ImageFilePaths;
    private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };

    void Start () 
	{
		UnityInitializer.AttachToGameObject(this.gameObject);

        m_credentials = new CognitoAWSCredentials (
            "eu-west-1:1f9f6bd1-3cfe-43c2-afbc-3e06d8d1fe27", // Identity Pool ID
            RegionEndpoint.EUWest1 // Region
        );
        m_s3Client = new AmazonS3Client (m_credentials, RegionEndpoint.EUWest1);

        m_s3ImageFilePaths = new List<string>();
	}

    public void UploadImage()
    {
        string fileName = m_imageSkybox.GetImageFilePath();

        Debug.Log("------- VREEL: UploadImage with FileName: " + fileName);

        //Application.persistentDataPath + Path.DirectorySeparatorChar +
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

    private bool alreadyRan = false; // TEMP
    public void DownloadAllImages()
    {
        // TEMP - making sure this function only gets called once, as in my Test it doesn't need to run multiple times!
        if (alreadyRan)
        {
            return;
        }
        alreadyRan = true;
        // -------

        Debug.Log("------- VREEL: Fetching all the Objects from" + m_s3BucketName);

        var request = new ListObjectsRequest()
        {
            BucketName = m_s3BucketName
        };

        m_s3Client.ListObjectsAsync(request, (responseObject) =>
        {
            if (responseObject.Exception == null)
            {
                Debug.Log("------- VREEL: Got Response, printing now!");

                //TODO: Make this foreach loop into a seperate function!
                responseObject.Response.S3Objects.ForEach((s3object) =>
                {
                    if (ImageExtensions.Contains(Path.GetExtension(s3object.Key).ToUpperInvariant())) // Check that the file is indeed an image
                    {   
                        m_s3ImageFilePaths.Add(s3object.Key);
                        Debug.Log("------- VREEL: Fetched " + s3object.Key);
                    }
                });

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

        StartCoroutine(DownloadImagesAndSetSpheres(m_currS3ImageIndex, numImageSpheres));       
    }

    public void PreviousImages()
    {
        Debug.Log("------- VREEL: PreviousPictures() called");

        int numImageSpheres = m_imageSpheres.GetLength(0);
        int numFilePaths = m_s3ImageFilePaths.Count;

        m_currS3ImageIndex = Mathf.Clamp(m_currS3ImageIndex - numImageSpheres, 0, numFilePaths);

        StartCoroutine(DownloadImagesAndSetSpheres(m_currS3ImageIndex, numImageSpheres));
    }

    /*
    private void StoreAllFilePaths(ResponseObject responseObject)   // WHATS THE TYPE HERE...!?
    {
        foreach (string filePath in System.IO.Directory.GetFiles(path))
        { 
            if (ImageExtensions.Contains(Path.GetExtension(filePath).ToUpperInvariant())) // Check that the file is indeed an image
            {                
                m_pictureFilePaths.Add(filePath);
            }
        }
    }
    */

    private void DownloadAllImagesInternal()
    {
        int numImageSpheres = m_imageSpheres.GetLength(0);
        StartCoroutine(DownloadImagesAndSetSpheres(m_currS3ImageIndex, numImageSpheres));
    }

    private IEnumerator DownloadImagesAndSetSpheres(int startingPictureIndex, int numImages)
    {
        Debug.Log(string.Format("------- VREEL: Downloading {0} pictures beginning at index {1}. There are {2} pictures in the S3 bucket!", 
            numImages, startingPictureIndex, m_s3ImageFilePaths.Count));

        int currPictureIndex = startingPictureIndex;
        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, currPictureIndex++)
        {
            if (currPictureIndex < m_s3ImageFilePaths.Count)
            {   
                Debug.Log("------- VREEL: Loop iteration: " + sphereIndex);
                string filePath = m_s3ImageFilePaths[currPictureIndex];
                DownloadImage(filePath, sphereIndex);
                Resources.UnloadUnusedAssets();
                yield return new WaitForSeconds(2.0f); // HACK to deal with the lack of asynchronous image loading...
            }
        }   
    }

    private void DownloadImage(string filePath, int sphereIndex)
    {
        string fullFilePath = m_s3BucketName + filePath;
        string logString = string.Format("------- VREEL: Fetching {0} from bucket {1}", filePath, m_s3BucketName);       
        Debug.Log(logString);

        m_s3Client.GetObjectAsync(m_s3BucketName, filePath, (s3ResponseObj) =>
        {               
            var response = s3ResponseObj.Response;
            if (response.ResponseStream != null)
            {   
                using (var stream = response.ResponseStream)
                {
                    byte[] myBinary = ToByteArray(stream);

                    Texture2D downloadedTexture = new Texture2D(2,2);
                    downloadedTexture.LoadImage(myBinary);

                    m_imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndPath(downloadedTexture, fullFilePath);
                    downloadedTexture = null;
                }

                Debug.Log("------- VREEL: Successfully downloaded and set " + fullFilePath);
            }
            else
            {
                Debug.Log("------- VREEL: Got an Exception downloading " + fullFilePath);
            }
        });
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