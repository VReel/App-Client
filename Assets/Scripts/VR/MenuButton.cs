using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;   //UnityEvent
using System;

namespace VRStandardAssets.Menu
{
    public class MenuButton : MonoBehaviour
    {        
        [SerializeField] private Image m_buttonImage;
        [SerializeField] private Sprite m_spriteButtonUp;       // Button's default sprite
        [SerializeField] private Sprite m_spriteButtonOver;     // Button's sprite when the icon is over it
        [SerializeField] private Sprite m_spriteButtonDown;     // Button's sprite when user presses on it
        [SerializeField] private VRStandardAssets.Utils.VRInteractiveItem m_InteractiveItem;       // The interactive item used to know how the user is interacting with the button

        // TODO: Make this into a single event...
        public UnityEvent OnButtonSelectedFunc;              // This event is triggered when the selection of the button has finished.
        public event Action<MenuButton> OnButtonSelected;    // This event is triggered when the selection of the button has finished.

        private bool m_gazeOver = false;                     // Whether the user is looking at the VRInteractiveItem currently.
        private bool m_buttonDown = false;                   // Whether the user is pushing the VRInteractiveItem down.

        public bool GetGazeOver()
        {
            return m_gazeOver;
        }

        public bool GetButtonDown()
        {
            return m_buttonDown;
        }

        public Sprite GetSpriteButtonUp()
        {
            return m_spriteButtonUp;
        }

        public Sprite GetSpriteButtonOver()
        {
            return m_spriteButtonOver;
        }

        public Sprite GetSpriteButtonDown()
        {
            return m_spriteButtonDown;
        }

        public void SetSpriteButtonUp(Sprite spriteButtonUp)
        {
            m_spriteButtonUp = spriteButtonUp;
        }

        public void RefreshButtonSprite()
        {
            if (m_buttonImage == null)
            {
                return;
            }

            if (m_buttonDown && m_spriteButtonDown != null)
            {
                m_buttonImage.sprite = m_spriteButtonDown;
            }
            else if (m_gazeOver && m_spriteButtonOver != null)
            {
                m_buttonImage.sprite = m_spriteButtonOver;
            }
            else if (m_spriteButtonUp != null)
            {
                m_buttonImage.sprite = m_spriteButtonUp;
            }
        }

        private void OnEnable ()
        {
            m_InteractiveItem.OnOver += HandleOver;
            m_InteractiveItem.OnOut += HandleOut;
            m_InteractiveItem.OnDown += HandleDown;
            m_InteractiveItem.OnUp += HandleUp;
        }

        private void OnDisable ()
        {
            m_InteractiveItem.OnOver -= HandleOver;
            m_InteractiveItem.OnOut -= HandleOut;
            m_InteractiveItem.OnDown -= HandleDown;
            m_InteractiveItem.OnUp -= HandleUp;
        }

        private void HandleOver()
        {
            m_gazeOver = true;

            RefreshButtonSprite();
        }
            
        private void HandleOut()
        {
            m_gazeOver = false;

            RefreshButtonSprite();
        }

        private void HandleDown()
        {
            m_buttonDown = true;

            RefreshButtonSprite();
        }

        private void HandleUp()
        {
            m_buttonDown = false;

            RefreshButtonSprite();

            if (m_gazeOver)
            {
                if (OnButtonSelected != null)
                {
                    OnButtonSelected(this);
                }

                if (OnButtonSelectedFunc != null)
                {
                    OnButtonSelectedFunc.Invoke();
                }
            }           
        }

        // NOTE: The following functions is for making debugging without a headset easier...
        // ------------------------------------------
        private void OnMouseOver()
        {            
            HandleOver();
        }

        private void OnMouseExit()
        {            
            HandleOut();
        }

        private void OnMouseDown()
        {            
            HandleDown();
        }

        private void OnMouseUp()
        {
            HandleUp();
        }
    }
}   