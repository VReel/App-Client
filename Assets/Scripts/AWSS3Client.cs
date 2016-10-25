using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using Amazon;                   // UnityInitializer
using Amazon.CognitoIdentity;   // CognitoAWSCredentials
using Amazon.S3;                // AmazonS3Client
using Amazon.S3.Model;          // ListBucketsRequest
using Amazon.S3.Util;
/*
using Amazon.Runtime;
*/

public class AWSS3Client : MonoBehaviour 
{
    public string m_s3BucketName = null;
    public string m_sampleFileName = null;
    public Text m_resultText;
    public GameObject m_resultSphere;

    private AmazonS3Client m_s3Client = null;
    private CognitoAWSCredentials m_credentials = null;

	void Start () 
	{
		UnityInitializer.AttachToGameObject(this.gameObject);

        m_credentials = new CognitoAWSCredentials (
            "eu-west-1:1f9f6bd1-3cfe-43c2-afbc-3e06d8d1fe27", // Identity Pool ID
            RegionEndpoint.EUWest1 // Region
        );
        m_s3Client = new AmazonS3Client (m_credentials, RegionEndpoint.EUWest1);
	}

    public void OnMouseDown()
    {
        GetObject();
    }
	
    public void GetObjects()
    {
        m_resultText.text = "Fetching all the Objects from " + m_s3BucketName;

        var request = new ListObjectsRequest()
        {
            BucketName = m_s3BucketName
        };

        m_s3Client.ListObjectsAsync(request, (responseObject) =>
        {
            m_resultText.text += "\n";
            if (responseObject.Exception == null)
            {
                m_resultText.text += "Got Response \nPrinting now \n";
                responseObject.Response.S3Objects.ForEach((o) =>
                {
                    m_resultText.text += string.Format("{0}\n", o.Key);
                });
            }
            else
            {
                m_resultText.text += "Got Exception \n";
            }
        });
    }

    public void GetObject()
    {
        m_resultText.text = string.Format("fetching {0} from bucket {1}", m_s3BucketName, m_sampleFileName);
        m_s3Client.GetObjectAsync(m_s3BucketName, m_sampleFileName, (responseObj) =>
        {   
            m_resultText.text += "\n";
            var response = responseObj.Response;
            if (response.ResponseStream != null)
            {   
                using (var stream = response.ResponseStream)
                {
                    byte[] myBinary = ToByteArray(stream);

                    Texture2D texture2d = new Texture2D(2,2);

                    texture2d.LoadImage(myBinary);

                    m_resultSphere.GetComponent<MeshRenderer>().material.mainTexture = texture2d;
                }

                m_resultText.text += "Success";
            }
            else
            {
                m_resultText.text += "Got Exception";
            }
        });
    }

    /*
    private byte[] ToByteArray2(Stream stream)
    {
        stream.Position = 0;
        byte[] buffer = new byte[stream.Length];
        for (int totalBytesCopied = 0; totalBytesCopied < stream.Length; )
        {
            totalBytesCopied += stream.Read(buffer, totalBytesCopied, Convert.ToInt32(stream.Length) - totalBytesCopied);
        }
        return buffer;
    }
    */

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