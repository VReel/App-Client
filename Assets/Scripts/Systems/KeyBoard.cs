using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class KeyBoard : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************
    	
	public float m_blinkingTime = 1.5f;
    public Text m_objectiveTextObject;
    public float m_distPos = 0.1f;
    public GameObject m_mainCanvas;
    public bool m_conserveText = false;

    private string m_previousText;      // Text before selecting inputform
    private string m_defaultText;       // Default value for inputform text
    private float m_elapsedTime;
    private bool m_capitalLeters, m_symbolMode, m_blink;
    private int m_numLetters;

    private Text[] m_allTextChars;
    private Text[] m_allSymbolChars;
    private string m_blinkText, m_actualText;

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

        m_mainCanvas.SetActive(false);
        m_symbolMode = false;
        m_capitalLeters = false;
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
                    m_objectiveTextObject.text = m_actualText;
				}
				else
				{
                    m_objectiveTextObject.text = m_blinkText;
				}

                m_blink = !m_blink;
			}
		}
	}

	public void WriteChar(Text txt)
	{
        m_objectiveTextObject.text = m_actualText + txt.text;
        m_actualText = m_objectiveTextObject.text;
        m_blinkText = m_objectiveTextObject.text + " |";	
	}

	public void Erase()
	{
        if(m_actualText.Length > 0)
		{
            m_objectiveTextObject.text = m_actualText.Remove(m_actualText.Length-1);
		}

        m_actualText = m_objectiveTextObject.text;
        m_blinkText = m_objectiveTextObject.text + " |";
	}

	// this function gets new input forms and stores temp string values
    public void SelectTextInput(Text clickedTextObject)
	{
        // If keyboard active because of a previous SelectTextInput() call, then AcceptText() on currently active InputForm
        if (m_mainCanvas.activeInHierarchy)
        {
            AcceptText();
        }
            
        m_objectiveTextObject = clickedTextObject;
        m_previousText = clickedTextObject.text;

        if(m_conserveText == false)
		{
            m_objectiveTextObject.text = "";
		}

        m_actualText = m_objectiveTextObject.text;
        m_blinkText = m_objectiveTextObject.text + " |";

        m_mainCanvas.SetActive(true);
	}

    public void SelectTextInputDefaultText(string defaultText)
    {
        m_defaultText = defaultText;

        if (m_defaultText.CompareTo(m_objectiveTextObject.text) == 0)
        {
            m_objectiveTextObject.text = "";
            m_actualText = m_objectiveTextObject.text;
            m_blinkText = m_objectiveTextObject.text + " |";
        }
    }

	public void AcceptText()
	{
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

        m_mainCanvas.SetActive(false);
        m_objectiveTextObject = null;	
	}

	public void CancelText()
	{		
        m_objectiveTextObject.text = m_previousText;
        m_mainCanvas.SetActive(false);
        m_objectiveTextObject = null;
	}
        
	public void UperLowerCase()
	{
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
}