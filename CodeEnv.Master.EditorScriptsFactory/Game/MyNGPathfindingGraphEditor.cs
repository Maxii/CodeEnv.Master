// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyNGPathfindingGraphEditor.cs
// Custom Editor for MyNGPathfindingGraph.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using Pathfinding;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor for MyNGPathfindingGraph.
/// </summary>
[CustomGraphEditor(typeof(MyNGPathfindingGraph), "MyNGPathfindingGraph")]
public class MyNGPathfindingGraphEditor : GraphEditor {

    public string DebugName { get { return GetType().Name; } }

    public override void OnInspectorGUI(NavGraph target) {
        var graph = target as MyNGPathfindingGraph;

        EditorGUIUtility.labelWidth = 200F;
        graph.maxDistance = EditorGUILayout.FloatField(new GUIContent("Max Distance", "The max distance in world space for a connection to be valid. A zero counts as infinity"), graph.maxDistance, GUILayout.MaxWidth(240F));

        graph.optimizeForSparseGraph = EditorGUILayout.Toggle(new GUIContent("Optimize For Sparse Graph", "Check on line documentation for more information."), graph.optimizeForSparseGraph);

        graph.raycast = EditorGUILayout.Toggle(new GUIContent("Use raycasts", "Use raycasts to determine the validity of node connections."), graph.raycast);

        if (graph.raycast) {
            EditorGUI.indentLevel++;

            graph.thickRaycast = EditorGUILayout.Toggle(new GUIContent("Use thick raycasts", "Use thick raycasts to determine the validity of node connections."), graph.thickRaycast);

            if (graph.thickRaycast) {
                EditorGUI.indentLevel++;
                graph.thickRaycastRadius = EditorGUILayout.FloatField(new GUIContent("Thick raycast radius", "The radius of the thick raycast used to determine valid node connections."), graph.thickRaycastRadius, GUILayout.MaxWidth(240F));
                EditorGUI.indentLevel--;
            }

            graph.mask = EditorGUILayoutx.LayerMaskField("Mask", graph.mask);
            EditorGUI.indentLevel--;
        }
    }

    public override string ToString() {
        return DebugName;
    }

}

