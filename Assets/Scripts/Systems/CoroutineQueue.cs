using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CoroutineQueue
{
    MonoBehaviour m_owner = null;
    Coroutine m_mainInternalCoroutine = null;
    Queue<IEnumerator> actions = new Queue<IEnumerator>();

    public CoroutineQueue(MonoBehaviour aCoroutineOwner)
    {
        m_owner = aCoroutineOwner;
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
        actions.Clear();
        m_owner.StopAllCoroutines();
        m_mainInternalCoroutine = m_owner.StartCoroutine(Process());
    }

    public void EnqueueAction(IEnumerator aAction)
    {
        actions.Enqueue(aAction);
    }

    public void EnqueueWait(float aWaitTime)
    {
        actions.Enqueue(Wait(aWaitTime));
    }

    private IEnumerator Wait(float aWaitTime)
    {
        yield return new WaitForSeconds(aWaitTime);
    }

    private IEnumerator Process()
    {
        while (true)
        {
            if (actions.Count > 0)
            {                                
                yield return m_owner.StartCoroutine(actions.Dequeue());
            }
            else
            {
                yield return null;
            }
        }
    }
}