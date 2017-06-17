using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class AESpriteUIFootageUI : AEFootageUI {

	public override void SetMaterial() {

		if(_layer.footageType == AEFootageType.FileSource){
			string textureName = _anim.imagesFolder + _layer.sourceNoExt;
			Sprite tex = Resources.Load<Sprite>(textureName);


			if(tex != null) {

				plane.GetComponent<Image>().sprite = tex;


                //w = _layer.width / tex.bounds.size.x;
                //h = _layer.height / tex.bounds.size.y;

                w = _layer.width;
                h = _layer.height;


            } else {
				Debug.LogWarning("Affter Effect: Texture " + textureName + " not found");
			}
		}

        //if (w < 0)
        //{
        //    w = -w;
        //}
        //if (h < 0)
        //{
        //    h = -h;
        //}
        //plane.anchorMin = new Vector2(0f, 1f);
        //plane.anchorMax = new Vector2(0f, 1f);
        //plane.pivot = new Vector2(0, 1);
        plane.anchorMin = new Vector2(0f, 1f);
        plane.anchorMax = new Vector2(0f, 1f);
        plane.pivot = new Vector2(0, 1);


        Debug.Log("AESpriteUIFootage. SetMaterial(). Size delta: x = " + w + ", y = " + h);
        plane.sizeDelta = new Vector2(w, h);
       // plane.localPosition = Vector3.zero;
		plane.localScale = new Vector3 (1, 1, 1); 



	}


	public override Color color {
		get {
			return plane.GetComponent<Image>().color;
		}

		set {
			plane.GetComponent<Image>().color = value;
		}
	}



}