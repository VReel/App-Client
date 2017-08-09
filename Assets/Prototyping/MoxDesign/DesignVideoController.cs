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

    public float m_timeMultiplier = 1.6f; // speed multiplier to make the effect faster

    public bool m_isRecordingOn = false;
    public float m_startRecordingTime = 0;
    public float m_stopRecordingTime = 100;

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

    private void Awake()
    {
        Application.runInBackground = true;
    }

	public void Update()
	{
        m_currTime += (Time.deltaTime * m_timeMultiplier);

        for (int i = 0; i < m_eventGroups.Count; i++) 
        {
            UpdateAllEventsBasedOnCurrTime(i);
        }

        if (m_isRecordingOn)
        {
            if (m_currTime > m_startRecordingTime &&
                 RockVR.Video.VideoCaptureCtrl.instance.status == RockVR.Video.VideoCaptureCtrl.StatusType.NOT_START)
            {
                RockVR.Video.VideoCaptureCtrl.instance.StartCapture();
            }

            if (m_currTime > m_stopRecordingTime &&
                 RockVR.Video.VideoCaptureCtrl.instance.status == RockVR.Video.VideoCaptureCtrl.StatusType.STARTED)
            {
                RockVR.Video.VideoCaptureCtrl.instance.StopCapture();
            }
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