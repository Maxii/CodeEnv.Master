// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PooledSphericalHighlightEditor.cs
// Custom Editor for the PooledSphericalHighlight Monobehaviour.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor for the PooledSphericalHighlight Monobehaviour.
/// </summary>
[CustomEditor(typeof(PooledSphericalHighlight))]
public class PooledSphericalHighlightEditor : Editor {

    public override void OnInspectorGUI() {
        var script = target as PooledSphericalHighlight;

        script.enableTrackingLabel = EditorGUILayout.Toggle(new GUIContent("Tracking Label", "Check to show a tracking label."), script.enableTrackingLabel);

        if (GUI.changed) {
            EditorUtility.SetDirty(target);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

