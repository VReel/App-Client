using UnityEngine;

public class LoadingIcon : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private GameObject m_loadingIcon;

    private int m_iconDisplayCount;   

    // **************************
    // Public functions
    // **************************

    void Start()
    {
        m_iconDisplayCount = 0;
        UpdateIconVisibility();
    }

    public void Display() 
    {
        m_iconDisplayCount++;
        UpdateIconVisibility();
	}

    public void Hide()
    {
        m_iconDisplayCount--;
        UpdateIconVisibility();
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void UpdateIconVisibility()
    {
        if (Debug.isDebugBuild) Debug.Log("------- VREEL: LoadingIcon DisplayCount = " + m_iconDisplayCount);

        bool displayIcon = m_iconDisplayCount > 0;
        m_loadingIcon.SetActive(displayIcon);
        m_loadingIcon.GetComponent<Collider>().enabled = displayIcon;
        foreach (var renderer in m_loadingIcon.GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = displayIcon;
        }

        if (m_iconDisplayCount < 0)
        {
            if (Debug.isDebugBuild) Debug.Log("------- VREEL: ERROR - Loading Icon has been set to Hide too many times...");
        }
    }
}