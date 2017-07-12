using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesignVideoController : MonoBehaviour 
{
    // **************************
    // Member Variables
    // **************************

	public float m_currTime = 0;

    [SerializeField] private List<EventList> m_eventGroups; 

    [System.Serializable]
    public class EventList
    {
        public List<TriggerAndTimeEvent> events;
    }

    [System.Serializable]
    public class TriggerAndTimeEvent
    {
        public Animator animator;
        public string triggerName;
        public float triggerTime;
        public bool setToTrue;
        public bool triggered { get; set; }
    }		

    // **************************
    // Public functions
    // **************************

	public void Update()
	{
		m_currTime += Time.deltaTime;

        for (int i = 0; i < m_eventGroups.Count; i++) 
        {
            UpdateAllEventsBasedOnCurrTime(i);
        }
	}

    // **************************
    // Private functions
    // **************************

    private void UpdateAllEventsBasedOnCurrTime(int eventGroupIndex)
	{
        for (int i = 0; i < m_eventGroups[eventGroupIndex].events.Count; i++) 
		{
            TriggerAndTimeEvent thisEvent = m_eventGroups[eventGroupIndex].events[i];
            if (thisEvent.triggered != true) 
			{			
                if (m_currTime > thisEvent.triggerTime && thisEvent.animator != null)
                {
                    thisEvent.animator.SetBool(thisEvent.triggerName, thisEvent.setToTrue);
                    thisEvent.triggered = true;
                }
			}
		}
	}
}