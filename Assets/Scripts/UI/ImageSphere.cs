using UnityEngine;
using UnityEngine.UI;                //Text
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
    [SerializeField] private GameObject m_handleObject;
    [SerializeField] private GameObject m_likesObject;
    [SerializeField] private GameObject m_captionObject;

    private const float kMinShrink = 0.0005f; // Minimum value the sphere will shrink to...
    private const int kLoadingTextureIndex = -1;

    private string m_imageIdentifier; // This is either (1) A Local Path on the Device or (2) A PostID from the backend
    private Texture2D m_imageSphereTexture;
    private string m_handle;
    private string m_caption;
    private int m_likes;

    private int m_imageSphereIndex = -1; // ImageSphere's know their index into the ImageSphereController - this is currently only for Debug!
    private int m_currTextureIndex = -1; // ImageSphere's track the index of the texture they are pointing to
    private int m_nextTextureIndex = -1; // ImageSphere's also track the index of the next texture they will point to - necessary because we don't swap textures immediately
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

    public string GetImageIdentifier()
    {
        return m_imageIdentifier;
    }

    public void SetImage(Texture2D texture, string imageIdentifier, int textureIndex, bool animateOnSet)
    {
        m_imageSphereController.SetTextureInUse(m_nextTextureIndex, false); // Textures that were going to be used can be thrown away
        m_imageSphereController.SetTextureInUse(textureIndex, true);
        m_nextTextureIndex = textureIndex;

        m_imageIdentifier = imageIdentifier;
        m_imageSphereTexture = texture;

        if (Debug.isDebugBuild) 
            Debug.Log("------- VREEL: Finished Loading Image from Texture2D, " +
                " AnimateOnSet = " + animateOnSet +
                " , ImageIdentifier = " + imageIdentifier +
                " , PluginTextureIndex = " + textureIndex +
                " , Texture size = " + m_imageSphereTexture.width + " x " + m_imageSphereTexture.height);

        m_coroutineQueue.Clear();
        if (!animateOnSet)
        {
            UpdateTextureAndID();
        }
        else
        {
            m_coroutineQueue.EnqueueAction(AnimateSetTexture());
        }
    }

    public void SetMetadata(string handle, string caption, int likes) 
    { //NOTE: These are only set onto their UI elements when the Animation has ended!
        m_handle = handle;
        m_caption = caption;
        m_likes = likes;
    }

    public void Hide()
    {        
        m_imageIdentifier = "";
        m_handle = "";
        m_caption = "";
        m_likes = -1;

        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(AnimateHide());
    }

    public void ForceHide()
    {        
        m_imageIdentifier = "";
        m_handle = "";
        m_caption = "";
        m_likes = -1;

        UpdateMetadata();

        m_coroutineQueue.Clear();
        transform.localScale = new Vector3(kMinShrink, kMinShrink, kMinShrink);
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void UpdateTextureAndID()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: UpdateTextureAndID() called on sphere: " + (m_imageSphereIndex+1) );

        gameObject.GetComponent<MeshRenderer>().material.mainTexture = m_imageSphereTexture;

        m_imageSphereController.SetTextureInUse(m_currTextureIndex, false);
        m_currTextureIndex = m_nextTextureIndex;
        m_nextTextureIndex = kLoadingTextureIndex;
    }

    private void UpdateMetadata()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: UpdateMetada() called on sphere: " + (m_imageSphereIndex+1) );

        m_handleObject.SetActive(m_handle.Length > 0);
        m_handleObject.GetComponentInChildren<Text>().text = m_handle;

        m_captionObject.SetActive(m_caption.Length > 0);
        m_captionObject.GetComponentInChildren<Text>().text = m_caption;

        m_likesObject.SetActive(m_likes >= 0);
        m_likesObject.GetComponentInChildren<Text>().text = m_likes.ToString();
    }

    private void HideMetadata()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: HideMetadata() called on sphere");

        m_handleObject.SetActive(false);
        m_captionObject.SetActive(false);
        m_likesObject.SetActive(false);
    }

    private IEnumerator AnimateSetTexture()
    {   
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: AnimateSetTexture() began on sphere: " + (m_imageSphereIndex+1) );

        HideMetadata();

        float scalingFactor = m_imageSphereController.GetScalingFactor();
        float defaultScale = m_imageSphereController.GetDefaultSphereScale();

        // Scale down
        while (transform.localScale.magnitude > kMinShrink)
        {
            transform.localScale = transform.localScale * scalingFactor;
            yield return null;
        }

        // Set texture and textureID
        UpdateTextureAndID();

        // Scale up
        while (transform.localScale.magnitude < defaultScale)
        {
            transform.localScale = transform.localScale / scalingFactor;
            yield return null;
        }

        UpdateMetadata();

        transform.localScale = new Vector3(defaultScale, defaultScale, defaultScale);
    }

    private IEnumerator AnimateHide()
    {        
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: AnimateHide() called on sphere: " + (m_imageSphereIndex+1));

        HideMetadata();

        float scalingFactor = m_imageSphereController.GetScalingFactor();

        while (transform.localScale.magnitude > kMinShrink)
        {
            transform.localScale = transform.localScale * scalingFactor;
            yield return null;
        }

        UpdateMetadata();

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
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: SetSkybox() got called on sphere: " + (m_imageSphereIndex+1));

        if (m_imageSphereSkybox != null && (m_imageSphereSkybox.GetImageIdentifier().CompareTo(m_imageIdentifier) != 0) && m_currTextureIndex != kLoadingTextureIndex)
        {            
            m_imageSphereSkybox.SetImage(m_imageSphereTexture, m_imageIdentifier, m_currTextureIndex);
        }
    }
}