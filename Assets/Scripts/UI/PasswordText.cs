using UnityEngine;
using UnityEngine.UI;

public class PasswordText : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    public string m_defaultString;

    private string m_myString;
    private Text m_myText;

    // **************************
    // Public functions
    // **************************

    void Start()
    {
        m_myText = GetComponent<Text>();
    }

    public void SetString (string newText) 
    {
        if (m_myText.text.CompareTo(m_defaultString) != 0)
        {
            m_myString = m_myText.text;
            m_myText.text = ReplaceStringWithAsterixes(m_myText.text);
        }
        else
        {
            m_myText.text = m_defaultString;
        }
	}

    public string GetString()
    {
        return m_myString;
    }

    // **************************
    // Private/Helper functions
    // **************************

    private string ReplaceStringWithAsterixes(string actualString)
    {
        string returnString = "";
        for (int i = 0; i < actualString.Length; i++)
        {
            returnString += "*";
        }
        return returnString;
    }
}