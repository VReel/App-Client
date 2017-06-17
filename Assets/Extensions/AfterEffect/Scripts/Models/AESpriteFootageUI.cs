using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class AESpriteFootageUI : AEFootageUI {


	public override void SetMaterial() {

		if(_layer.footageType == AEFootageType.FileSource){
			string textureName = _anim.imagesFolder + _layer.sourceNoExt;
			Sprite tex = Resources.Load<Sprite>(textureName);


			if(tex != null) {
				
				switch(blending) {
				case AELayerBlendingType.ADD:
				//	plane.GetComponent<SpriteRenderer>().material = new Material (Shader.Find ("Particles/Additive"));
                        plane.GetComponent<Image>().material = new Material(Shader.Find("Particles/Additive"));

                        break;
				}
				
			//	plane.GetComponent<SpriteRenderer>().sprite = tex;
               plane.GetComponent<Image>().sprite = tex;

		//		w = _layer.width / tex.bounds.size.x;
		//		h = _layer.height / tex.bounds.size.y;

                w = _layer.width ;
                h = _layer.height ;

            } else {
				Debug.LogWarning("Affter Effect: Texture " + textureName + " not found");
			}
		}

        plane.anchorMin = new Vector2(0, 1);
        plane.anchorMax = new Vector2(0, 1);
        plane.pivot = new Vector2(0, 1);
        Debug.Log("Sprites Layer w= " + w + ", h= " + h);
        plane.sizeDelta = new Vector2(w, h);
		//plane.localScale = new Vector3 (w, h, 1); 
		

		
	}





	public override Color color {
		get {
            //	return plane.GetComponent<SpriteRenderer>().color;
            return plane.GetComponent<Image>().color;
        }
		
		set {
			//plane.GetComponent<SpriteRenderer>().color = value;
            plane.GetComponent<Image>().color = value;
		}
	}

	

}

