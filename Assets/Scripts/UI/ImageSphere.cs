using UnityEngine;
using System.Collections;            //IEnumerator

public class ImageSphere : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSphereSkybox;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;

    private Texture2D m_imageSphereTexture;
    private string m_imageFilePath; // All ImageSphere's have a path (either through the internet, or local to the device) associated with them!
    private string kEmptyString = "emptyString";
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Awake()
    {
        m_imageSphereTexture = new Texture2D(2,2); // TODO Create a default texture for loading showing the VReel logo
        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();
    }

    public string GetImageFilePath()
    {
        return m_imageFilePath;
    }

    public void SetImageAndFilePath(byte[] textureStream, string filePath)
    {
        m_imageFilePath = filePath;
        m_imageSphereTexture.LoadImage(textureStream);

        Debug.Log("------- VREEL: Finished Loading Image, texture width x height:  " + m_imageSphereTexture.width + " x " + m_imageSphereTexture.height);

        StartCoroutine(AnimateSetTexture());
    }

    public void SetImageAndFilePath(Texture2D texture, string filePath)
    {
        m_imageFilePath = filePath;
        m_imageSphereTexture = texture;

        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(AnimateSetTexture());
    }

    public void SetImageAndFilePath(ref WWW www, string filePath)
    {
        m_imageFilePath = filePath;
        www.LoadImageIntoTexture(m_imageSphereTexture);

        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(AnimateSetTexture());
    }

    public void Hide()
    {
        m_imageFilePath = kEmptyString;

        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(AnimateHide());
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator AnimateSetTexture()
    {   
        const float kMinShrink = 0.05f; // Minimum value you the sphere can shrink to...
        float scalingFactor = m_imageSphereController.GetScalingFactor();
        float defaultScale = m_imageSphereController.GetDefaultSphereScale();

        while (transform.localScale.magnitude > kMinShrink)
        {
            transform.localScale = transform.localScale * scalingFactor;
            yield return new WaitForEndOfFrame();
        }

        gameObject.GetComponent<MeshRenderer>().material.mainTexture = m_imageSphereTexture;

        while (transform.localScale.magnitude < defaultScale)
        {
            transform.localScale = transform.localScale / scalingFactor;
            yield return new WaitForEndOfFrame();
        }

        transform.localScale = new Vector3(defaultScale, defaultScale, defaultScale);
    }

    private IEnumerator AnimateHide()
    {        
        float scalingFactor = m_imageSphereController.GetScalingFactor();

        const float kMinShrink = 0.005f; // Minimum value you the sphere can shrink to...
        while (transform.localScale.magnitude > kMinShrink)
        {
            transform.localScale = transform.localScale * scalingFactor;
            yield return new WaitForEndOfFrame();
        }
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
        SetSkybox();
    }

    private void SetSkybox()
    {    
        if (m_imageSphereSkybox != null)
        {            
            m_imageSphereSkybox.SetImageAndPath(m_imageSphereTexture, m_imageFilePath);
        }
    }
}