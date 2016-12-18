﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RotateMesh.cs
// Takes a scaled, rotatedGameObject containing a mesh and makes a clone that has local scale of (1,1,1) and rotation (0,0,0). 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Takes a scaled, rotatedGameObject containing a mesh and makes a clone that has local scale of (1,1,1) and rotation (0,0,0). 
///  Usage: Select the gameObject in the hierarchy, and select 'RotateMesh' from the 'Window' menu. 
///  Courtesy of robertbu via UnityAnswers.
///  <remarks>When I have a mesh that is either the wrong size, wrong rotation (not facing +z) or both, this script can generate
///  a replacement gameObject of the size I desire rotated to face +z, aka just like they way I would desire an art object to be delivered
///  to me from an authoring package. To implement, take the existing mesh gameObject and scale it to the size I want, and rotate it
///  to face +z. Then select the gameObject and execute RotateMesh from the Editor's 'Window' menu. It will produce the gameObject
///  sized to what I desire, facing +z with a local scale of (1,1,1) and local rotation of (0,0,0).</remarks>
/// </summary>
public class RotateMesh : EditorWindow {

    /// <summary>
    /// The relative (to ProjectDirectory) save path for the mesh assets generated by this class.
    /// </summary>
    private static string _meshRelativeSavePath = "Assets/Models/RotateMesh/"; // my addition

    private string error = "";

    private string _newlyCreatedMeshName = "";  // my addition

    [MenuItem("Window/Rotate Mesh %#r")]
    public static void ShowWindow() {
        EditorWindow.GetWindow(typeof(RotateMesh));
    }

    void OnGUI() {
        Transform curr = Selection.activeTransform;
        GUILayout.Label("Creates a clone of the game object with a rotated mesh\n" +
            "so that the rotation will be (0,0,0) and the scale will\nbe (1,1,1).");
        GUILayout.Space(20);

        _newlyCreatedMeshName = EditorGUILayout.TextField("Mesh save name w/o ext", _newlyCreatedMeshName); // my addition

        GUILayout.Space(20);    // my addition

        if (GUILayout.Button("Rotate Mesh")) {
            error = "";
            SaveNewMeshName(curr.name); // my addition
            RotateTheMesh();
        }

        GUILayout.Space(20);
        GUILayout.Label(error);
    }


    // my addition
    private void SaveNewMeshName(string selectedTransformName) {
        _newlyCreatedMeshName = _newlyCreatedMeshName.Trim();
        // IMPROVE Check for illegal characters
        if (string.IsNullOrEmpty(_newlyCreatedMeshName)) {
            string dialogTitle = "No mesh save name provided";
            _newlyCreatedMeshName = selectedTransformName + Random.Range(0, int.MaxValue).ToString() + ".asset";
            string dialogMsg = "Mesh will be saved as " + _newlyCreatedMeshName + ".";
            string okButtonLabel = "OK";
            EditorUtility.DisplayDialog(dialogTitle, dialogMsg, okButtonLabel);
        }
    }

    void RotateTheMesh() {
        List<Transform> children = new List<Transform>();
        Transform curr = Selection.activeTransform;

        MeshFilter mf;
        if (curr == null) {
            error = "No appropriate object selected.";
            Debug.Log(error);
            return;
        }

        if (curr.localScale.x < 0.0 || curr.localScale.y < 0.0f || curr.localScale.z < 0.0f) {
            error = "Cannot process game object with negative scale values.";
            Debug.Log(error);
            return;
        }

        mf = curr.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) {
            error = "No mesh on the selected object";
            Debug.Log(error);
            return;
        }

        // Create the duplicate game object
        GameObject go = Instantiate(curr.gameObject) as GameObject;
        mf = go.GetComponent<MeshFilter>();
        mf.sharedMesh = Instantiate(mf.sharedMesh) as Mesh;
        curr = go.transform;

        // Disconnect any child objects and same them for later
        foreach (Transform child in curr) {
            if (child != curr) {
                children.Add(child);
                child.parent = null;
            }
        }

        // Rotate and scale the mesh
        Vector3[] vertices = mf.sharedMesh.vertices;
        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] = curr.TransformPoint(vertices[i]) - curr.position;
        }
        mf.sharedMesh.vertices = vertices;


        // Fix the normals
        Vector3[] normals = mf.sharedMesh.normals;
        if (normals != null) {
            for (int i = 0; i < normals.Length; i++)
                normals[i] = curr.rotation * normals[i];
        }
        mf.sharedMesh.normals = normals;
        mf.sharedMesh.RecalculateBounds();

        curr.transform.rotation = Quaternion.identity;
        curr.localScale = new Vector3(1, 1, 1);

        // Restore the children
        foreach (Transform child in children) {
            child.parent = curr;
        }

        // Set selection to new game object
        Selection.activeObject = curr.gameObject;

        //--- Do a rudimentary fix of mesh, box, and sphere colliders----
        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
        MeshCollider mc = curr.GetComponent<MeshCollider>();
        Profiler.EndSample();

        if (mc != null) {
            mc.sharedMesh = mf.sharedMesh;
        }

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
        BoxCollider bc = curr.GetComponent<BoxCollider>();
        Profiler.EndSample();

        if (bc != null) {
            DestroyImmediate(bc);

            Profiler.BeginSample("Proper AddComponent allocation");
            curr.gameObject.AddComponent<BoxCollider>();
            Profiler.EndSample();
        }

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
        SphereCollider sc = curr.GetComponent<SphereCollider>();
        Profiler.EndSample();

        if (sc != null) {
            DestroyImmediate(sc);

            Profiler.BeginSample("Proper AddComponent allocation");
            curr.gameObject.AddComponent<SphereCollider>();
            Profiler.EndSample();
        }

        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)");
        var col = curr.GetComponent<Collider>();
        Profiler.EndSample();

        if (col) {
            error = "Be sure to verify size of collider.";
        }

        // Save a copy to disk
        string meshSaveName = _newlyCreatedMeshName + ".asset"; // my addition
        AssetDatabase.CreateAsset(mf.sharedMesh, _meshRelativeSavePath + meshSaveName);
        AssetDatabase.SaveAssets();
        //string name = "Assets/Editor/" + go.name + Random.Range(0, int.MaxValue).ToString() + ".asset";
        //AssetDatabase.CreateAsset(mf.sharedMesh, name);
        //AssetDatabase.SaveAssets();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

