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
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: SetString() called with: " + newText);

        if (newText.CompareTo(m_defaultString) != 0)
        {
            m_myString = newText;
            m_myText.text = ReplaceStringWithAsterixes(newText);
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