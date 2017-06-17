////////////////////////////////////////////////////////////////////////////////
//  
// @module Affter Effect Importer
// @author Osipov Stanislav support@stansassets.com
//
////////////////////////////////////////////////////////////////////////////////
/// 
using UnityEngine;
using System.Collections;

#if UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
#else
using UnityEngine.SceneManagement;
#endif


public class AEMenu : MonoBehaviour {

	private float w;
	private float h;

	// Use this for initialization
	void Start () {
		w = Screen.width * 0.2f;
		h = w * 0.3f;
	}
	

	void OnGUI() {
		float startX = w * 0.17f;
		float StartY = Screen.height - h * 1.5f;

				Rect r = new Rect (startX, StartY, w, h);

		if(GUI.Button(r, "Example 1")) {
			LoadLevel ("Boxes");
		}

				r.x += w * 1.2f;

		if(GUI.Button(r, "Example 2")) {
			LoadLevel ("FireSphere");
		}

		r.x += w * 1.2f;

		if(GUI.Button(r, "Example 3")) {
			LoadLevel ("Fog_Sphere");
		}

		r.x += w * 1.2f;

		if(GUI.Button(r, "Example 4")) {
			LoadLevel ("Bouncing_Sphere");
		}
	}

	

	public void LoadLevel(string levelName) {
			#if UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
			Application.LoadLevel(levelName);
			#else
			SceneManager.LoadScene(levelName);
			#endif
	}

}
