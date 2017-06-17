////////////////////////////////////////////////////////////////////////////////
//  
// @module Affter Effect Importer
// @author Osipov Stanislav lacost.st@gmail.com
//
////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


[ExecuteInEditMode]
[System.Serializable]
public class AfterEffectAnimation : MonoBehaviour {


	public const string ANIMATION_COMPLETE = "animation_complete";
	public const string FADE_COMPLETE = "fade_complete";
	public const string ENTER_FRAME = "enter_frame";

	public Action<int> OnEnterFrame = delegate {};
	public Action OnFadeComplete = delegate {};
	public Action OnAnimationComplete = delegate {};




	public TextAsset dataFile;
	public string imagesFolder = "";
	public Color GizmosColor = Color.green;
	public Color MaterialColor = Color.white;

	public int currentFrame = 0;
	public bool PlayOnStart = true;
	public bool Loop = true;
	public bool PlayBack = true;
	public bool PlayBackwards = false;

	public bool IsForceSelected = true;
	public bool CPUOptimization = true;

	public float opacity = 1f;


	public float pivotCenterX = 0f;
	public float pivotCenterY = 0f;

	[SerializeField]
	public Transform lastParent;

	[SerializeField]
	public AESettingsMode mode = AESettingsMode.Simple;

	public int normal_mode_shader = 1;
	public int add_mode_shader = 4;


	[SerializeField]
	private AEAnimationData _animationData = null;

	[SerializeField]
	private GameObject _zIndexHolder = null;

	[SerializeField]
	private GameObject _spritesHolder = null;

	[SerializeField]
	private float _timeScale = 25;

	[SerializeField]
	private float _frameDuration = 0.04f;



	private bool _isPlaying = false;

	[SerializeField]
	private List<AESprite> _sprites = new List<AESprite>();


	//--------------------------------------
	// INITIALIZE
	//--------------------------------------


	void Awake() {
		foreach(AESprite sprite in sprites) {
			sprite.WakeUp();
		}
	}

	void Start() {
		if(Application.isPlaying) {
			if(PlayOnStart) {
				Play ();
			}
		}
	}


	//--------------------------------------
	// PUBLIC METHODS
	//--------------------------------------


	public void Play() {
		if (_animationData == null) {
			return;
		}

		if(!_isPlaying) {
			PlayQueue ();
			_isPlaying = true;
		}

	}

	public void Stop() {
		if(_isPlaying) {
			CancelInvoke ("PlayQueue");
			_isPlaying = false;
		}
	}

	public void GoToAndStop(int index) {
		Stop ();
		currentFrame = index;
		GoToCurrentFrameForced ();
	}

	public void GoToAndPlay(int index) {
		currentFrame = index;
		GoToCurrentFrameForced ();
		Play ();
	}



	private void PlayQueue() {
		GoToCurrentFrame ();
		OnEnterFrame(currentFrame);

		if(PlayBackwards) {
			currentFrame--;
		} else {
			currentFrame++;
		}

		if(PlayBackwards) {
			if(currentFrame >= 0) {
				Invoke("PlayQueue", _frameDuration);
			} else {
				if(Loop) {
					currentFrame = lastFrame;
					Invoke("PlayQueue", _frameDuration);
				} else {
					currentFrame = 0;
					_isPlaying = false;
					OnAnimationComplete();
				}
			}
		} else {
			if(currentFrame != totalFrames) {
				Invoke("PlayQueue", _frameDuration);
			} else {
				if(Loop) {
					currentFrame = 0;
					Invoke("PlayQueue", _frameDuration);
				} else {
					currentFrame = lastFrame;
					_isPlaying = false;
					OnAnimationComplete();
				}
			}
		}


	}

	public void GoToCurrentFrameForced() {
		foreach(AESprite sprite in sprites) {
			sprite.GoToFrameForced (currentFrame);
		} 
	}

	public void GoToCurrentFrame() {
		if(!CPUOptimization) {
			GoToCurrentFrameForced();
			return;
		}

		foreach(AESprite sprite in sprites) {
			sprite.GoToFrame (currentFrame);
		} 
	}


	public AESprite GetSpriteByLayerId(int layerId) {
		foreach(AESprite sprite in sprites) {
			if(sprite.layerId == layerId) {
				return sprite;
			}
		} 

		Debug.LogWarning ("GetSpriteByLayerId  -> sprite not found, layer: " + layerId);
		return null;

	}

	public Shader GetNormalShader() {
		return AEShaders.shaders [normal_mode_shader];
	}

	public Shader GetAddShader() {
		return AEShaders.shaders [add_mode_shader];
	}



	public void AnimateOpacity(float valueFrom, float valueTo, float time) {
		AETween tw = AETween.Create(transform);
		tw.MoveTo(valueFrom, valueTo, time, OnOpacityAnimationEvent);
		tw.OnComplete = OnFadeCompleteEvent;
	}

	private void OnFadeCompleteEvent() {
		OnFadeComplete();
	}


	//--------------------------------------
	// EVENTS
	//--------------------------------------

	void  OnDrawGizmosSelected () {
		if(_animationData != null) {
			Gizmos.color = GizmosColor;

			Vector3 pos = Vector3.zero;

			pos.x += width / 2f;
			pos.y -= height / 2f;


			pos.x -= width * pivotCenterX;
			pos.y += height * pivotCenterY;

			Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

			Gizmos.matrix = rotationMatrix; 

			Gizmos.DrawWireCube (pos, new Vector3 (width, height, 0.01f));



		} else {
			if(dataFile != null) {
				OnAnimationDataChange ();
			}
		}
	}


	public virtual void UpdateColor() {
		SetColor(MaterialColor);
	}

	public virtual void SetColor(Color c) {
		foreach(AESprite sprite in sprites) {
			sprite.SetColor(c);
		}
	}



	public virtual void OnAnimationDataChange() {
		if(dataFile != null) {
			gameObject.name = "AE " + dataFile.name;

			#if !UNITY_WEBPLAYER
			_animationData = AEDataParcerWP8.ParceAnimationData (dataFile.text);
			#else
			_animationData = AEDataParcer.ParceAnimationData (dataFile.text);
			#endif

			InitSprites ();
		}

		timeScale = 1f;
		OnPivotPositionChnage ();
		SetColor(MaterialColor);

		int f = currentFrame;
		currentFrame = 0;
		OnEditorFrameChange();
		currentFrame = f;
		OnEditorFrameChange();
	}

	public void OnEditorFrameChange() {
		GoToCurrentFrameForced();

		OnPivotPositionChnage ();
	}

	public void OnPivotPositionChnage() {
		if(_animationData != null) {
			Vector3 pos = Vector3.zero;
			pos.x = -width * pivotCenterX;
			pos.y = height * pivotCenterY;
			spritesHolder.transform.localPosition = pos;
		}

	}


	private void OnOpacityAnimationEvent(float val) {
		opacity = val;
		GoToCurrentFrameForced();
	}




	//--------------------------------------
	// GET / SET
	//--------------------------------------

	public bool isPlaying {
		get {
			return _isPlaying;
		}
	}

	public float width {
		get {
			return _animationData.composition.width;
		}
	}

	public float height {
		get {
			return _animationData.composition.heigh;
		}
	}

	public AECompositionTemplate composition {
		get {
			return _animationData.composition;
		}
	}

	public int totalFrames {
		get {
			if(_animationData != null) {
				return _animationData.totalFrames;
			} else {
				return 0;
			}
		}
	}

	public int lastFrame {
		get {
			return totalFrames - 1;
		}
	}

	public AEAnimationData  animationData {
		get {
			return _animationData;
		}
	}

	public GameObject zIndexHolder {
		get {
			if(_zIndexHolder == null) {
				_zIndexHolder = new GameObject ("ZIndexHolder");
				_zIndexHolder.transform.parent = transform;
				_zIndexHolder.transform.localScale = Vector3.one;
				_zIndexHolder.transform.localPosition = Vector3.zero;
			}
			return _zIndexHolder;
		}
	}

	public GameObject spritesHolder {
		get {
			if(_spritesHolder == null) {
				_spritesHolder = new GameObject ("SpritesHolder");
				_spritesHolder.transform.parent = transform;
				_spritesHolder.transform.localScale = Vector3.one;
				_spritesHolder.transform.localPosition = Vector3.zero;
				_spritesHolder.transform.localRotation = Quaternion.identity;
			}
			return _spritesHolder;
		}
	}


	public float GetLayerGlobalZ(float index, AESprite sp) {

		zIndexHolder.transform.localPosition = new Vector3 (0, 0, index * AEConf.ZPosPadding);
		zIndexHolder.transform.parent = sp.transform.parent;
		float depth = zIndexHolder.transform.localPosition.z;

		zIndexHolder.transform.parent = transform;

		return depth;
	}




	public List<AESprite> sprites {
		get {
			return _sprites;
		}
	}




	public float timeScale  {
		get {
			return _timeScale;
		}

		set {
			_timeScale = value;
			if(_animationData != null) {
				_frameDuration = _animationData.frameDuration / _timeScale;
			}

		}
	}


	public float frameDuration {
		get {
			return _frameDuration;
		}
	}

	public float duration {
		get {
			return animationData.duration / timeScale;
		}
	}




	//--------------------------------------
	// PRIVATE METHODS
	//--------------------------------------

	private void InitSprites() {

		_sprites.Clear ();
		List<Transform> _childs = new List<Transform> ();
		foreach(Transform child in transform) {
			_childs.Add (child);
		} 

		foreach(Transform c in _childs) {
			DestroyImmediate (c.gameObject);
		}





		foreach(AELayerTemplate layer in _animationData.composition.layers) {
			AESprite sprite = null;


			switch(layer.type) {
			case AELayerType.FOOTAGE:
				if(layer.footageType == AEFootageType.FileSource) {
					sprite = CreateSpriteFootage ();
				} else {
					sprite = CreateFootage ();
				}

				break;
			case AELayerType.COMPOSITION:
				sprite = CreateComposition ();
				break;
			default:
				Debug.LogError ("Unsupported layer type: " + layer.type.ToString());
				break;

			}

			_sprites.Add(sprite);

			sprite.transform.parent = spritesHolder.transform;

			if(layer.parent != 0) {
				sprite.layerId = layer.index;
			} else {
				sprite.init (layer, this);
			}
		} 



		foreach(AELayerTemplate layer in _animationData.composition.layers) {
			if(layer.parent != 0) {
				AESprite p = GetSpriteByLayerId(layer.parent);
				AESprite c = GetSpriteByLayerId (layer.index);
				p.AddChild (c);
				c.init (layer, this);
			}
		} 

		OnEditorFrameChange ();

	}

	protected virtual AEFootage CreateFootage() {
		return AEResourceManager.CreateFootage ();
	}

	protected virtual AEFootage CreateSpriteFootage() {
		return AEResourceManager.CreateSpriteFootage ();
	}

	protected virtual AEComposition CreateComposition() {
		return AEResourceManager.CreateComposition ();
	}



	protected Vector3 GetWorldScale() {

		Vector3 worldScale = transform.localScale;
		Transform parent = transform.parent;


		while (parent != null) {
			worldScale = Vector3.Scale(worldScale,parent.localScale);
			parent = parent.parent;
		}
		return worldScale;
	}

	/*
	private void CheckInit() {
		if(!isInited) {
			foreach(AESprite sprite in sprites) {
				sprite.initInPlayMode (composition.GetLayer (sprite.layerId), this);
			} 

			isInited = true;
		}
	}
	*/

}
