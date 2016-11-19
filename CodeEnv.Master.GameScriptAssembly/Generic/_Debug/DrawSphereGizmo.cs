// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DrawSphereGizmo.cs
// Draws a Gizmo wire outline of a Sphere around this GameObject with a Radius and CenterOffset from the GameObject's position.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Draws a Gizmo wire outline of a Sphere around this GameObject
/// with a Radius and CenterOffset from the GameObject's position.
/// <remarks>Derived from RyanMeier.</remarks>
/// <see cref="http://www.ryan-meier.com/blog"/>
/// </summary>
public class DrawSphereGizmo : AMonoBase {

    public bool DrawSelected { get; set; }

    private Color _color = Color.white;
    public Color Color {
        get { return _color; }
        set { _color = value; }
    }

    public float Radius { get; set; }

    public Vector3 CenterOffset { get; set; }

    void OnDrawGizmos() {
        if (enabled && !DrawSelected) {
            DrawGizmos();
        }
    }

    void OnDrawGizmosSelected() {
        if (enabled && DrawSelected) {
            DrawGizmos();
        }
    }

    private void DrawGizmos() {
        if (Radius == Constants.ZeroF) { return; }
        Color oldColor = Gizmos.color;
        Gizmos.color = _color;
        var center = transform.position + CenterOffset;
        Gizmos.DrawWireSphere(center, Radius);
        Gizmos.color = oldColor;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

