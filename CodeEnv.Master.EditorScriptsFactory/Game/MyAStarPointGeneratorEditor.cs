// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyAStarPointGeneratorEditor.cs
// My implementation of a Pathfinding Graph Editor, customized for MyAStarPointGenerator.
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
/// My implementation of a Pathfinding Graph Editor, customized for MyAStarPointGenerator.
/// </summary>
[CustomGraphEditor(typeof(MyAStarPointGenerator), "MyAStarPointGenerator")]
public class MyAStarPointGeneratorEditor : GraphEditor {

    public override void OnInspectorGUI(NavGraph target) {
        MyAStarPointGenerator graph = target as MyAStarPointGenerator;

        graph.maxDistance = EditorGUILayout.FloatField(new GUIContent("Max Distance", "The max distance in world space for a connection to be valid. A zero counts as infinity"), graph.maxDistance);

        EditorGUIUtility.LookLikeControls();
        EditorGUILayoutx.BeginIndent();
        graph.limits = EditorGUILayout.Vector3Field("Max Distance (axis aligned)", graph.limits);
        EditorGUILayoutx.EndIndent();
        //EditorGUIUtility.LookLikeInspector(); // deprecated

        graph.raycast = EditorGUILayout.Toggle(new GUIContent("Raycast", "Use raycasting to check if connections are valid between each pair of nodes"), graph.raycast);

        editor.GUILayoutx.BeginFadeArea(graph.raycast, "raycast");

        EditorGUI.indentLevel++;

        graph.thickRaycast = EditorGUILayout.Toggle(new GUIContent("Thick Raycast", "A thick raycast checks along a thick line with radius instead of just along a line"), graph.thickRaycast);

        editor.GUILayoutx.BeginFadeArea(graph.thickRaycast, "thickRaycast");

        graph.thickRaycastRadius = EditorGUILayout.FloatField(new GUIContent("Raycast Radius", "The radius in world units for the thick raycast"), graph.thickRaycastRadius);

        editor.GUILayoutx.EndFadeArea();

        graph.mask = EditorGUILayoutx.LayerMaskField(/*new GUIContent (*/"Mask"/*,"Used to mask which layers should be checked")*/, graph.mask);
        EditorGUI.indentLevel--;

        editor.GUILayoutx.EndFadeArea();
    }

    //public override void OnDrawGizmos() {
    //    MyAStarPointGenerator graph = target as MyAStarPointGenerator;
    //    if (graph == null) {
    //        return;
    //    }

    //    Gizmos.color = new Color(0.161F, 0.341F, 1F, 0.5F);

    //    //if (graph.root != null) {
    //    //    DrawChildren (graph, graph.root);
    //    //} else {

    //    //    GameObject[] gos = GameObject.FindGameObjectsWithTag (graph.searchTag);
    //    //    for (int i=0;i<gos.Length;i++) {
    //    //        Gizmos.DrawCube (gos[i].transform.position,Vector3.one*HandleUtility.GetHandleSize(gos[i].transform.position)*0.1F);
    //    //    }
    //    //}
    //}

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

