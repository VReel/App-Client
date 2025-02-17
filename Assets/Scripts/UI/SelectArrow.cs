﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR;               //VRSettings

// NOTE: This class is being used for scroll too, it should probably be renamed...
public class SelectArrow : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private AppDirector m_appDirector;
    [SerializeField] private MenuController m_menuController;
    [SerializeField] private Carousel m_carousel;
    [SerializeField] private Posts m_posts;
    [SerializeField] private Gallery m_gallery;
    [SerializeField] private Search m_search;
    [SerializeField] private MeshCollider m_meshCollider;
    [SerializeField] private Image m_arrowImage;
    [SerializeField] private Image m_transparentBackgroundImage;
    [SerializeField] private ArrowType m_arrowType = ArrowType.kNext;

    private VRStandardAssets.Menu.MenuButton m_menuButton;

    public enum ArrowType
    {
        kPrev,
        kNext
        //kScroll
    };

    // **************************
    // Public functions
    // **************************

    public void Start()
    {
        m_menuButton = gameObject.GetComponent<VRStandardAssets.Menu.MenuButton>();
    }

    public void Update() //TODO: Remove this Update and make this a simpler event based class!
    {
        UpdateVisibility();
    }

    public void OnButtonSelected()
    {
        //OnButtonSelectedInternal();
    }

    public ArrowType GetArrowType()
    {
        return m_arrowType;
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
            if (!m_menuController.IsMenuActive())
            {
                return false;
            }
        }

        if (m_appDirector == null ||
            m_appDirector.GetState() == AppDirector.AppState.kInit  ||             
            m_appDirector.GetOverlayShowing())
        {
            return false;
        }

        if (IsPostsActive())
        {
            if (m_arrowType == ArrowType.kNext)
            {
                return !(m_posts.IsPostIndexAtEnd() && m_carousel.IsAtMiddle());
            }
            else if (m_arrowType == ArrowType.kPrev)
            {
                return !(m_posts.IsPostIndexAtStart() && m_carousel.IsAtMiddle());
            }
        }

        if (m_appDirector.GetState() == AppDirector.AppState.kGallery)
        {
            if (m_arrowType == ArrowType.kNext)
            {
                return !(m_gallery.IsGalleryIndexAtEnd() && m_carousel.IsAtMiddle());
            }
            else if (m_arrowType == ArrowType.kPrev)
            {
                return !(m_gallery.IsGalleryIndexAtStart() && m_carousel.IsAtMiddle());
            }
        }
            
        return false;
    }

    /*
    private void OnButtonSelectedInternal()
    {        
        if (IsPostsActive())
        {
            if (m_arrowType == ArrowType.kNext)
            {
                m_posts.NextPosts();
            }
            else if (m_arrowType == ArrowType.kPrev)
            {
                m_posts.PreviousPosts();
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
    */

    private bool IsPostsActive()
    {
        bool isPostsActive = 
             m_appDirector.GetState() == AppDirector.AppState.kExplore   ||
              m_appDirector.GetState() == AppDirector.AppState.kFollowing ||
                m_appDirector.GetState() == AppDirector.AppState.kProfile ||
              /* (m_appDirector.GetState() == AppDirector.AppState.kSearch && m_search.GetSearchState() == Search.SearchState.kUserDisplay) || */
              (m_appDirector.GetState() == AppDirector.AppState.kSearch && m_search.GetSearchState() == Search.SearchState.kTagDisplay);

        return isPostsActive;
    }
}