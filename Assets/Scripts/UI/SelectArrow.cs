using UnityEngine;

public class SelectArrow : MonoBehaviour 
{
    public enum ArrowType
    {
        kPrev,
        kNext
    };

    [SerializeField] private ArrowType m_arrowType = ArrowType.kNext;
    [SerializeField] private DeviceGallery m_deviceGallery;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;

    public void OnMouseDown()
    {
        Scroll();
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

        if (m_arrowType == ArrowType.kNext)
        {
            m_deviceGallery.NextPictures();
        }
        else if (m_arrowType == ArrowType.kPrev)
        {
            m_deviceGallery.PreviousPictures();
        }           
    }
}
