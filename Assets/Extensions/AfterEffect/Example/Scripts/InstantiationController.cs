using UnityEngine;
using System.Collections;

public class InstantiationController : MonoBehaviour {

	private float w;
	private float h;

	public GameObject prefab;
	
	// Use this for initialization
	void Start () {
		w = Screen.width * 0.2f;
		h = w * 0.3f;
	}


	void OnGUI() {
		float startX = w * 0.17f;
		float StartY = h * 0.5f;
		
		Rect r = new Rect (startX, StartY, w, h);
		
		if(GUI.Button(r, "Spawn")) {
			GameObject g = Instantiate(prefab) as GameObject;

			g.transform.parent  = transform;

			g.transform.localPosition = Vector3.zero;
		}
		

	}
}
