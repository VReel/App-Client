using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesignVideoController : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

    [SerializeField] private List<TriggerAndTimeEvent> m_events; 

    [System.Serializable]
    public class TriggerAndTimeEvent
    {
        public Animator animator;
        public string triggerName;
        public float triggerTime;
        public bool triggered { get; set; }
    }

	private float m_currTime = 0;

    // **************************
    // Public functions
    // **************************

	public void Update()
	{
        m_currTime += Time.fixedDeltaTime;

        UpdateAllEventsBasedOnCurrTime();
	}

    // **************************
    // Private functions
    // **************************

    private void UpdateAllEventsBasedOnCurrTime()
	{
        for (int i = 0; i < m_events.Count; i++) 
		{
            if (m_events[i].triggered != true) 
			{			
                if (m_currTime > m_events[i].triggerTime && m_events[i].animator != null)
                {
                    m_events[i].animator.SetBool(m_events[i].triggerName, true);
                    m_events[i].triggered = true;
                }
			}
		}
	}
}