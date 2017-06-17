using UnityEngine;
using System.Collections;

public class TestCharacter : MonoBehaviour {

	public AEAnimationController anim;
	
	private Rect r;
	
	
	void Start() {
		r = new Rect(20, 130, 130, 50);
		
		
	}
	
	
	void OnGUI() {
		
		Rect buttonRect = r;
		
		if(GUI.Button(buttonRect, "Pos 1")) {
			anim.CrossFade("j1", 0.5f);
		}
		
		
		buttonRect.y += 70;
		if(GUI.Button(buttonRect, "Pos 2")) {
			anim.CrossFade("j2",  0.5f);
		}
		
	}
}
