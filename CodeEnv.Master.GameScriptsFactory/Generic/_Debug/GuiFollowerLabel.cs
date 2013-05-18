// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiFollowerLabel.cs
// A label that follows a designated GameObject target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

// Warning: Unused and untested. guiCamera is intended to be the Ngui 2D camera.

/// <summary>
/// A label that follows a designated GameObject target.
/// </summary>
[Obsolete]
public class GuiFollowerLabel : MonoBehaviourBase {

    // TODO position label on the game object by creating an attachment point

    private Camera mainCamera;
    private Camera guiCamera;

    [SerializeField]
    private GameObject followTarget;  // the GameObject the label is to follow
    private Transform _transform;   // cached transform of the GameObject holding this script
    private UILabel _label; // cached transform of the label itself. 

    public string Text {
        get { return _label.text; }
        set { _label.text = value; }
    }

    public Color TextColor {
        get { return _label.color; }
        set { _label.color = value; }
    }

    public UIFont Font {
        get { return _label.font; }
        set { _label.font = value; }
    }

    //public Vector3 FontSize {
    //    get { return _label.transform.localScale; }
    //    set { _label.transform.localScale = value; }
    //}

    /// <summary>
    /// Gets or sets the GameObject the label is to follow.
    /// </summary>
    public GameObject FollowTarget {
        get { return followTarget; }
        set {
            followTarget = value;
            mainCamera = NGUITools.FindCameraForLayer(followTarget.layer);
        }
    }

    public bool Active { get; set; }

    void Awake() {
        _label = GetComponentInChildren<UILabel>(); // the UILabel can be a direct component or a component of a child
        _transform = transform;
    }

    void Start() {
        guiCamera = NGUITools.FindCameraForLayer(_transform.gameObject.layer);
        if (followTarget != null) {
            mainCamera = NGUITools.FindCameraForLayer(followTarget.layer);
            Active = true;
        }
    }

    void LateUpdate() {
        if (followTarget == null || !Active) { return; }

        Vector3 position = mainCamera.WorldToViewportPoint(followTarget.transform.position);
        position = guiCamera.ViewportToWorldPoint(position);
        //position.z = 0.0f;  // keep this hover label within the clip range of the 2D camera
        _transform.position = position;
    }

    public void SpawnAt(GameObject target, Vector3 size, Transform parent) {
        FollowTarget = target;
        _transform.parent = parent;
        //FontSize = size;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

