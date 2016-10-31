using UnityEngine;
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
    private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };

    private bool alreadyRan = false; 

	void Start () 
	{
		UnityInitializer.AttachToGameObject(this.gameObject);

        m_credentials = new CognitoAWSCredentials (
            "eu-west-1:1f9f6bd1-3cfe-43c2-afbc-3e06d8d1fe27", // Identity Pool ID
            RegionEndpoint.EUWest1 // Region
        );
        m_s3Client = new AmazonS3Client (m_credentials, RegionEndpoint.EUWest1);
	}

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

        int currImageSphere = 0;
        m_s3Client.ListObjectsAsync(request, (responseObject) =>
        {
            if (responseObject.Exception == null)
            {
                Debug.Log("------- VREEL: Got Response \nPrinting now \n");
                responseObject.Response.S3Objects.ForEach((s3object) =>
                {
                    if (ImageExtensions.Contains(Path.GetExtension(s3object.Key).ToUpperInvariant())) // Check that the file is indeed an image
                    {                        
                        DownloadImage(s3object.Key, m_imageSpheres[currImageSphere]); // Set this image onto one of the imageSpheres
                        currImageSphere++;
                        Debug.Log("------- VREEL:" + s3object.Key);
                    }                        
                });
            }
            else
            {
                Debug.Log("------- VREEL: Got Exception");
            }
        });
    }

    public void DownloadImage(string fileName, GameObject resultSphere)
    {
        string fullFileName = m_s3BucketName + fileName;
        string logString = string.Format("------- VREEL: Fetching {0} from bucket {1}", fileName, m_s3BucketName);       
        Debug.Log(logString);

        m_s3Client.GetObjectAsync(m_s3BucketName, fileName, (s3ResponseObj) =>
        {               
            var response = s3ResponseObj.Response;
            if (response.ResponseStream != null)
            {   
                using (var stream = response.ResponseStream)
                {
                    byte[] myBinary = ToByteArray(stream);

                    Texture2D downloadedTexture = new Texture2D(2,2);
                    downloadedTexture.LoadImage(myBinary);

                    resultSphere.GetComponent<SelectImage>().SetImageAndPath(downloadedTexture, fullFileName);
                }

                Debug.Log("------- VREEL: Success");
            }
            else
            {
                Debug.Log("------- VREEL: Got Exception");
            }
        });
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