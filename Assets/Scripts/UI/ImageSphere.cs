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

    private const float kMinShrink = 0.005f; // Minimum value the sphere will shrink to...
    private const string kEmptyString = "emptyString";
    private int m_imageSphereIndex = -1; // ImageSphere's know their index - this is currently only for Debug!
    private Texture2D m_imageSphereTexture;
    private string m_imageFilePath; // All ImageSphere's have a path (either through the internet, or local to the device) associated with them!
    private CoroutineQueue m_coroutineQueue;

    // **************************
    // Public functions
    // **************************

    public void Awake()
    {
        m_imageSphereTexture = new Texture2D(2,2); // TODO Create a default texture for loading showing the VReel logo
        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();
    }

    public void SetSphereIndex(int imageSphereIndex)
    {
        m_imageSphereIndex = imageSphereIndex;
    }

    public string GetImageFilePath()
    {
        return m_imageFilePath;
    }        

    public void SetImageAndFilePath(Texture2D texture, string filePath)
    {
        m_imageFilePath = filePath;
        m_imageSphereTexture = texture;

        Debug.Log("------- VREEL: Finished Loading Image from Texture2D, about to set texture with width x height:  " + m_imageSphereTexture.width + " x " + m_imageSphereTexture.height);

        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(AnimateSetTexture());
    }

    /*
    public void SetImageAndFilePath(byte[] textureStream, string filePath)
    {
        m_imageFilePath = filePath;
        m_imageSphereTexture.LoadImage(textureStream);

        Debug.Log("------- VREEL: Finished Loading Image from TextureStream, about to set texture with width x height:  " + m_imageSphereTexture.width + " x " + m_imageSphereTexture.height);

        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(AnimateSetTexture());
    }

    public void SetImageAndFilePath(ref WWW www, string filePath)
    {
        m_imageFilePath = filePath;
        www.LoadImageIntoTexture(m_imageSphereTexture);

        Debug.Log("------- VREEL: Finished Loading Image from WWW, about to set texture with width x height:  " + m_imageSphereTexture.width + " x " + m_imageSphereTexture.height);

        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(AnimateSetTexture());
    }
    */

    public void Hide()
    {
        m_imageFilePath = kEmptyString;

        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(AnimateHide());
    }

    public void ForceHide()
    {
        m_imageFilePath = kEmptyString;

        transform.localScale = new Vector3(kMinShrink, kMinShrink, kMinShrink);
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator AnimateSetTexture()
    {   
        Debug.Log("------- VREEL: AnimateSetTexture() began on sphere: " + m_imageSphereIndex);

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
        Debug.Log("------- VREEL: AnimateHide() called on sphere: " + m_imageSphereIndex);

        float scalingFactor = m_imageSphereController.GetScalingFactor();

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
        Debug.Log("------- VREEL: SetSkybox() got called on sphere: " + m_imageSphereIndex);

        if (m_imageSphereSkybox != null)
        {            
            m_imageSphereSkybox.SetImageAndPath(m_imageSphereTexture, m_imageFilePath);
        }
    }
}