// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyAStarPointGraphEditor.cs
// My implementation of a Pathfinding Graph Editor, customized for MyAStarPointGraph.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
#define UNITY_4
#endif

// default namespace

using CodeEnv.Master.Common;
using Pathfinding;
using UnityEditor;
using UnityEngine;

/// <summary>
/// My implementation of a Pathfinding Graph Editor, customized for MyAStarPointGraph.
/// </summary>
[CustomGraphEditor(typeof(MyAStarPointGraph), "MyAStarPointGraph")]
[System.Obsolete]
public class MyAStarPointGraphEditor : PointGraphEditor {

    public override void OnInspectorGUI(NavGraph target) {

        MyAStarPointGraph graph = target as MyAStarPointGraph;

        graph.maxDistance = EditorGUILayout.FloatField(new GUIContent("Max Distance", "The max distance in world space for a connection to be valid. A zero counts as infinity"), graph.maxDistance);

        EditorGUILayoutx.BeginIndent();
        graph.limits = EditorGUILayout.Vector3Field("Max Distance (axis aligned)", graph.limits);
        EditorGUILayoutx.EndIndent();

        graph.raycast = EditorGUILayout.Toggle(new GUIContent("Raycast", "Use raycasting to check if connections are valid between each pair of nodes"), graph.raycast);

        if (graph.raycast) {
            EditorGUI.indentLevel++;

            graph.thickRaycast = EditorGUILayout.Toggle(new GUIContent("Thick Raycast", "A thick raycast checks along a thick line with radius instead of just along a line"), graph.thickRaycast);

            if (graph.thickRaycast) {
                graph.thickRaycastRadius = EditorGUILayout.FloatField(new GUIContent("Raycast Radius", "The radius in world units for the thick raycast"), graph.thickRaycastRadius);
            }

            graph.mask = EditorGUILayoutx.LayerMaskField(/*new GUIContent (*/"Mask"/*,"Used to mask which layers should be checked")*/, graph.mask);
            EditorGUI.indentLevel--;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

