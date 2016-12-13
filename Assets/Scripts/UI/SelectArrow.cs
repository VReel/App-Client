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

    // **************************
    // Public functions
    // **************************

    public void Update() //TODO: Remove this Update and make this a simpler event based class!
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
}