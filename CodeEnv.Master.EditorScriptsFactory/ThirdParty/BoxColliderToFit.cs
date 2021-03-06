﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BoxColliderToFit.cs
// Editor Class that constructs a single BoxCollider in a parent GameObject that fits around all the child meshes.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Class that constructs a single BoxCollider in a parent GameObject that fits around all the child meshes.
/// </summary>
public class BoxColliderToFit : MonoBehaviour {

    public string DebugName { get { return GetType().Name; } }

    /// <summary>
    /// Constructs a single BoxCollider in a parent GameObject that fits around all the child meshes.
    /// </summary>
    [MenuItem("My Tools/Collider/Fit to Children")]
    public static void FitToChildren() {
        foreach (GameObject rootGameObject in Selection.gameObjects) {
            BoxCollider boxCollider = rootGameObject.GetComponent<BoxCollider>();
            if (boxCollider == null) {
                // not a box collider
                continue;
            }

            bool hasBounds = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

            for (int i = 0; i < rootGameObject.transform.childCount; ++i) {
                Renderer childRenderer = rootGameObject.transform.GetChild(i).GetComponent<Renderer>();
                if (childRenderer != null) {
                    if (hasBounds) {
                        bounds.Encapsulate(childRenderer.bounds);
                    }
                    else {
                        bounds = childRenderer.bounds;
                        hasBounds = true;
                    }
                }
            }

            boxCollider.center = bounds.center - rootGameObject.transform.position;
            boxCollider.size = bounds.size;
        }
    }

    public override string ToString() {
        return DebugName;
    }

}

