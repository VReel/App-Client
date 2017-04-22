using UnityEngine;
using UnityEngine.UI;

public class FollowButton : MonoBehaviour
{        
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private ProfileDetails m_profileDetails;
    [SerializeField] private Image m_followButton;
    [SerializeField] private Color m_buttonColourSelected;
    [SerializeField] private Color m_buttonColourDeselected;
    [SerializeField] private GameObject m_followersCountObject;

    private bool m_isOn = false;

    // **************************
    // Public functions
    // **************************

    public bool GetIsOn()
    {
        return m_isOn;
    }
        
    public void FollowOnOffSwitch()
    {
        m_isOn = !m_isOn;
        m_profileDetails.FollowOrUnfollowUser(m_profileDetails.GetUserId(), m_isOn);
        if (m_isOn)
        {
            m_followButton.color = m_buttonColourSelected;                      
        }
        else
        {
            m_followButton.color = m_buttonColourDeselected;
        }

        if (m_followersCountObject != null)
        {
            Text textObject = m_followersCountObject.GetComponentInChildren<Text>();
            int followers = System.Convert.ToInt32(textObject.text);
            followers = m_isOn ? followers+1 : followers-1;
            textObject.text = followers.ToString();
        }
    }
}