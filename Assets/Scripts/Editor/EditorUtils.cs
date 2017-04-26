using UnityEngine;
using UnityEditor;
using System.Collections.Generic;   //List

public class EditorUtils
{
    // **************************
    // Private/Helper functions
    // **************************

    [MenuItem("CONTEXT/Component/Find Scene References for this Asset")]
    private static void FindReferences(MenuCommand data)
    {
        Object context = data.context;
        if (context)
        {
            var comp = context as Component;
            if (comp)
            {
                FindReferencesTo(comp);
            }
        }
    }

    [MenuItem("Assets/Find Scene References for this Asset")]
    private static void FindReferencesToAsset(MenuCommand data)
    {
        var selected = Selection.activeObject;
        if (selected)
        {
            FindReferencesTo(selected);
        }
    }

    private static void FindReferencesTo(Object to)
    {
        var referencedBy = new List<Object>();
        var allObjects = Object.FindObjectsOfType<GameObject>();
        for (int j = 0; j < allObjects.Length; j++)
        {
            var go = allObjects[j];

            if (PrefabUtility.GetPrefabType(go) == PrefabType.PrefabInstance)
            {
                if (PrefabUtility.GetPrefabParent(go) == to)
                {
                    Debug.Log(string.Format("Referenced by {0}, {1}", go.name, go.GetType()), go);
                    referencedBy.Add(go);
                }
            }

            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                var c = components[i];
                if (!c) continue;

                var so = new SerializedObject(c);
                var sp = so.GetIterator();

                while (sp.NextVisible(true))
                {
                    if (sp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (sp.objectReferenceValue == to)
                        {
                            Debug.Log(string.Format("Referenced by {0}, {1}", c.name, c.GetType()), c);
                            referencedBy.Add(c.gameObject);
                        }
                    }
                }
            }
        }

        if (referencedBy.Count > 0)
        {
            Selection.objects = referencedBy.ToArray();
        }
        else 
        {
            Debug.Log("No references in scene");
        }
    }
}