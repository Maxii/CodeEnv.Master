// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiTrackingLabelFactory.cs
// Singleton Factory that creates preconfigured GuiTrackingLabels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton Factory that creates preconfigured GuiTrackingLabels.
/// </summary>
public class GuiTrackingLabelFactory : AGenericSingleton<GuiTrackingLabelFactory> {


    public enum LabelPlacement {

        None,

        AboveTarget,

        OverTarget,

        BelowTarget

    }

    private GuiTrackingLabelFactory() {
        Initialize();
    }

    protected override void Initialize() { }

    /// <summary>
    /// Creates a GUI tracking label centered over the target.
    /// </summary>
    /// <param name="target">The target to track.</param>
    /// <param name="placement">The placement of the label.</param>
    /// <param name="minShowDistance">The minimum show distance. Default is zero.</param>
    /// <param name="maxShowDistance">The maximum show distance. Default is infinity.</param>
    /// <param name="text">Text to show on the label. If left blank, the name of the target will be used.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public GuiTrackingLabel CreateGuiTrackingLabel(Transform target, LabelPlacement placement, float minShowDistance = 0F, float maxShowDistance = Mathf.Infinity, string text = "") {
        Vector3 pivotOffset;
        Vector3 viewportOffsetFromPivot;

        switch (placement) {
            case LabelPlacement.AboveTarget:
                pivotOffset = new Vector3(0F, target.collider.bounds.extents.y, 0F);
                viewportOffsetFromPivot = new Vector3(0F, 0.05F, 0F);
                break;
            case LabelPlacement.OverTarget:
                pivotOffset = Vector3.zero;
                viewportOffsetFromPivot = Vector3.zero;
                break;
            case LabelPlacement.BelowTarget:
                pivotOffset = new Vector3(0F, -target.collider.bounds.extents.y, 0F);
                viewportOffsetFromPivot = new Vector3(0F, -0.05F, 0F);
                break;
            case LabelPlacement.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(placement));
        }
        return CreateGuiTrackingLabel(target, pivotOffset, viewportOffsetFromPivot, minShowDistance, maxShowDistance, text);
    }

    /// <summary>
    /// Creates a GUI tracking label centered on the pivotOffset from the target.
    /// </summary>
    /// <param name="target">The target to track.</param>
    /// <param name="pivotOffset">The pivot point offset from the target in Worldspace coordinates..</param>
    /// <param name="minShowDistance">The minimum show distance. Default is zero.</param>
    /// <param name="maxShowDistance">The maximum show distance. Default is infinity.</param>
    /// <param name="text">Text to show on the label. If left blank, the name of the target will be used.</param>
    /// <returns></returns>
    public GuiTrackingLabel CreateGuiTrackingLabel(Transform target, Vector3 pivotOffset, float minShowDistance = 0F, float maxShowDistance = Mathf.Infinity, string text = "") {
        return CreateGuiTrackingLabel(target, pivotOffset, Vector3.zero, minShowDistance, maxShowDistance, text);
    }

    /// <summary>
    /// Creates a GUI tracking label centered on the pivotOffset from the target.
    /// </summary>
    /// <param name="target">The target to track.</param>
    /// <param name="pivotOffset">The pivot point offset from the target in Worldspace coordinates.</param>
    /// <param name="viewportOffsetFromPivot">The offset from pivot point in Viewport coordinates.</param>
    /// <param name="minShowDistance">The minimum show distance. Default is zero.</param>
    /// <param name="maxShowDistance">The maximum show distance. Default is infinity.</param>
    /// <param name="text">Text to show on the label. If left blank, the name of the target will be used.</param>
    /// <returns></returns>
    public GuiTrackingLabel CreateGuiTrackingLabel(Transform target, Vector3 pivotOffset, Vector3 viewportOffsetFromPivot, float minShowDistance = 0F, float maxShowDistance = Mathf.Infinity, string text = "") {
        GameObject guiTrackingLabelPrefab = RequiredPrefabs.Instance.guiTrackingLabel.gameObject;
        if (guiTrackingLabelPrefab == null) {
            D.Error("Prefab of Type {0} is not present.".Inject(typeof(GuiTrackingLabel).Name));
            return null;
        }
        GameObject guiTrackingLabelCloneGO = NGUITools.AddChild(DynamicTrackingLabels.Folder.gameObject, guiTrackingLabelPrefab);
        // NGUITools.AddChild handles all scale, rotation, position, parent and layer settings
        if (text == string.Empty) { text = target.name; }
        guiTrackingLabelCloneGO.name = text + CommonTerms.Label;  // readable name of runtime instantiated label

        GuiTrackingLabel trackingLabel = guiTrackingLabelCloneGO.GetSafeMonoBehaviourComponent<GuiTrackingLabel>();
        // assign the System as the Target of the tracking label
        trackingLabel.Target = target;
        trackingLabel.TargetPivotOffset = pivotOffset;
        trackingLabel.ViewportOffsetFromPivot = viewportOffsetFromPivot;
        trackingLabel.MinimumShowDistance = minShowDistance;
        trackingLabel.MaximumShowDistance = maxShowDistance;
        trackingLabel.Set(text);
        NGUITools.SetActive(guiTrackingLabelCloneGO, true);
        //D.Log("A new {0} for {1} has been created.".Inject(typeof(GuiTrackingLabel), target.name));
        return trackingLabel;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

