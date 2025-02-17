﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.VR;       //VRSettings

public class KeyBoard : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************
    	
	public float m_blinkingTime = 1.5f;
    public float m_distPos = 0.1f;
    public Text m_typeTextObject;
    public GameObject m_mainCanvas;
    public bool m_conserveText = false;

    private Text m_objectiveTextObject;
    private string m_previousText = "";      // Text before selecting inputform
    private string m_defaultText = "";       // Default value for inputform text
    private float m_elapsedTime;
    private bool m_capitalLeters, m_symbolMode, m_blink;
    private bool m_shouldBeShowing = false;
    private int m_numLetters;

    private Text[] m_allTextChars;
    private Text[] m_allSymbolChars;
    private string m_actualText;

    // **************************
    // Public functions
    // **************************

	public void Start () 
	{
		/*  USE THIS AS A PROTOTYPE ONLY
		//GameObject[] go = GameObject.FindGameObjectsWithTag("char");
		nb_leters = go.Length;

		charText = new Text[nb_leters];

		for(int ii = 0; ii <nb_leters; ii++)
		{
			charText[ii] = go[ii].GetComponent<Text>();
		}

		//find all the symbols (on the background)
	    go = GameObject.FindGameObjectsWithTag("charBack");
		nb_symbol = go.Length;

		if(nb_symbol != nb_leters)
		{
			Debug.Log("Error: symbols != chars --> letters = "+ nb_leters+ "  symbols = "+nb_symbol);
			return;
		}

		charSymbol = new Text[nb_leters];
		for(int ii = 0; ii < nb_leters; ii++)
		{
		  	//Debug.Log(go[ii].name + "  " + ii);
			charSymbol[ii] = go[ii].GetComponent<Text>();
		}
		*/

		GameObject[] go = GameObject.FindGameObjectsWithTag("KeyboardButton");
		m_numLetters = go.Length;
        m_allTextChars = new Text[m_numLetters];
        m_allSymbolChars = new Text[m_numLetters];

        for(int i = 0; i < m_numLetters; i++)
		{
            m_allTextChars[i] = go[i].transform.GetChild(0).GetComponent<Text>();
            m_allSymbolChars[i] = go[i].transform.GetChild(1).GetComponent<Text>();
		}

        m_elapsedTime = 0;
        m_objectiveTextObject = null;

        Hide();
        m_shouldBeShowing = false;
        m_symbolMode = false;
        m_capitalLeters = true;

        UperLowerCase(); // Force Lower case to begin with...
	}
	
	// Update is called once per frame
	void FixedUpdate () 
	{
		// only blink when a text is selected
        m_elapsedTime += Time.fixedDeltaTime;
        if(m_objectiveTextObject != null)
		{
            if(m_elapsedTime > m_blinkingTime)
			{
                m_elapsedTime = 0;

				//Debug.Log("blink");

                if(!m_blink)
				{
                    m_typeTextObject.text = m_actualText;
                    m_objectiveTextObject.text = m_actualText;
				}
				else
				{
                    m_typeTextObject.text = m_actualText + " |";
                    m_objectiveTextObject.text = m_actualText + " |";
				}

                m_blink = !m_blink;
			}
		}
	}

	public void WriteChar(Text txt)
	{
        m_actualText = m_actualText + txt.text;
        m_typeTextObject.text = m_actualText;
        m_objectiveTextObject.text = m_actualText;
	}

	public void Erase()
	{
        if(m_actualText.Length > 0)
		{
            m_actualText = m_actualText.Remove(m_actualText.Length-1);
            m_typeTextObject.text = m_actualText;
            m_objectiveTextObject.text = m_actualText;
		}
	}

    public bool ShouldBeShowing()
    {
        return m_shouldBeShowing;
    }

    public void Show()
    {
        m_mainCanvas.SetActive(true);
    }

    public void Hide()
    {
        m_mainCanvas.SetActive(false);
    }

	// this function gets new input forms and stores temp string values
    public void SelectTextInput(Text clickedTextObject)
	{
        // If keyboard active because of a previous SelectTextInput() call, then AcceptText() on currently active InputForm
        AcceptText();
            
        m_objectiveTextObject = clickedTextObject;
        m_previousText = clickedTextObject.text;

        if(m_conserveText == false)
		{
            m_objectiveTextObject.text = "";
		}

        m_actualText = m_objectiveTextObject.text;

        m_shouldBeShowing = true;
        Show();
	}

    public void SelectTextInputDefaultText(string defaultText)
    {
        m_defaultText = defaultText;

        var passwordText = m_objectiveTextObject.GetComponent<PasswordText>();
        if (m_defaultText.CompareTo(m_objectiveTextObject.text) == 0 || passwordText != null)
        {
            m_actualText = "";
            m_objectiveTextObject.text = "";
        }
    }

	public void AcceptText()
	{
        if (!m_shouldBeShowing)
        {
            return;
        }
        
        if (m_actualText.Length > 0)
        {
            var passwordText = m_objectiveTextObject.GetComponent<PasswordText>();
            if (passwordText != null)
            {
                passwordText.SetString(m_actualText);
            }
            else
            {
                m_objectiveTextObject.text = m_actualText;
            }
        }
        else
        {
            m_objectiveTextObject.text = m_defaultText;
        }

        m_shouldBeShowing = false;
        Hide();
        m_objectiveTextObject = null;	
	}

	public void CancelText()
	{		
        if (m_objectiveTextObject != null)
        {
            m_objectiveTextObject.text = m_previousText;
        }

        m_objectiveTextObject = null;
        m_previousText = "";

        m_shouldBeShowing = false;
        Hide();
	}
        
	public void UperLowerCase()
	{
        if(m_symbolMode == true)
        {
            return;
        }
        
        if(m_capitalLeters == false)
		{
            for(int i = 0; i < m_numLetters; i++)
			{
                m_allTextChars[i].text = m_allTextChars[i].text.ToUpper();
			}

            m_capitalLeters = true;
		}
		else
		{
            for(int i = 0; i < m_numLetters; i++)
			{
                m_allTextChars[i].text = m_allTextChars[i].text.ToLower();
			}

            m_capitalLeters = false;
		}
	}
	
	public void SymbolChangeMode()
	{
        if(m_symbolMode == false)
		{			
			string temp1;
            for(int i = 0; i < m_numLetters; i++)
			{
                temp1 = m_allTextChars[i].text;
                m_allTextChars[i].text = m_allSymbolChars[i].text;
                m_allSymbolChars[i].text = temp1;
			}

			m_symbolMode = true;
		}
		else
		{
			string temp2;
            for(int i = 0; i < m_numLetters; i++)
			{
                temp2 = m_allSymbolChars[i].text;
                m_allSymbolChars[i].text = m_allTextChars[i].text;
                m_allTextChars[i].text = temp2;
			}

            m_symbolMode = false;
		}		
	}

    public void SetInVRMode(bool inVR)
    {
        if (inVR)
        {
            transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        }
        else
        {
            transform.localScale = new Vector3(0.0175f, 0.0175f, 0.01f);
        }
    }
}