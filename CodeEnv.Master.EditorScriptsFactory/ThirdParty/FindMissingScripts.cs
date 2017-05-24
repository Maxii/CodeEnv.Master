﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FindMissingScripts.cs
// Editor extension that adds a MenuItem to search out any missing scripts in the startScene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

#if (UNITY_EDITOR)
using UnityEditor;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Editor extension that adds a MenuItem to search out any missing scripts in the startScene.
/// </summary>
public class FindMissingScripts : EditorWindow {

    static int go_count = 0, components_count = 0, missing_count = 0;

    [MenuItem("Window/FindMissingScripts")]
    public static void ShowWindow() {
        EditorWindow.GetWindow(typeof(FindMissingScripts));
    }

    public string DebugName { get { return GetType().Name; } }

    public void OnGUI() {
        if (GUILayout.Button("Find Missing Scripts in selected GameObjects")) {
            FindInSelected();
        }
    }

    private static void FindInSelected() {
        GameObject[] go = Selection.gameObjects;
        go_count = 0;
        components_count = 0;
        missing_count = 0;
        foreach (GameObject g in go) {
            FindInGO(g);
        }
        Debug.Log(string.Format("Searched {0} GameObjects, {1} components, found {2} missing", go_count, components_count, missing_count));
    }

    private static void FindInGO(GameObject g) {
        go_count++;
        Component[] components = g.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++) {
            components_count++;
            if (components[i] == null) {
                missing_count++;
                Debug.Log(g.name + " has an empty script attached in _location: " + i, g);
            }
        }
        // Now recurse through each child GO (if there are any):
        foreach (Transform childT in g.transform) {
            //Debug.Log("Searching " + childT.item  + " " );
            FindInGO(childT.gameObject);
        }
    }

    public override string ToString() {
        return DebugName;
    }

}
#endif

