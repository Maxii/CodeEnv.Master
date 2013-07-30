// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Hayden Scott-Baron (Dock) - http://starfruitgames.com
// 19 Oct 2012
// </copyright> 
// <summary> 
// File: ScaleRelativeToCamera.cs
// Scales object relative to camera. Useful for GUI objects attached to game objects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Scales an object relative to the distance to the camera. Gives the appearance of the object size being the same
/// while the camera moves. Useful for GUI objects attached to game objects. Often useful when combined with CameraFacing.
/// 
///Usage: Place this script on the gameobject you wish to keep a constant size. Measures the distance from the Camera cameraPlane, 
///rather than the camera itself, and uses the initial scale as a basis. Use the public objectScale variable to adjust the object size on the screen.
/// </summary>
public class ScaleRelativeToCamera : MonoBehaviour {

    public float objectScale = 1.0f;
    private Transform _cameraTransform;
    private Vector3 _initialScale;
    private Transform _transform;

    void Awake() {
        _transform = transform;
    }

    // set the initial scale, and setup reference camera
    void Start() {
        // record initial scale of the GO and use it as a basis
        _initialScale = _transform.localScale;

        // if no specific camera, grab the default camera
        if (_cameraTransform == null)
            _cameraTransform = Camera.main.transform;
    }

    // scale object relative to distance from camera cameraPlane
    void Update() {
        Plane cameraPlane = new Plane(_cameraTransform.forward, _cameraTransform.position);
        float distanceToCamera = cameraPlane.GetDistanceToPoint(_transform.position);
        _transform.localScale = _initialScale * distanceToCamera * objectScale;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

