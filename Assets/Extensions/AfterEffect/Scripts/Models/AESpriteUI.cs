////////////////////////////////////////////////////////////////////////////////
//  
// @module <module_name>
// @author Osipov Stanislav lacost.st@gmail.com
//
////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using UnityEngine.UI;


[System.Serializable]
public abstract class AESpriteUI : MonoBehaviour {

	public int layerId;
	public float zIndex;
	public float parentIndex = 0;
	public float indexModifayer = 1f;


	[SerializeField]
	public RectTransform plane; 


	[SerializeField]
	protected AfterEffectAnimationUi _anim;

	[SerializeField]
	private GameObject _childAnchor = null;

	[SerializeField]
	public AECompositionUI parentComposition = null;


	[SerializeField]
	public AELayerBlendingType blending = AELayerBlendingType.NORMAL;
	

	[SerializeField]
	protected AELayerTemplate _layer;


	//--------------------------------------
	// INITIALIZE
	//--------------------------------------
	
	public abstract void WakeUp();
	


	public virtual void init(AELayerTemplate layer, AfterEffectAnimationUi animation) {
		init (layer, animation, AELayerBlendingType.NORMAL);
	}

	public virtual void init(AELayerTemplate layer, AfterEffectAnimationUi animation, AELayerBlendingType forcedBlending) {
		_layer = layer;
        Debug.Log("init");
        if (_layer.Equals(null))
        {
            Debug.LogError("LayerIsNull!!");
        }
		_anim = animation;
		layerId = layer.index;

		zIndex = parentIndex + (layer.index) * indexModifayer;

		if(forcedBlending == AELayerBlendingType.NORMAL) {
			blending = _layer.blending;
		} else {
			blending = forcedBlending;
		}

	}


	public abstract void GoToFrame (int index);
	public abstract void GoToFrameForced (int index);
	public abstract void disableRenderer ();
	public abstract void enableRenderer ();
	public abstract void SetColor(Color c);
	
	//--------------------------------------
	//  PUBLIC METHODS
	//--------------------------------------

	public void AddChild(AESpriteUI sprite) {
		sprite.transform.SetParent( childAnchor.transform);
	}
	

	//--------------------------------------
	//  GET/SET
	//--------------------------------------


	public float parentOpacity {
		get {
			if(parentComposition != null) {
				return parentComposition.opacity;
			} else {
				return 1f;
			}
		}
	}


	public GameObject childAnchor {
		get {
			if(_childAnchor == null) {
				_childAnchor = new GameObject ("ChildAnchor");
                _childAnchor.AddComponent<RectTransform>();
                _childAnchor.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);
				_childAnchor.transform.SetParent( gameObject.transform);
				_childAnchor.transform.localPosition = plane.localPosition;
			}

			return _childAnchor;

		}
	}

	
	//--------------------------------------
	//  EVENTS
	//--------------------------------------
	
	//--------------------------------------
	//  PRIVATE METHODS
	//--------------------------------------
	
	//--------------------------------------
	//  DESTROY
	//--------------------------------------

}
