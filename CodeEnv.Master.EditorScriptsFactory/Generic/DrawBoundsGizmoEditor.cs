// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DrawBoundsGizmoEditor.cs
// Editor script that executes the drawing code for DrawBoundsGizmo attached
// to the target game object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor script that executes the drawing code for DrawBoundsGizmo attached
/// to the target game object.
/// </summary>
[CustomEditor(typeof(DrawBoundsGizmo))]
public class DrawBoundsGizmoEditor : Editor {

    /// <summary>
    /// When the game object is selected this will draw the gizmos
    /// </summary>
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void RenderBoundsGizmo(DrawBoundsGizmo boundsGizmo, GizmoType gizmoType) {
        Gizmos.color = boundsGizmo.color;

        // get renderer bonding box
        var bounds = new Bounds();
        var initBound = false;
        if (UnityUtility.GetBoundWithChildren(boundsGizmo.transform, ref bounds, ref initBound)) {
            if (boundsGizmo.drawCube) {
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
            if (boundsGizmo.drawSphere) {
                Gizmos.DrawWireSphere(bounds.center, Mathf.Max(Mathf.Max(bounds.extents.x, bounds.extents.y), bounds.extents.z));
            }
        }

        if (boundsGizmo.showCenter) {
            Gizmos.DrawLine(new Vector3(bounds.min.x, bounds.center.y, bounds.center.z), new Vector3(bounds.max.x, bounds.center.y, bounds.center.z));
            Gizmos.DrawLine(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
            Gizmos.DrawLine(new Vector3(bounds.center.x, bounds.center.y, bounds.min.z), new Vector3(bounds.center.x, bounds.center.y, bounds.max.z));
        }

        // UnityEditor code draws a label with the dimensions of the bounding box
        Handles.BeginGUI();
        var view = SceneView.currentDrawingSceneView;
        var pos = view.camera.WorldToScreenPoint(bounds.center);
        var size = GUI.skin.label.CalcSize(new GUIContent(bounds.ToString()));
        GUI.Label(new Rect(pos.x - (size.x / 2), -pos.y + view.position.height + 4, size.x, size.y), bounds.ToString());
        Handles.EndGUI();
    }

}

