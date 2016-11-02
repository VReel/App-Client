using UnityEngine;
using UnityEngine.UI;
using System;

namespace VRStandardAssets.Menu
{
    public class MenuButton : MonoBehaviour
    {
        public event Action<MenuButton> OnButtonSelected;                   // This event is triggered when the selection of the button has finished.

        [SerializeField] private Image m_buttonImage;
        [SerializeField] private Sprite m_spriteButtonDown;
        [SerializeField] private Sprite m_spriteButtonUp;
        [SerializeField] private VRStandardAssets.Utils.VRInteractiveItem m_InteractiveItem;       // The interactive item used to know how the user is interacting with the button

        private bool m_gazeOver = false;                                    // Whether the user is looking at the VRInteractiveItem currently.
        private bool m_buttonDown = false;                                  // Whether the user is pushing the VRInteractiveItem down.

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

        public void SetSpriteButtonUp(Sprite spriteButtonUp)
        {
            m_spriteButtonUp = spriteButtonUp;
            if (m_buttonDown)
            {
                HandleDown();
            }
            else
            {
                HandleUp();
            }
        }

        private void OnEnable ()
        {
            m_InteractiveItem.OnOver += HandleOver;
            m_InteractiveItem.OnOut += HandleOut;
            m_InteractiveItem.OnDown += HandleDown;
            m_InteractiveItem.OnUp += HandleUp;
            m_InteractiveItem.OnClick += HandleClick;
        }

        private void OnDisable ()
        {
            m_InteractiveItem.OnOver -= HandleOver;
            m_InteractiveItem.OnOut -= HandleOut;
            m_InteractiveItem.OnClick -= HandleDown;
            m_InteractiveItem.OnClick -= HandleUp;
            m_InteractiveItem.OnClick -= HandleClick;
        }

        private void HandleOver()
        {
            m_gazeOver = true;
        }
            
        private void HandleOut()
        {
            m_gazeOver = false;
        }

        private void HandleDown()
        {
            m_buttonDown = true;

            if (m_buttonImage != null && m_spriteButtonDown != null)
            {
                m_buttonImage.sprite = m_spriteButtonDown;
            }
        }

        private void HandleUp()
        {
            m_buttonDown = false;

            if (m_buttonImage != null && m_spriteButtonUp != null)
            {
                m_buttonImage.sprite = m_spriteButtonUp;
            }
        }

        private void HandleClick()
        {                
            OnButtonSelected(this);
        }

        // NOTE: The following functions is for making debugging without a headset easier...
        private void OnMouseDown()
        {
            HandleClick();
            HandleDown();
        }

        // NOTE: The following functions is for making debugging without a headset easier...
        private void OnMouseUp()
        {
            HandleUp();
        }
    }
}   