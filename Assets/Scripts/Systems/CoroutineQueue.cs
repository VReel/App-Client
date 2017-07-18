using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CoroutineQueue
{
    // **************************
    // Member Variables
    // **************************

    MonoBehaviour m_owner = null;
    Coroutine m_mainInternalCoroutine = null;
    Queue<IEnumerator> m_actions = new Queue<IEnumerator>();

    //System.DateTime m_lastProcessTime = System.DateTime.Now;

    // **************************
    // Public functions
    // **************************

    public CoroutineQueue(MonoBehaviour aCoroutineOwner)
    {
        m_owner = aCoroutineOwner;
    }

    public bool IsActive()
    {
        /*
        const float kIsActiveThresholdInSeconds = 1.0f;
        bool processFunctionActive = (System.DateTime.Now - m_lastProcessTime).TotalSeconds < kIsActiveThresholdInSeconds;
        */

        bool internalCoroutineNotNull = m_mainInternalCoroutine != null;
        return internalCoroutineNotNull;
    }

    public int Size()
    {
        return m_actions.Count;
    }

    public void StartLoop()
    {
        m_mainInternalCoroutine = m_owner.StartCoroutine(Process());
    }

    public void StopLoop()
    {
        m_owner.StopCoroutine(m_mainInternalCoroutine);
        m_mainInternalCoroutine = null;
    }

    public void Clear()
    {
        m_actions.Clear();
        m_owner.StopAllCoroutines();
        m_mainInternalCoroutine = m_owner.StartCoroutine(Process());
    }

    public void EnqueueAction(IEnumerator aAction)
    {
        m_actions.Enqueue(aAction);
    }

    public void EnqueueWait(float aWaitTime)
    {
        m_actions.Enqueue(Wait(aWaitTime));
    }

    public void DebugPrint()
    {
        if (Debug.isDebugBuild) 
        {
            Debug.Log("------- VREEL: CoroutineQueue belonging to '" + m_owner + "' is active = " + IsActive() +
                ", and has " + m_actions.Count + " Functions in the Queue!");
        }
    }

    // **************************
    // Private/Helper functions
    // **************************

    private IEnumerator Wait(float aWaitTime)
    {
        yield return new WaitForSeconds(aWaitTime);
    }

    private IEnumerator Process()
    {
        while (true)
        {
            //m_lastProcessTime = System.DateTime.Now;
            
            if (m_actions.Count > 0)
            {                                
                yield return m_owner.StartCoroutine(m_actions.Dequeue());
            }
            else
            {
                yield return null;
            }
        }
    }
}