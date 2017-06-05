using UnityEngine;
using UnityEngine.UI;                //Text
using System;                        //IntPtrs
using System.Collections;            //IEnumerator

public class ImageSphere : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private Search m_search;
    [SerializeField] private Posts m_posts;
    [SerializeField] private ListUsers m_listUsers;
    [SerializeField] private ListComments m_listComments;
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private Profile m_profile;
    [SerializeField] private ProfileDetails m_profileDetails;
    [SerializeField] private ImageSphereController m_imageSphereController;
    [SerializeField] private ImageSkybox m_imageSphereSkybox;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;
    [SerializeField] private GameObject m_imageObject;
    [SerializeField] private GameObject m_handleImageObject;
    [SerializeField] private GameObject m_handleObject;
    [SerializeField] private GameObject m_heartObject;
    [SerializeField] private GameObject m_likesObject;
    [SerializeField] private GameObject m_captionObject;
    [SerializeField] private GameObject m_commentCountObject;
    [SerializeField] private bool m_isSmallImageSphere;

    private const float kMinShrink = 0.0005f; // Minimum value the sphere will shrink to...
    private const int kLoadingTextureIndex = -1;

    private string m_imageIdentifier; // This is either (1) A Local Path on the Device or (2) A PostID from the backend
    private string m_workingImageIdentifier; // This is in order to allow the ImageIdentifier to change midway through the TextureSettingAnimation
    private Texture2D m_imageSphereTexture;
    private string m_userId;
    private string m_handle;
    private string m_caption;
    private int m_commentCount;
    private int m_numLikes;
    private bool m_heartOn;

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

    public void SetMetadata(string userId, string handle, string caption, int commentCount, int likes, bool likedByMe, bool updateMetadata = false) 
    {   //NOTE: These are only set onto their UI elements when the Animation has ended!
        m_userId = userId;
        m_handle = handle;
        m_caption = caption;
        m_commentCount = commentCount;
        m_numLikes = likes;
        m_heartOn = likedByMe;

        if (updateMetadata)
        {
            UpdateMetadata();
        }
    }

    public void SetMetadataToEmpty(bool updateMetadata = false)
    {        
        m_handle = "";
        m_caption = "";
        m_commentCount = -1;
        m_numLikes = -1;
        m_heartOn = false;

        if (updateMetadata)
        {
            UpdateMetadata();
        }
    }

    public void AddToCommentCount(int addValue)
    {
        m_commentCount += addValue;
        m_commentCountObject.GetComponentInChildren<Text>().text = (m_commentCount + 1).ToString(); //Adding 1 for Caption itself
    }

    public void Hide()
    {        
        SetMetadataToEmpty();

        if (m_coroutineQueue != null)
        {
            m_coroutineQueue.Clear();
            m_coroutineQueue.EnqueueAction(AnimateHide());
        }
    }

    public void ForceHide()
    {        
        SetMetadataToEmpty();
        UpdateMetadata();

        if (m_coroutineQueue != null)
        {
            m_coroutineQueue.Clear();
        }
        m_imageObject.transform.localScale = new Vector3(kMinShrink, kMinShrink, kMinShrink);
    }

    public void HandleSelected()
    {
        m_profile.OpenProfileWithId(m_userId, m_handle);
    }

    public void HeartSelected()
    {
        m_heartOn = !m_heartOn;
        m_numLikes = m_heartOn ? m_numLikes+1 : m_numLikes-1;

        m_heartObject.GetComponentInChildren<SelectedButton>().ButtonSelected(m_heartOn);
        m_likesObject.GetComponentInChildren<Text>().text = m_numLikes.ToString();
        m_posts.LikeOrUnlikePost(m_imageIdentifier, m_heartOn);
    }

    public void LikesSelected()
    {
        m_listUsers.DisplayLikeResults(m_imageIdentifier);
    }

    public void CaptionSelected()
    {
        m_listComments.DisplayCommentResults(m_imageIdentifier, this);
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void UpdateTextureAndID()
    {
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: UpdateTextureAndID() called on sphere: " + (m_imageSphereIndex+1) );

        m_imageObject.GetComponent<MeshRenderer>().material.mainTexture = m_imageSphereTexture;

        m_imageSphereController.SetTextureInUse(m_currTextureIndex, false);
        m_currTextureIndex = m_nextTextureIndex;
        m_nextTextureIndex = kLoadingTextureIndex;

        m_imageObject.GetComponent<ImageSphereAnimation>().SetActive(true);
    }

    private void UpdateMetadata()
    {
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: UpdateMetada() called on sphere: " + (m_imageSphereIndex+1) );

        bool isSphereLoading = m_currTextureIndex == kLoadingTextureIndex;

        if (m_handleImageObject != null)
        {
            m_handleImageObject.SetActive(false);
            //m_handleImageObject.SetActive(!isSphereLoading && m_handle.Length > 0 && !m_posts.IsProfileType());
            //TODO: Appropriately load image...
        }

        if (m_handleObject != null)
        {
            m_handleObject.SetActive(!isSphereLoading && m_handle.Length > 0 && !m_posts.IsProfileType());
            m_handleObject.GetComponentInChildren<Text>().text = m_handle;
        }

        if (m_captionObject != null)
        {
            m_captionObject.SetActive(!isSphereLoading && m_caption.Length > 0);
            m_captionObject.GetComponentInChildren<Text>().text = m_caption;
        }

        if (m_commentCountObject != null)
        {
            m_commentCountObject.SetActive(!isSphereLoading && m_caption.Length > 0);
            m_commentCountObject.GetComponentInChildren<Text>().text = (m_commentCount + 1).ToString(); //Adding 1 for Caption itself
        }

        if (m_likesObject != null)
        {
            m_likesObject.SetActive(!isSphereLoading && m_numLikes >= 0);
            m_likesObject.GetComponentInChildren<Text>().text = m_numLikes.ToString();
        }

        if (m_heartObject != null)
        {
            m_heartObject.SetActive(!isSphereLoading && m_numLikes >= 0);
            m_heartObject.GetComponentInChildren<SelectedButton>().ButtonSelected(m_heartOn);
        }
          
        if (m_menuController != null)
        {
            bool isVisible = m_menuController.IsMenuActive() && m_imageObject.GetComponent<MeshRenderer>().enabled;
            EnableAllInteractableComponentsInObject(m_handleImageObject, isVisible);
            EnableAllInteractableComponentsInObject(m_handleObject, isVisible);
            EnableAllInteractableComponentsInObject(m_captionObject, isVisible);
            EnableAllInteractableComponentsInObject(m_commentCountObject, isVisible);
            EnableAllInteractableComponentsInObject(m_likesObject, isVisible);
            EnableAllInteractableComponentsInObject(m_heartObject, isVisible);
        }
    }

    private void HideMetadata()
    {
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: HideMetadata() called on sphere");

        if (m_handleImageObject != null) m_handleImageObject.SetActive(false);
        if (m_handleObject != null) m_handleObject.SetActive(false);
        if (m_captionObject != null) m_captionObject.SetActive(false);
        if (m_commentCountObject != null) m_commentCountObject.SetActive(false);
        if (m_likesObject != null) m_likesObject.SetActive(false);
        if (m_heartObject != null) m_heartObject.SetActive(false);
    }

    private void EnableAllInteractableComponentsInObject(GameObject gameObject, bool enable)
    {
        if (gameObject != null)
        {
            foreach(var renderer in gameObject.GetComponentsInChildren<Renderer>())
            {                
                renderer.enabled = enable; // Handles Mesh + SpriteRenderer components
            }

            foreach(var ui in gameObject.GetComponentsInChildren<UnityEngine.UI.Graphic>())
            {                
                ui.enabled = enable; // Handles Images + Text components
            }

            foreach(var collider in gameObject.GetComponentsInChildren<Collider>())
            {                
                collider.enabled = enable; // Handles BoxCollider + MeshCollider components
            }
        }
    }

    private IEnumerator AnimateSetTexture()
    {   
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: AnimateSetTexture() began on sphere: " + (m_imageSphereIndex+1) );

        HideMetadata();

        float kScalingFactor = m_imageSphereController.GetScalingFactor();
        float kMaxScale = m_isSmallImageSphere ? m_imageSphereController.GetSmallSphereScale() : m_imageSphereController.GetDefaultSphereScale();

        // Scale down
        while (m_imageObject.transform.localScale.magnitude > kMinShrink)
        {
            m_imageObject.transform.localScale = m_imageObject.transform.localScale * kScalingFactor;
            yield return null;
        }

        // Set texture and textureID
        UpdateTextureAndID();

        // Scale up
        while (m_imageObject.transform.localScale.magnitude < kMaxScale)
        {
            m_imageObject.transform.localScale = m_imageObject.transform.localScale / kScalingFactor;
            yield return null;
        }

        UpdateMetadata();

        m_imageObject.transform.localScale = new Vector3(kMaxScale, kMaxScale, kMaxScale);
    }

    private IEnumerator AnimateHide()
    {        
        //if (Debug.isDebugBuild) Debug.Log("------- VREEL: AnimateHide() called on sphere: " + (m_imageSphereIndex+1));

        HideMetadata();

        float scalingFactor = m_imageSphereController.GetScalingFactor();

        while (m_imageObject.transform.localScale.magnitude > kMinShrink)
        {
            m_imageObject.transform.localScale = m_imageObject.transform.localScale * scalingFactor;
            yield return null;
        }

        UpdateMetadata();

        m_imageSphereController.SetTextureInUse(m_currTextureIndex, false);
        m_currTextureIndex = kLoadingTextureIndex;
        m_imageIdentifier = "";
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
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OnButtonSelected() called on sphere: " + (m_imageSphereIndex+1));

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OnButtonSelected() (m_imageIdentifier.Length > 0): " + (m_imageIdentifier.Length > 0));

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OnButtonSelected() (m_imageSphereSkybox.GetImageIdentifier().CompareTo(m_imageIdentifier) != 0): " + (m_imageSphereSkybox.GetImageIdentifier().CompareTo(m_imageIdentifier) != 0));

        if (Debug.isDebugBuild) Debug.Log("------- VREEL: OnButtonSelected() (m_currTextureIndex != kLoadingTextureIndex): " + (m_currTextureIndex != kLoadingTextureIndex));

        bool willChangeImage = (m_imageIdentifier.Length > 0) && (m_imageSphereSkybox.GetImageIdentifier().CompareTo(m_imageIdentifier) != 0) && (m_currTextureIndex != kLoadingTextureIndex);
        if (!willChangeImage)
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: OnButtonSelected() will not ChangeImage");
            return;
        }

        m_workingImageIdentifier = m_imageIdentifier;
        const bool kAnimationEffectOn = true;
        if (kAnimationEffectOn)
        {
            m_coroutineQueue.EnqueueAction(AnimateSphereTowardsUser(m_workingImageIdentifier));
        }
        else
        {
            SetOriginalImageOnSkybox(m_workingImageIdentifier);
            SetSkybox(m_workingImageIdentifier);
        }            
    }

    private IEnumerator AnimateSphereTowardsUser(string imageIdentifier)
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: AnimateSphereTowardsUser() called on sphere: " + (m_imageSphereIndex+1));

        SetOriginalImageOnSkybox(imageIdentifier);
        m_imageObject.GetComponent<ImageSphereAnimation>().SetActive(false);
        m_imageObject.GetComponent<Collider>().enabled = false;

        float kScalingFactor = m_imageSphereController.GetScalingFactor();
        float kMaxScale = m_isSmallImageSphere ? m_imageSphereController.GetSmallSphereScale() : m_imageSphereController.GetDefaultSphereScale();

        Vector3 initialPos = m_imageObject.transform.position;
        Vector3 userPos = Camera.main.gameObject.transform.position;
        Quaternion initialRot = m_imageObject.transform.rotation;
        Quaternion skyboxRot = m_imageSphereSkybox.transform.rotation;
        Vector3 initialScale = new Vector3(kMaxScale, kMaxScale, kMaxScale);
        Vector3 skyboxScale = m_imageSphereSkybox.transform.localScale;

        // Travel to user and partially scale
        const float kPercentageRotationDuration = 0.3f;
        const float kSecondsToGetToUser = 3.0f;
        float progress = 0;
        while (progress <= 1)
        {
            if (progress < kPercentageRotationDuration) // All Rotation needs to have taken place before it reaches your face!
            {
                m_imageObject.transform.rotation = Quaternion.Slerp(initialRot, skyboxRot, progress/kPercentageRotationDuration); 
            }
            else
            {
                m_imageObject.transform.rotation = Quaternion.Slerp(initialRot, skyboxRot, 1.0f); 
            }

            m_imageObject.transform.position = Vector3.Lerp(initialPos, userPos, progress);
            m_imageObject.transform.localScale = Vector3.Lerp(initialScale, skyboxScale/2.0f, ExponentialProgress(progress));
            progress += Time.deltaTime / kSecondsToGetToUser;
            yield return null;
        }

        // When Image has reached the user then SetSkybox to Thumbnail and grow the sphere
        m_imageObject.transform.position = userPos;
        SetSkybox(imageIdentifier);

        // Scale to size of SkyBox
        const float kSecondsToGetToGrowFully = 1.0f;
        progress = 0;
        while (progress <= 1)
        {            
            m_imageObject.transform.localScale = Vector3.Lerp(skyboxScale/2.0f, skyboxScale, progress);
            progress += Time.deltaTime / kSecondsToGetToGrowFully;
            yield return null;
        }

        m_imageObject.transform.position = userPos;
        m_imageObject.transform.localScale = skyboxScale;

        yield return null;

        // When Sphere has grown reset it
        m_imageObject.transform.position = initialPos;
        m_imageObject.transform.rotation = initialRot;
        m_imageObject.transform.localScale = new Vector3(kMinShrink, kMinShrink, kMinShrink);

        // Scale up the newly appeared ImageSphere
        while (m_imageObject.transform.localScale.magnitude < kMaxScale)
        {
            m_imageObject.transform.localScale = m_imageObject.transform.localScale / kScalingFactor;
            yield return null;
        }

        m_imageObject.transform.localScale = initialScale;
        m_imageObject.GetComponent<ImageSphereAnimation>().SetActive(true);
        m_imageObject.GetComponent<Collider>().enabled = true;
    }

    private float ExponentialProgress(float progress)
    {
        // f(x) = e^(1−(1/x^2))
        const float kA = 100;
        float result = (Mathf.Pow(kA, progress) - 1)/(kA - 1);
        return result;
    }

    private void SetOriginalImageOnSkybox(string imageIdentifier)
    {                
        bool isImageFromDevice = imageIdentifier.StartsWith(m_imageSphereController.GetTopLevelDirectory());
        if (!isImageFromDevice)
        {
            if (m_isSmallImageSphere) // Only small image Spheres query ProfileDetails...
            {
                bool isLoggedUserImage = m_profileDetails.IsLoggedUser(imageIdentifier);
                bool isProfilePageImage = m_profileDetails.IsUser(imageIdentifier); 
                if (isProfilePageImage) // Image Identifier is of the User for Profile Pictures
                {
                    m_profileDetails.DownloadOriginalImage();
                }
                else if (isLoggedUserImage)
                {
                    m_profileDetails.DownloadMenuBarOriginalImage();
                }
            }
            else
            {
                m_posts.DownloadOriginalImage(imageIdentifier);
            }
        }
    }

    private void SetSkybox(string imageIdentifier)
    {    
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: SetSkybox() got called on sphere: " + (m_imageSphereIndex+1));

        if (m_imageSphereSkybox != null)
        {            
            m_imageSphereSkybox.SetImage(m_imageSphereTexture, imageIdentifier, m_currTextureIndex);
        }
    }
}