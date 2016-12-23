﻿using UnityEngine;
using System;                   // Func
using System.Collections;       // IEnumerator

public class ThreadJob
{
    // **************************
    // Member Variables
    // **************************

    // Thread-safe "IsDone" check!
    public bool IsDone                     
    {
        get
        {
            bool tmp;
            lock (m_handle)
            {
                tmp = m_isDoneFlag;
            }
            return tmp;
        }
        set
        {
            lock (m_handle)
            {
                m_isDoneFlag = value;
            }
        }
    }

    private bool m_isDoneFlag = false;
    private object m_handle = new object();
    private System.Threading.Thread m_thread = null;
    private MonoBehaviour m_owner = null;
    private Func<object> m_threadFunc;

    // **************************
    // Public functions
    // **************************

    public ThreadJob(MonoBehaviour owner)
    {
        m_owner = owner;
        Debug.Log("------- VREEL: A ThreadJob was created by = " + m_owner.name);
    }

    public void Start(Func<object> threadFunc)
    {
        Debug.Log("------- VREEL: Start on Thread has been called!");
        
        IsDone = false;
        m_threadFunc = threadFunc;        
        m_thread = new System.Threading.Thread(Run);
        m_thread.Start();
    }

    public void Abort()
    {
        m_thread.Abort();
    }        
        
    public IEnumerator WaitFor()
    {
        while(!IsDone)
        {
            yield return null;
        }
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void Run()
    {
        Debug.Log("------- VREEL: Began Running function on background thread!");

        m_threadFunc();
        IsDone = true;

        Debug.Log("------- VREEL: Finished Running function on background thread!");
    }
}