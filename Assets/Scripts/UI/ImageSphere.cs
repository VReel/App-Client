using UnityEngine;
using System;                        //IntPtrs
using System.Collections;            //IEnumerator

public class ImageSphere : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSphereSkybox;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;

    private int m_imageSphereIndex = -1; // ImageSphere's know their index - this is currently only for Debug!
    private int m_currTextureIndex = -1; // ImageSphere's track the index of the texture they are pointing to
    private Texture2D m_imageSphereTexture;
    private string m_imageFilePath; // All ImageSphere's have a path (either through the internet, or local to the device) associated with them!
    private CoroutineQueue m_coroutineQueue;

    private const float kMinShrink = 0.0005f; // Minimum value the sphere will shrink to...
    private const int kLoadingTextureIndex = -1;

    // TODO: Detelete these two strings below
    private const string kEmptyString = "emptyString";

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

    public void SetImageAndFilePath(Texture2D texture, string filePath, int textureIndex)
    {
        m_imageFilePath = filePath;
        m_imageSphereTexture = texture;

        Debug.Log("------- VREEL: Finished Loading Image from Texture2D, " +
            "FilePath = " + m_imageFilePath +
            " , PluginTextureIndex = " + textureIndex +
            " , Texture size = " + m_imageSphereTexture.width + " x " + m_imageSphereTexture.height);

        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(AnimateSetTexture(textureIndex));
    }

    public void SetImageAndFilePath(IntPtr texturePtr, string filePath, int textureIndex)
    {
        m_imageFilePath = filePath;
        m_imageSphereTexture.UpdateExternalTexture(texturePtr);

        Debug.Log("------- VREEL: Finished Loading Image from Texture2D, " +
            "FilePath = " + m_imageFilePath +
            " , PluginTextureIndex = " + textureIndex +
            " , TexturePtr = " + texturePtr);

        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(AnimateSetTexture(textureIndex));
    }

    public void Hide()
    {        
        m_imageFilePath = kEmptyString;

        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(AnimateHide());
    }

    public void ForceHide()
    {        
        m_imageFilePath = kEmptyString;

        m_coroutineQueue.Clear();
        transform.localScale = new Vector3(kMinShrink, kMinShrink, kMinShrink);
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator AnimateSetTexture(int textureIndex)
    {   
        Debug.Log("------- VREEL: AnimateSetTexture() began on sphere: " + (m_imageSphereIndex+1) );

        float scalingFactor = m_imageSphereController.GetScalingFactor();
        float defaultScale = m_imageSphereController.GetDefaultSphereScale();

        // Scale down
        while (transform.localScale.magnitude > kMinShrink)
        {
            transform.localScale = transform.localScale * scalingFactor;
            yield return new WaitForEndOfFrame();
        }

        // Set texture and textureID
        gameObject.GetComponent<MeshRenderer>().material.mainTexture = m_imageSphereTexture;

        m_imageSphereController.SetTextureInUse(m_currTextureIndex, false);
        m_currTextureIndex = textureIndex;
        m_imageSphereController.SetTextureInUse(m_currTextureIndex, true);

        // Scale up
        while (transform.localScale.magnitude < defaultScale)
        {
            transform.localScale = transform.localScale / scalingFactor;
            yield return new WaitForEndOfFrame();
        }

        transform.localScale = new Vector3(defaultScale, defaultScale, defaultScale);
    }

    private IEnumerator AnimateHide()
    {        
        Debug.Log("------- VREEL: AnimateHide() called on sphere: " + (m_imageSphereIndex+1));

        float scalingFactor = m_imageSphereController.GetScalingFactor();

        while (transform.localScale.magnitude > kMinShrink)
        {
            transform.localScale = transform.localScale * scalingFactor;
            yield return new WaitForEndOfFrame();
        }

        m_imageSphereController.SetTextureInUse(m_currTextureIndex, false);
        m_currTextureIndex = kLoadingTextureIndex;
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
        Debug.Log("------- VREEL: SetSkybox() got called on sphere: " + (m_imageSphereIndex+1));

        if (m_imageSphereSkybox != null && m_imageSphereSkybox.GetImageFilePath() != m_imageFilePath && m_currTextureIndex != kLoadingTextureIndex)
        {            
            m_imageSphereSkybox.SetImageAndPath(m_imageSphereTexture, m_imageFilePath, m_currTextureIndex);
        }
    }
}