// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Hayden Scott-Baron (Dock) - http://starfruitgames.com
// 19 Oct 2012
// </copyright> 
// <summary> 
// File: ScaleRelativeToCamera.cs
// Scales object relative to camera. Useful for GUI and items that appear in world space.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;


/// <summary>
/// Scales an object relative to the distance to the camera. Gives the appearance of the object size being the same
/// while the camera moves. Useful for GUI objects that appear within the game scene. Often useful when combined with CameraFacing.
/// 
///Usage: Place this script on the gameobject you wish to keep a constant size. Measures the distance from the Camera cameraPlane, 
///rather than the camera itself, and uses the initial scale as a basis. Use the public objectScale variable to adjust the object size on the screen.
/// </summary>
public class ScaleRelativeToCamera : MonoBehaviour {

    public float objectScale = 1.0f;
    private Transform cameraTransform;
    private Vector3 initialScaleOfGO;
    private Transform goTransform;

    void Awake() {
        goTransform = transform;
    }

    // set the initial scale, and setup reference camera
    void Start() {
        // record initial scale of the GO and use it as a basis
        initialScaleOfGO = goTransform.localScale;

        // if no specific camera, grab the default camera
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    // scale object relative to distance from camera cameraPlane
    void Update() {
        Plane cameraPlane = new Plane(cameraTransform.forward, cameraTransform.position);
        float distanceToCamera = cameraPlane.GetDistanceToPoint(goTransform.position);
        goTransform.localScale = initialScaleOfGO * distanceToCamera * objectScale;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

