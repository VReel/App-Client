using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR;       //VRSettings
using UnityEngine.Events;   //UnityEvent
using System;

//TODO: Take this out of this Namespace
namespace VRStandardAssets.Menu
{
    public class MenuButton : MonoBehaviour
    {        
        // **************************
        // Member Variables
        // **************************

        [SerializeField] private Image m_buttonImage;
        [SerializeField] private Text m_buttonText;
        [SerializeField] private Color m_buttonColourUp;     // Button's default sprite
        [SerializeField] private Color m_buttonColourOver;   // Button's sprite when the icon is over it
        [SerializeField] private Color m_buttonColourDown;   // Button's sprite when user presses on it
        [SerializeField] private VRStandardAssets.Utils.VRInteractiveItem m_InteractiveItem;       // The interactive item used to know how the user is interacting with the button

        public UnityEvent OnButtonSelectedFunc;              // This event is triggered when the selection of the button has finished.
        public UnityEvent OnButtonDownFunc;                  // This event is triggered when the selection of the button has started.

        private bool m_gazeOver = false;                     // Whether the user is looking at the VRInteractiveItem currently.
        private bool m_buttonDown = false;                   // Whether the user is pushing the VRInteractiveItem down.

        private Color m_buttonForcedColour;  
        private bool m_forceColour = false;

        // **************************
        // Public functions
        // **************************

        public void Start()
        {
            if (m_InteractiveItem)
            {
                m_InteractiveItem.OnOver += HandleOver;
                m_InteractiveItem.OnOut += HandleOut;
                m_InteractiveItem.OnDown += HandleDown;
                m_InteractiveItem.OnUp += HandleUp;
            }
        }

        public void OnDestroy()
        {
            if (m_InteractiveItem)
            {
                m_InteractiveItem.OnOver -= HandleOver;
                m_InteractiveItem.OnOut -= HandleOut;
                m_InteractiveItem.OnDown -= HandleDown;
                m_InteractiveItem.OnUp -= HandleUp;
            }
        }

        public void OnEnable()
        {
            HandleOut();
        }

        public bool GetGazeOver()
        {
            return m_gazeOver;
        }

        public bool GetButtonDown()
        {
            return m_buttonDown;
        }

        public void SetForceColour(bool forceColour, Color buttonForcedColour)
        {
            m_forceColour = forceColour;
            m_buttonForcedColour = buttonForcedColour;

            RefreshColour();
        }

        public void RefreshColour()
        {
            RefreshButtonColor();
            RefreshTextColor();
        }            

        // **************************
        // Private/Helper functions
        // **************************

        private void RefreshButtonColor()
        {
            if (m_buttonImage == null)
            {
                return;
            }

            if (m_forceColour)
            {
                m_buttonImage.color = m_buttonForcedColour;
            }
            else if (m_buttonDown)
            {
                m_buttonImage.color = m_buttonColourDown;
            }
            else if (m_gazeOver)
            {
                m_buttonImage.color = m_buttonColourOver;
            }
            else
            {
                m_buttonImage.color = m_buttonColourUp;
            }
        }

        private void RefreshTextColor()
        {
            if (m_buttonText == null)
            {
                return;
            }

            if (m_forceColour)
            {
                m_buttonText.color = m_buttonForcedColour;
            }
            else if (m_buttonDown)
            {
                m_buttonText.color = m_buttonColourDown;
            }
            else if (m_gazeOver)
            {
                m_buttonText.color = m_buttonColourOver;
            }
            else
            {
                m_buttonText.color = m_buttonColourUp;
            }
        }

        private void HandleOver()
        {
            m_gazeOver = true;

            RefreshColour();
        }
            
        private void HandleOut()
        {
            m_gazeOver = false;
            m_buttonDown = false;

            RefreshColour();
        }

        private void HandleDown()
        {
            m_buttonDown = true;

            RefreshColour();

            if (OnButtonDownFunc != null)
            {
                OnButtonDownFunc.Invoke();
            }
        }

        private void HandleUp()
        {       
            bool buttonSelected = m_gazeOver && m_buttonDown;

            m_buttonDown = false;

            RefreshColour();

            if (buttonSelected)
            {
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
            if (!VRSettings.enabled)
            {
                HandleOver();
            }
        }

        private void OnMouseExit()
        {            
            if (!VRSettings.enabled)
            {
                HandleOut();
            }
        }

        private void OnMouseDown()
        {          
            if (!VRSettings.enabled)
            {
                HandleDown();
            }
        }

        private void OnMouseUp()
        {
            if (!VRSettings.enabled)
            {
                HandleUp();
            }
        }
    }
}   