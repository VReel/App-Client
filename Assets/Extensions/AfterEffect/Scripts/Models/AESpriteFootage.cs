using UnityEngine;
using System.Collections;

public class AESpriteFootage : AEFootage {






	public override void SetMaterial() {

		if(_layer.footageType == AEFootageType.FileSource){
			string textureName = _anim.imagesFolder + _layer.sourceNoExt;
			Sprite tex = Resources.Load<Sprite>(textureName);


			if(tex != null) {
				
				switch(blending) {
				case AELayerBlendingType.ADD:
					plane.GetComponent<SpriteRenderer>().material = new Material (Shader.Find ("Particles/Additive"));
					break;
				}
				
				plane.GetComponent<SpriteRenderer>().sprite = tex;


				w = _layer.width / tex.bounds.size.x;
				h = _layer.height / tex.bounds.size.y;

			} else {
				Debug.LogWarning("Affter Effect: Texture " + textureName + " not found");
			}
		}



		
		plane.localScale = new Vector3 (w, h, 1); 
		

		
	}





	public override Color color {
		get {
			return plane.GetComponent<SpriteRenderer>().color;
		}
		
		set {
			plane.GetComponent<SpriteRenderer>().color = value;
		}
	}

	

}

