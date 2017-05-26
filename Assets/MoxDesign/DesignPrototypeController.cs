using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesignPrototypeController : MonoBehaviour 
{
	// Member variables

	[SerializeField] private List<GameObject> m_displays; 

	private int m_currDisplayIndex = 0;

	// public functions

	public void Update()
	{
		if (Input.GetButtonDown("Fire1"))
		{
			SwitchDisplay();
		}
	}

	// private functions

	private void SwitchDisplay()
	{
		m_currDisplayIndex = (m_currDisplayIndex + 1) % m_displays.Count;

		for (int i = 0; i < m_displays.Count; i++) 
		{
			if (m_displays [i] != null) 
			{			
				m_displays [i].SetActive (i == m_currDisplayIndex);
			}
		}
	}
}