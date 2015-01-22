// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DrawTriggerCollider.cs
// Draws a Gizmo wire outline of the trigger collider(s) that are attached to the gameObject this script is attached too
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Draws a Gizmo wire outline of the trigger collider(s) that are attached 
/// to the gameObject this script is attached too.
/// <remarks>Derived from RyanMeier.</remarks>
/// <see cref="http://www.ryan-meier.com/blog"/>
/// </summary>
public class DrawTriggerCollider : AMonoBase {

    public bool activate;

    public bool drawOnlyWhenSelected;

    public Color color = Color.white;

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
        if (activate && !drawOnlyWhenSelected) {
            DrawGizmos();
        }
    }

    void OnDrawGizmosSelected() {
        if (activate && drawOnlyWhenSelected) {
            DrawGizmos();
        }
    }

    private void DrawGizmos() {
        Color oldColor = Gizmos.color;
        Gizmos.color = color;
        if (BoxCollider != null) {
            var colliderCenter = _transform.position + BoxCollider.center;
            var colliderSize = new Vector3(BoxCollider.size.x, BoxCollider.size.y, BoxCollider.size.z);
            Gizmos.DrawWireCube(colliderCenter, colliderSize);
        }
        if (SphereCollider != null) {
            var colliderCenter = _transform.position + SphereCollider.center;
            Gizmos.DrawWireSphere(colliderCenter, SphereCollider.radius);
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

