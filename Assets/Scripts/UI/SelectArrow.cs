using UnityEngine;
using UnityEngine.UI;

public class SelectArrow : MonoBehaviour 
{
    public enum ArrowType
    {
        kPrev,
        kNext
    };
        
    [SerializeField] private MeshCollider m_meshCollider;
    [SerializeField] private Image m_arrowImage;
    [SerializeField] private ArrowType m_arrowType = ArrowType.kNext;
    [SerializeField] private DeviceGallery m_deviceGallery;
    [SerializeField] private AWSS3Client m_awsS3Client;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;

    public void Update() //TODO: Remove this Update and make it event based!
    {
        UpdateActive();
    }

    private void UpdateActive()
    {
        bool shouldBeActive = ShouldBeActive();
        m_meshCollider.enabled = shouldBeActive;
        m_arrowImage.enabled = shouldBeActive;
    }

    private bool ShouldBeActive()
    {
        if (m_deviceGallery != null)
        {
            if (m_arrowType == ArrowType.kNext)
            {
                return !m_deviceGallery.IsIndexAtEnd();
            }
            else if (m_arrowType == ArrowType.kPrev)
            {
                return !m_deviceGallery.IsIndexAtStart();
            }
        }
        else if (m_awsS3Client != null)
        {
            if (m_arrowType == ArrowType.kNext)
            {
                return !m_awsS3Client.IsIndexAtEnd();
            }
            else if (m_arrowType == ArrowType.kPrev)
            {
                return !m_awsS3Client.IsIndexAtStart();
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
        Scroll();
    }

    private void Scroll()   
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
