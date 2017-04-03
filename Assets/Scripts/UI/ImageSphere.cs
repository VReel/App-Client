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
    private int m_nextTextureIndex = -1; // ImageSphere's also track the index of the next texture they will point to - necessary because we don't swap textures immediately
    private Texture2D m_imageSphereTexture;
    private string m_imageIdentifier; // This is either (1) A Local Path on the Device or (2) A PostID from the backend
    private CoroutineQueue m_coroutineQueue;

    private const float kMinShrink = 0.0005f; // Minimum value the sphere will shrink to...
    private const int kLoadingTextureIndex = -1;

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
            SetTextureAndID();
        }
        else
        {
            m_coroutineQueue.EnqueueAction(AnimateSetTexture());
        }
    }

    public void Hide()
    {        
        m_imageIdentifier = "";

        m_coroutineQueue.Clear();
        m_coroutineQueue.EnqueueAction(AnimateHide());
    }

    public void ForceHide()
    {        
        m_imageIdentifier = "";

        m_coroutineQueue.Clear();
        transform.localScale = new Vector3(kMinShrink, kMinShrink, kMinShrink);
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void SetTextureAndID()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: SetTextureAndID() called on sphere: " + (m_imageSphereIndex+1) );

        gameObject.GetComponent<MeshRenderer>().material.mainTexture = m_imageSphereTexture;

        m_imageSphereController.SetTextureInUse(m_currTextureIndex, false);
        m_currTextureIndex = m_nextTextureIndex;
        m_nextTextureIndex = kLoadingTextureIndex;
    }

    private IEnumerator AnimateSetTexture()
    {   
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: AnimateSetTexture() began on sphere: " + (m_imageSphereIndex+1) );

        float scalingFactor = m_imageSphereController.GetScalingFactor();
        float defaultScale = m_imageSphereController.GetDefaultSphereScale();

        // Scale down
        while (transform.localScale.magnitude > kMinShrink)
        {
            transform.localScale = transform.localScale * scalingFactor;
            yield return null;
        }

        // Set texture and textureID
        SetTextureAndID();

        // Scale up
        while (transform.localScale.magnitude < defaultScale)
        {
            transform.localScale = transform.localScale / scalingFactor;
            yield return null;
        }

        transform.localScale = new Vector3(defaultScale, defaultScale, defaultScale);
    }

    private IEnumerator AnimateHide()
    {        
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: AnimateHide() called on sphere: " + (m_imageSphereIndex+1));

        float scalingFactor = m_imageSphereController.GetScalingFactor();

        while (transform.localScale.magnitude > kMinShrink)
        {
            transform.localScale = transform.localScale * scalingFactor;
            yield return null;
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
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: SetSkybox() got called on sphere: " + (m_imageSphereIndex+1));

        if (m_imageSphereSkybox != null && (m_imageSphereSkybox.GetImageIdentifier().CompareTo(m_imageIdentifier) != 0) && m_currTextureIndex != kLoadingTextureIndex)
        {            
            m_imageSphereSkybox.SetImage(m_imageSphereTexture, m_imageIdentifier, m_currTextureIndex);
        }
    }
}