////////////////////////////////////////////////////////////////////////////////
//  
// @module <module_name>
// @author Osipov Stanislav lacost.st@gmail.com
//
////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AECompositionUI : AESpriteUI {


	private bool _isEnabled = true;

	[SerializeField]
	public float opacity;


	[SerializeField]
	private AECompositionTemplate composition;

	[SerializeField]
	private List<AESpriteUI> _sprites = new List<AESpriteUI>();
	

	//--------------------------------------
	// INITIALIZE
	//--------------------------------------

	//--------------------------------------
	//  PUBLIC METHODS
	//--------------------------------------
	 
	public override void WakeUp() {
		foreach(AESpriteUI sprite in sprites) {
			sprite.WakeUp ();
		} 
	}
	

	public override void init(AELayerTemplate layer, AfterEffectAnimationUi animation,  AELayerBlendingType forcedBlending) {

		base.init (layer, animation, forcedBlending);

		gameObject.name = layer.name + " (Composition)";
	
		composition = animation.animationData.getCompositionById (layer.id);
        //  Debug.Log(layer.index + ", " + layer.name);
        //    gameObject.transform.SetSiblingIndex(10 - layer.index);

        gameObject.transform.SetAsFirstSibling();
		InitSprites ();
		ApplayCompositionFrame (0);
	}




	public override void disableRenderer () {

		if(_isEnabled) {
			foreach(AESpriteUI sprite in sprites) {
				sprite.disableRenderer ();
			} 

			_isEnabled = false;
		}


	}

	public override void enableRenderer () {

		if(!_isEnabled) {
			foreach(AESpriteUI sprite in sprites) {
				sprite.enableRenderer ();
			} 

			_isEnabled = true;
		}

	}

	
	public override void GoToFrameForced (int index) {
		int frameIndex = 0;

		if(index >= _layer.inFrame) {
			frameIndex = index - _layer.inFrame;
		} else {
			disableRenderer ();
			return;
		}
		
		ApplayCompositionFrameForced(frameIndex);
	}
	
	
	public void ApplayCompositionFrameForced(int frameIndex) {
        RectTransform m_transform = GetComponent<RectTransform>();

		AEFrameTemplate frame = _layer.GetFrame (frameIndex);
		if(frame == null) {
			disableRenderer ();
			return;
		}

		enableRenderer ();

		Vector3 pos;
        	pos = frame.positionUnity;
     //   pos = frame.position;
        Debug.Log("AEComposition,  ApplayCompositionFrameForced position x: " + pos.x + ", y= " + pos.y + ", z=" + pos.z);
    //    Debug.Log(animation.animationData.getCompositionById(0). );
       // composition.width
        m_transform.localPosition = pos;
		
	

		
		plane.localPosition = new Vector3 (-frame.pivot.x, frame.pivot.y, 0f);

        RectTransform m_childAncorTransform = childAnchor.GetComponent<RectTransform>();

        //childAnchor.transform.localPosition = plane.transform.localPosition;
        //childAnchor.transform.localScale = Vector3.one;

        m_childAncorTransform.localPosition = plane.transform.localPosition;
        m_childAncorTransform.localScale = Vector3.one;

        pos = plane.localPosition;
		//TODO remove z index caclulcation
		pos.z = _anim.GetLayerGlobalZ (zIndex, this);
		plane.localPosition = pos;
        //Debug.Log("x=" + pos.x + ", y=" + pos.y + ", z=" + pos.z);
		//plane.sizeDelta = new Vector2()

		
		m_transform.localRotation = Quaternion.Euler(new Vector3 (0f, 0f, -frame.rotation));
		


		
		m_transform.localScale = frame.scale;
		



		opacity = frame.opacity * 0.01f * parentOpacity;


		foreach(AESpriteUI sprite in sprites) {
			sprite.GoToFrameForced (frameIndex);
		} 
	}
	
	
	
	
	public override void GoToFrame(int index) {

		int frameIndex = 0;

		if(index >= _layer.inFrame) {
			frameIndex = index - _layer.inFrame;
		} else {
			disableRenderer ();
			return;
		}
		
		ApplayCompositionFrame(frameIndex);
	}
	
	
	
	public void ApplayCompositionFrame(int frameIndex) {
		AEFrameTemplate frame = _layer.GetFrame (frameIndex);
		if(frame == null) {
			disableRenderer ();
			return;
		}

		enableRenderer ();

		if(frame.IsPositionChanged) {
            		Vector3 pos = frame.positionUnity;
        //    Vector3 pos = frame.position;
       //     Debug.Log("AEComposition,  ApplayCompositionFrame position x: " + pos.x + ", y= " + pos.y + ", z=" + pos.z);
            transform.localPosition = pos;
		}


        if (frame.IsPivotChnaged)
        {
            plane.localPosition = new Vector3(-frame.pivot.x, frame.pivot.y, 0f);

            childAnchor.transform.localPosition = plane.transform.localPosition;
            childAnchor.transform.localScale = Vector3.one;


            Vector3 pos = plane.localPosition;
            //TODO remove z index caclulcation
            pos.z = _anim.GetLayerGlobalZ(zIndex, this);
            plane.localPosition = pos;
        }

        if (frame.IsRotationChanged) {
			transform.localRotation = Quaternion.Euler(new Vector3 (0f, 0f, -frame.rotation));
		}


		if(frame.IsScaleChanged) {
			transform.localScale = frame.scale;
		}



		opacity = frame.opacity * 0.01f * parentOpacity;


		foreach(AESpriteUI sprite in sprites) {
			sprite.GoToFrame (frameIndex);
		} 
	}
	
	
	public override void SetColor(Color c) {
		foreach(AESpriteUI sprite in sprites) {
			sprite.SetColor(c);
		} 
	}
	
	
	
	//--------------------------------------
	//  GET/SET
	//--------------------------------------


	public List<AESpriteUI> sprites {
		get {
			return _sprites;
		}
	}
	

	//--------------------------------------
	//  EVENTS
	//--------------------------------------
	
	//--------------------------------------
	//  PRIVATE METHODS
	//--------------------------------------

	private void InitSprites() {

		_sprites.Clear ();


		foreach(AELayerTemplate layer in composition.layers) {
			AESpriteUI sprite = null;

			layer.forcedBlending = _layer.blending;

			switch(layer.type) {
				case AELayerType.FOOTAGE:
				sprite = CreateFootage();
				break;
				case AELayerType.COMPOSITION:
				sprite = CreateComposition ();
				break;
                   
				default:
				Debug.LogError ("Unsupported layer type: " + layer.type.ToString());
				break;

			}

			_sprites.Add(sprite);
           // Debug.Log(sprite.GetComponent<RectTransform>().sizeDelta.x);
			sprite.transform.SetParent(plane.transform);
			sprite.parentIndex = zIndex;
			sprite.indexModifayer = indexModifayer * 0.01f;

			if(layer.parent != 0) {
				sprite.layerId = layer.index;
			} else {
				sprite.init (layer, _anim, blending); 
			}
		} 



		foreach(AELayerTemplate layer in composition.layers) {
			if(layer.parent != 0) {
				AESpriteUI p = GetSpriteByLayerId(layer.parent);
				AESpriteUI c = GetSpriteByLayerId (layer.index);
				p.AddChild (c);
				c.init (layer, _anim, blending); 
			}
		} 

		foreach(AESpriteUI sprite in sprites) {
			sprite.parentComposition = this;
		} 

	}


	public AESpriteUI GetSpriteByLayerId(int layerId) {
		foreach(AESpriteUI sprite in sprites) {
			if(sprite.layerId == layerId) {
				return sprite;
			}
		} 

		Debug.LogWarning ("GetSpriteByLayerId  -> sprite not found, layer: " + layerId);
		return null;

	}
	


	protected virtual AEFootageUI CreateFootage() {
		return AEResourceManager.CreateSpriteFootageUI ();
	}

	protected virtual AECompositionUI CreateComposition() {
		return AEResourceManager.CreateCompositionUI ();
	}

	
	//--------------------------------------
	//  DESTROY
	//--------------------------------------

}
