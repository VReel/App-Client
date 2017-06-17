////////////////////////////////////////////////////////////////////////////////
//  
// @module Affter Effect Importer
// @author Osipov Stanislav lacost.st@gmail.com
//
////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;

public class AEResourceManager  {


	public static AEFootage CreateFootage() {
		return (Object.Instantiate(Resources.Load("AEFootage")) as GameObject).GetComponent<AEFootage>();
	}
    public static AEFootageUI CreateFootageUI()
    {
        return (Object.Instantiate(Resources.Load("AEFootage")) as GameObject).GetComponent<AEFootageUI>();
    }


    public static AEFootage CreateSpriteFootage() {
		return (Object.Instantiate(Resources.Load("AESpriteFootage")) as GameObject).GetComponent<AEFootage>();
	}
    public static AEFootageUI CreateSpriteFootageUI()
    {
        return (Object.Instantiate(Resources.Load("AESpriteFootageUI")) as GameObject).GetComponent<AEFootageUI>();
    }

    public static AEFootage CreateUIFootage()
    {
        return (Object.Instantiate(Resources.Load("AEUIFootage")) as GameObject).GetComponent<AEFootage>();
    }
    public static AEFootageUI CreateUIFootageUI()
    {
        return (Object.Instantiate(Resources.Load("AEUIFootageUI")) as GameObject).GetComponent<AEFootageUI>();
    }










   /* public static GameObject CreateAnimationRoot() {
		return (Object.Instantiate(Resources.Load("AEAnimationRoot")) as GameObject);
	}*/

    public static GameObject CreateAnimationRootUI()
    {
        return (Object.Instantiate(Resources.Load("AEAnimationRootUI")) as GameObject);
    }




    public static AEComposition CreateComposition() {
		AEComposition comp = new GameObject ("AEComposition").AddComponent<AEComposition> ();
        comp.gameObject.AddComponent<RectTransform>();
		GameObject p =  new GameObject("Composition");
        p.AddComponent<RectTransform>();
		p.transform.SetParent(comp.transform);

		p.transform.localPosition = Vector3.zero;
		p.transform.localScale = Vector3.one;

		comp.plane = p.transform.GetComponent<RectTransform>();

		return comp;
	}

    public static AECompositionUI CreateCompositionUI()
    {
        AECompositionUI comp = new GameObject("AEComposition").AddComponent<AECompositionUI>();
        comp.gameObject.AddComponent<RectTransform>();
        GameObject p = new GameObject("Composition");
        p.AddComponent<RectTransform>();
        p.transform.SetParent(comp.transform);

        p.transform.localPosition = Vector3.zero;
        p.transform.localScale = Vector3.one;
        if (p.transform.GetComponent<RectTransform>().Equals(null))
        {
            Debug.LogError("Null!");
        }
        comp.plane = p.transform.GetComponent<RectTransform>();

        return comp;
    }
}
