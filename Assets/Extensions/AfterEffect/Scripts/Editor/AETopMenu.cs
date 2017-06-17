////////////////////////////////////////////////////////////////////////////////
//  
// @module Affter Effect Importer
// @author Osipov Stanislav lacost.st@gmail.com
//
////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;
using System.Collections;

public class AETopMenu : EditorWindow {

	// Use this for initialization

	[MenuItem("GameObject/Create Other/Affter Effect/3D Animation")]
	public static void CreateAEAnimation() {
		AfterEffectAnimation AE  =  new GameObject ("AE Animation 3D").AddComponent<AfterEffectAnimation> ();
		SetPositionAndScale(AE.gameObject);

		AE.pivotCenterX = AEEditorConfig.PIVOT_X;
		AE.pivotCenterY = AEEditorConfig.PIVOT_Y;

		Selection.activeGameObject = AE.gameObject;
	}


    [MenuItem("GameObject/Create Other/Affter Effect/UI Animation")]
    public static void CreateAEAnimationUi()
    {
        GameObject AE = AEResourceManager.CreateAnimationRootUI();
        Transform canvas;
        if (FindObjectOfType<Canvas>() != null)
        {
            canvas = FindObjectOfType<Canvas>().gameObject.transform;
        }else
        {
            GameObject canvasGO =  Instantiate(Resources.Load("Canvas")) as GameObject;
            canvas = canvasGO.transform;
        }
        AE.transform.SetParent(canvas);
        RectTransform AErectTransform = AE.GetComponent<RectTransform>();
        AErectTransform.offsetMin = new Vector2(0, 0);
        AErectTransform.offsetMax = new Vector2(0, 0);
        AErectTransform.anchorMin = new Vector2(0f, 1f);
        AErectTransform.anchorMax = new Vector2(0f, 1f);
        AErectTransform.pivot = new Vector2(0, 1);

        AE.name = "AE Animation UI";
        AE.AddComponent<AfterEffectAnimationUi>();
        Selection.activeGameObject = AE.gameObject;
    }
		

	private static void SetPositionAndScale (GameObject obj) {
		obj.transform.localScale = AEEditorConfig.SCALE;
		obj.transform.position = AEEditorConfig.POSITION;

	}


}
