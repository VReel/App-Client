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

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private Profile m_profile;
    [SerializeField] private Gallery m_gallery;
    [SerializeField] private Search m_search;
    [SerializeField] private MeshCollider m_meshCollider;
    [SerializeField] private Image m_arrowImage;
    [SerializeField] private Image m_transparentBackgroundImage;
    [SerializeField] private ArrowType m_arrowType = ArrowType.kNext;

    // **************************
    // Public functions
    // **************************

    public void Update() //TODO: Remove this Update and make this a simpler event based class!
    {
        UpdateVisibility();
    }

    public void OnButtonSelected()
    {
        OnButtonSelectedInternal();
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void UpdateVisibility()
    {
        bool shouldBeVisible = ShouldBeVisible();
        m_meshCollider.enabled = shouldBeVisible;
        m_arrowImage.enabled = shouldBeVisible;
        m_transparentBackgroundImage.enabled = shouldBeVisible;
    }

    private bool ShouldBeVisible()
    {
        if (m_menuController != null)
        {
            if (!m_menuController.GetMenuActive())
            {
                return false;
            }
        }

        if (m_appDirector.GetState() == AppDirector.AppState.kInit || 
            m_appDirector.GetState() == AppDirector.AppState.kLogin)
        {
            return false;
        }

        if (m_appDirector.GetState() == AppDirector.AppState.kProfile)
        {
            if (m_arrowType == ArrowType.kNext)
            {
                return !m_profile.IsPostIndexAtEnd();
            }
            else if (m_arrowType == ArrowType.kPrev)
            {
                return !m_profile.IsPostIndexAtStart();
            }      
        }

        if (m_appDirector.GetState() == AppDirector.AppState.kGallery)
        {
            if (m_arrowType == ArrowType.kNext)
            {
                return !m_gallery.IsGalleryIndexAtEnd();
            }
            else if (m_arrowType == ArrowType.kPrev)
            {
                return !m_gallery.IsGalleryIndexAtStart();
            }
        }

        if (m_appDirector.GetState() == AppDirector.AppState.kSearch)
        {
            if (m_search.GetSearchState() == Search.SearchState.kUserDisplay)
            {
                if (m_arrowType == ArrowType.kNext)
                {
                    return !m_profile.IsPostIndexAtEnd();
                }
                else if (m_arrowType == ArrowType.kPrev)
                {
                    return !m_profile.IsPostIndexAtStart();
                }   
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public void OnButtonSelectedInternal()
    {
        if (m_appDirector.GetState() == AppDirector.AppState.kProfile ||
            (m_appDirector.GetState() == AppDirector.AppState.kSearch && 
                m_search.GetSearchState() == Search.SearchState.kUserDisplay))
        {
            if (m_arrowType == ArrowType.kNext)
            {
                m_profile.NextImages();
            }
            else if (m_arrowType == ArrowType.kPrev)
            {
                m_profile.PreviousImages();
            }      
        }

        if (m_appDirector.GetState() == AppDirector.AppState.kGallery)
        {
            if (m_arrowType == ArrowType.kNext)
            {
                m_gallery.NextImages();
            }
            else if (m_arrowType == ArrowType.kPrev)
            {
                m_gallery.PreviousImages();
            }
        }  
    }
}