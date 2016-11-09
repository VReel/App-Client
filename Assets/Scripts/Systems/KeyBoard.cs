using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class KeyBoard : MonoBehaviour 
{
	// Use this for initialization
	public float blinkingTime = 1.5f;
	public Text objectiveText;
	public float distPos = 0.1f;
	public Canvas mainCanvas;
	public bool conserveText = false;

	private string previousText;
    private float elapsedTime;
    private bool capitalLeters, symbolMode, blink;
    private int nb_leters;

	public Text[] charText;
	public Text[] charSymbol;
    private string blinkText, actualText;

	void Start () 
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
		nb_leters = go.Length;
		charText = new Text[nb_leters];
		charSymbol = new Text[nb_leters];

		for(int ii=0; ii < nb_leters; ii++)
		{
			charText[ii] = go[ii].transform.GetChild(0).GetComponent<Text>();
			charSymbol[ii] = go[ii].transform.GetChild(1).GetComponent<Text>();
		}

        elapsedTime = 0;
		objectiveText = null;

		mainCanvas.enabled = false;
		symbolMode = false;
		capitalLeters = false;
	}
	
	// Update is called once per frame
	void FixedUpdate () 
	{
		// only blink when a text is selected
        elapsedTime += Time.fixedDeltaTime;
		if(objectiveText != null)
		{
            if(elapsedTime > blinkingTime)
			{
                elapsedTime = 0;

				//Debug.Log("blink");

				if(!blink)
				{
					objectiveText.text = actualText;
				}
				else
				{
					objectiveText.text = blinkText;
				}

				blink = !blink;
			}
		}
	}

	public void WriteChar(Text txt)
	{
        objectiveText.text = actualText + txt.text;
        actualText = objectiveText.text;
        blinkText = objectiveText.text + " |";	
	}

	public void Erase()
	{
        if(actualText.Length > 0)
		{
            objectiveText.text = actualText.Remove(actualText.Length-1);
		}

        actualText = objectiveText.text;
        blinkText = objectiveText.text + " |";
	}

	// this function gets new input forms and stores temp string values
	public void SelectTextInput(Text clickedText)
	{
		// prevent line to stay at the end of the text:
		if(objectiveText != null)
		{
			objectiveText.text = actualText;
		}
            
		objectiveText = clickedText;
		previousText = clickedText.text;

		if(conserveText == false)
		{
			objectiveText.text="";
		}

        actualText = objectiveText.text;
        blinkText = objectiveText.text + " |";

		mainCanvas.enabled = true;
	}

	public void AcceptText()
	{
        objectiveText.text = actualText;
		mainCanvas.enabled=false;
		objectiveText=null;	
	}

	public void CancelText()
	{		
		objectiveText.text = previousText;
		mainCanvas.enabled = false;

		objectiveText = null;
	}
        
	public void UperLowerCase()
	{
		if(capitalLeters == false)
		{
			for(int ii=0; ii < nb_leters;ii++)
			{
				charText[ii].text = charText[ii].text.ToUpper();
				capitalLeters = true;
			}
		}
		else
		{
			for(int ii=0; ii < nb_leters;ii++)
			{
				charText[ii].text = charText[ii].text.ToLower();
				capitalLeters = false;
			}
		}
	}
	
	public void SymbolChangeMode()
	{
		if(symbolMode == false)
		{			
			string temp1;
			for(int ii=0; ii<nb_leters; ii++)
			{
				temp1=charText[ii].text;
				charText[ii].text = charSymbol[ii].text;
				charSymbol[ii].text = temp1;
			}

			symbolMode = true;
		}
		else
		{
			string temp2;
			for(int ii=0; ii<nb_leters; ii++)
			{
				temp2=charSymbol[ii].text;
				charSymbol[ii].text = charText[ii].text;
				charText[ii].text = temp2;
			}

			symbolMode = false;
		}		
	}
}