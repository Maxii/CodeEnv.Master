// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RendererBoundsGizmo.cs
// Visualizes render bounds for debugging in the Editor Scene window.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
//using UnityEditor;
using UnityEngine;

/// <summary>
/// Visualizes render bounds for debugging in the Editor Scene window. Just attach it to a game object and it will
/// draw the render bounds of the object and all its children.
/// </summary>
public class DrawBoundsGizmo : MonoBehaviour {

    public bool showCenter;

    public Color color = Color.white;

    public bool drawCube = true;

    public bool drawSphere = false;

    // Best way to draw gizmos is from an Editor script in the Editor folder - see DrawBoundsGizmoEditor.
    // Below is the alternative way to draw Gizmos but since much of the gizmo drawing logic
    // is located in UnityEditor (Handles, SceneView below), placing this code within the editor folder
    // keeps using UnityEditor out of our scripts (as it breaks builds)

    /// <summary>
    /// When the game object is selected this will draw the gizmos
    /// </summary>
    /// <remarks>Only called when in the Unity editor.</remarks>
    //private void OnDrawGizmosSelected() {
    //    Gizmos.color = this.color;

    //    // get renderer bonding box
    //    var bounds = new Bounds();
    //    var initBound = false;
    //    if (UnityUtility.GetBoundWithChildren(this.transform, ref bounds, ref initBound)) {
    //        if (this.drawCube) {
    //            Gizmos.DrawWireCube(bounds.center, bounds.size);
    //        }
    //        if (this.drawSphere) {
    //            Gizmos.DrawWireSphere(bounds.center, Mathf.Max(Mathf.Max(bounds.extents.x, bounds.extents.y), bounds.extents.z));
    //        }
    //    }

    //    if (this.showCenter) {
    //        Gizmos.DrawLine(new Vector3(bounds.min.x, bounds.center.y, bounds.center.z), new Vector3(bounds.max.x, bounds.center.y, bounds.center.z));
    //        Gizmos.DrawLine(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
    //        Gizmos.DrawLine(new Vector3(bounds.center.x, bounds.center.y, bounds.min.z), new Vector3(bounds.center.x, bounds.center.y, bounds.max.z));
    //    }

    //    //Handles.BeginGUI();
    //    //var view = SceneView.currentDrawingSceneView;
    //    //var pos = view.camera.WorldToScreenPoint(bounds.center);
    //    //var size = GUI.skin.label.CalcSize(new GUIContent(bounds.ToString()));
    //    //GUI.Label(new Rect(pos.x - (size.x / 2), -pos.y + view.position.height + 4, size.x, size.y), bounds.ToString());
    //    //Handles.EndGUI();
    //}

}

