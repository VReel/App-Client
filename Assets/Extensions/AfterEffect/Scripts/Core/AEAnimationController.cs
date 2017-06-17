using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[System.Serializable]
public class AEAnimationController : MonoBehaviour {
	
	[SerializeField]
	public bool PlayOnStart;
	
	[SerializeField]
	public List<AEClipTemplate> _clips;
	
	[SerializeField]
	public float opacity = 1f;
	
	[SerializeField]
	public AEClipTemplate currentClip;
	
	private bool IsFading = false;
	
	
	void Awake() {
		if(PlayOnStart) {
			if(currentClip != null) {
                if (currentClip.animUI != null)
                    currentClip.animUI.Play();
                else
                    currentClip.anim.Play();

            }
		}
	}
	
	
	//--------------------------------------
	// PUBLIC METHODS
	//--------------------------------------
	
	
	public bool RegisterClip(AfterEffectAnimation anim) {
		
		bool IsRegisterd = false;
		foreach(AEClipTemplate clip in clips) {
			if(clip.anim == anim) {
				IsRegisterd = true;
			}
		}
		
		if(!IsRegisterd) {
			AEClipTemplate tpl =  new AEClipTemplate();
			tpl.anim = anim;
			tpl.name = anim.dataFile.name;
			anim.transform.localPosition = Vector3.zero;
			
			if(anim.Loop) {
				tpl.wrapMode = AEWrapMode.Loop;
			} else {
				tpl.wrapMode = AEWrapMode.Once;
			}
			
			anim.PlayOnStart = false;
			clips.Add(tpl);
			
			return true;
		} else {
			return false;
		}
		
	}

    public bool RegisterClipUI(AfterEffectAnimationUi anim)
    {

        bool IsRegisterd = false;
        foreach (AEClipTemplate clip in clips)
        {
            if (clip.animUI == anim)
            {
                IsRegisterd = true;
            }
        }

        if (!IsRegisterd)
        {
            AEClipTemplate tpl = new AEClipTemplate();
            tpl.animUI = anim;
            tpl.name = anim.dataFile.name;
            anim.transform.localPosition = Vector3.zero;

            if (anim.Loop)
            {
                tpl.wrapMode = AEWrapMode.Loop;
            }
            else
            {
                tpl.wrapMode = AEWrapMode.Once;
            }

            anim.PlayOnStart = false;
            clips.Add(tpl);

            return true;
        }
        else
        {
            return false;
        }

    }

    public void CrossFade(string clip, float time) {
		
		if(IsFading) {
			return;
		}
		
		
		AEClipTemplate c = GetClipByName(clip);
		if(c != null) {
			
			
			if(!c.name.Equals(currentClip.name)) {
				IsFading = true;
                if(currentClip.animUI != null)
                {
                    currentClip.animUI.OnFadeComplete += OnFadeComplete;
                    currentClip.animUI.AnimateOpacity(currentClip.animUI.opacity, 0f, time);


                    c.animUI.gameObject.SetActive(true);

                    if (c.wrapMode == AEWrapMode.Loop)
                    {
                        c.animUI.Loop = true;
                    }
                    else
                    {
                        c.animUI.Loop = false;
                    }

                    c.animUI.opacity = 0f;
                    c.animUI.gameObject.SetActive(true);
                    c.animUI.AnimateOpacity(0f, 1f, time);
                    c.animUI.GoToAndPlay(0);

                    currentClip = c;
                }else
                {
                    currentClip.anim.OnFadeComplete += OnFadeComplete;
                    currentClip.anim.AnimateOpacity(currentClip.anim.opacity, 0f, time);


                    c.anim.gameObject.SetActive(true);

                    if (c.wrapMode == AEWrapMode.Loop)
                    {
                        c.anim.Loop = true;
                    }
                    else
                    {
                        c.anim.Loop = false;
                    }

                    c.anim.opacity = 0f;
                    c.anim.gameObject.SetActive(true);
                    c.anim.AnimateOpacity(0f, 1f, time);
                    c.anim.GoToAndPlay(0);

                    currentClip = c;
                }
			} 
			
			
		}
	}

	void OnFadeComplete () {

		currentClip.anim.gameObject.SetActive(false);
        currentClip.animUI.gameObject.SetActive(false);
        IsFading = false;
	}
	
	public void SetClipName(string name, AfterEffectAnimation anim) {
		foreach(AEClipTemplate clip in clips) {
			if(clip.anim == anim) {
				clip.name = name;
			}
		}
	}

    public void SetClipNameUI(string name, AfterEffectAnimationUi anim)
    {
        foreach (AEClipTemplate clip in clips)
        {
            if (clip.animUI == anim)
            {
                clip.name = name;
            }
        }
    }

    public AEClipTemplate GetClipByName(string name) {
		foreach(AEClipTemplate clip in clips) {
			if(clip.name == name) {
				return clip;
			}
		}
		
		return null;
	}
	
	public void SetDefaultClip(AEClipTemplate tpl) {
		foreach(AEClipTemplate clip in clips) {
			clip.defaultClip = false;
		}
		
		tpl.defaultClip = true;
	}
	
	public void ClearClips() {
		clips.Clear();
	}
	
	public void CleanUpLayers() {
		foreach(AEClipTemplate clip in clips.ToArray()) {
			if(clip.anim.transform.parent != transform) {
				clips.Remove(clip);
			}
            if (clip.anim.transform.parent != transform)
            {
                clips.Remove(clip);
            }
        }
		
		if(clips.Count == 0) {
			currentClip = null;
			return;
		}
		
		bool hasDefault = false;
		foreach(AEClipTemplate clip in clips) {
			if(clip.defaultClip) {
				hasDefault = true;
			}
		}
		
		if(!hasDefault) {
			clips[0].defaultClip = true;
		}
		
		foreach(AEClipTemplate clip in clips) {
			clip.anim.PlayOnStart = false;
			if(!clip.defaultClip) {
				clip.anim.gameObject.SetActive(false);
			} else {
				currentClip = clip;
				clip.anim.gameObject.SetActive(true);
			}

		}
	}

    public void CleanUpLayersUI()
    {
        foreach (AEClipTemplate clip in clips.ToArray())
        {
            if (clip.animUI.transform.parent != transform)
            {
                clips.Remove(clip);
            }
            if (clip.animUI.transform.parent != transform)
            {
                clips.Remove(clip);
            }
        }

        if (clips.Count == 0)
        {
            currentClip = null;
            return;
        }

        bool hasDefault = false;
        foreach (AEClipTemplate clip in clips)
        {
            if (clip.defaultClip)
            {
                hasDefault = true;
            }
        }

        if (!hasDefault)
        {
            clips[0].defaultClip = true;
        }

        foreach (AEClipTemplate clip in clips)
        {
            clip.animUI.PlayOnStart = false;
            if (!clip.defaultClip)
            {
                clip.animUI.gameObject.SetActive(false);
            }
            else
            {
                currentClip = clip;
                clip.animUI.gameObject.SetActive(true);
            }

        }
    }

    //--------------------------------------
    // GET / SET
    //--------------------------------------

    public List<AEClipTemplate> clips {
		get {
			if(_clips == null) {
				_clips =  new List<AEClipTemplate>();
			}
			return _clips;
		}
	}
	
	public AEClipTemplate clip {
		get {
			return currentClip;
		}
	}
	
	
	
}
