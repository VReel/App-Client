using UnityEngine;
using UnityEngine.UI;

public class SelectArrow : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************
    
    public enum ArrowType
    {
        kPrev,
        kNext
    };
        
    [SerializeField] private MeshCollider m_meshCollider;
    [SerializeField] private Image m_arrowImage;
    [SerializeField] private Image m_transparentBackgroundImage;
    [SerializeField] private ArrowType m_arrowType = ArrowType.kNext;
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private DeviceGallery m_deviceGallery;
    [SerializeField] private AWSS3Client m_awsS3Client;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;

    // **************************
    // Public functions
    // **************************

    public void Update() //TODO: Remove this Update and make it event based!
    {
        UpdateActive();
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void UpdateActive()
    {
        bool shouldBeActive = ShouldBeActive();
        m_meshCollider.enabled = shouldBeActive;
        m_arrowImage.enabled = shouldBeActive;
        m_transparentBackgroundImage.enabled = shouldBeActive;
    }

    private bool ShouldBeActive()
    {
        if (m_menuController != null)
        {
            if (!m_menuController.GetMenuActive())
            {
                return false;
            }
        }

        if (m_deviceGallery != null)
        {
            if (m_arrowType == ArrowType.kNext)
            {
                return !m_deviceGallery.IsGalleryIndexAtEnd();
            }
            else if (m_arrowType == ArrowType.kPrev)
            {
                return !m_deviceGallery.IsGalleryIndexAtStart();
            }
        }
        else if (m_awsS3Client != null)
        {
            if (m_arrowType == ArrowType.kNext)
            {
                return !m_awsS3Client.IsS3ImageIndexAtEnd();
            }
            else if (m_arrowType == ArrowType.kPrev)
            {
                return !m_awsS3Client.IsS3ImageIndexAtStart();
            }      
        }

        return true;
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
        ArrowPressed();
    }

    private void ArrowPressed()   
    {    
        Debug.Log("------- VREEL: Called Scroll() on a " + m_arrowType);

        if (m_deviceGallery != null)
        {
            if (m_arrowType == ArrowType.kNext)
            {
                m_deviceGallery.NextPictures();
            }
            else if (m_arrowType == ArrowType.kPrev)
            {
                m_deviceGallery.PreviousPictures();
            }      
        }
        else if (m_awsS3Client != null)
        {
            if (m_arrowType == ArrowType.kNext)
            {
                m_awsS3Client.NextImages();
            }
            else if (m_arrowType == ArrowType.kPrev)
            {
                m_awsS3Client.PreviousImages();
            }      
        }
    }
}