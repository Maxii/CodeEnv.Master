﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DrawColliderGizmo.cs
// Draws a Gizmo wire outline of the collider(s) that are attached to the gameObject this script is attached too.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Draws a Gizmo wire outline of the collider(s) that are attached 
/// to the gameObject this script is attached too.
/// <remarks>Derived from RyanMeier.</remarks>
/// <see cref="http://www.ryan-meier.com/blog"/>
/// </summary>
public class DrawColliderGizmo : AMonoBase {

    public string DebugName { get { return GetType().Name; } }

#pragma warning disable 0414
    [SerializeField]
    private string _note1 = "To Hide all, collapse component";
    [SerializeField]
    private string _note2 = "Enable to show wireframe.";
#pragma warning restore 0414

    [Tooltip("Draw only when selected")]
    [SerializeField]
    private bool _drawSelected = false;

    [SerializeField]
    private Color _color = Color.white;
    public Color Color {
        get { return _color; }
        set { _color = value; }
    }

    private BoxCollider _boxCollider;
    public BoxCollider BoxCollider {
        get {
            if (_boxCollider == null) { _boxCollider = GetComponent<BoxCollider>(); }
            return _boxCollider;
        }
    }

    private SphereCollider _sphereCollider;
    public SphereCollider SphereCollider {
        get {
            if (_sphereCollider == null) { _sphereCollider = GetComponent<SphereCollider>(); }
            return _sphereCollider;
        }
    }

    void OnDrawGizmos() {
        if (enabled && !_drawSelected) {
            DrawGizmos();
        }
    }

    void OnDrawGizmosSelected() {
        if (enabled && _drawSelected) {
            DrawGizmos();
        }
    }

    private void DrawGizmos() {
        Color oldColor = Gizmos.color;
        Gizmos.color = _color;
        if (BoxCollider != null) {
            var colliderCenter = transform.position + BoxCollider.center;
            var colliderSize = new Vector3(BoxCollider.size.x, BoxCollider.size.y, BoxCollider.size.z);
            Gizmos.DrawWireCube(colliderCenter, colliderSize);
        }
        if (SphereCollider != null) {
            var colliderCenter = transform.position + SphereCollider.center;
            Gizmos.DrawWireSphere(colliderCenter, SphereCollider.radius);
        }
        Gizmos.color = oldColor;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return DebugName;
    }

}

