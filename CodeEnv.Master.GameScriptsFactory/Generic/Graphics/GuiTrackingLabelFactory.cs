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

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Singleton Factory that creates preconfigured GuiTrackingLabels.
/// </summary>
public class GuiTrackingLabelFactory : AMonoBehaviourBaseSingleton<GuiTrackingLabelFactory> {

    /// <summary>
    /// Creates a GUI tracking label centered over the target.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <returns></returns>
    public static GuiTrackingLabel CreateGuiTrackingLabel(Transform target) {
        return CreateGuiTrackingLabel(target, Vector3.zero, Vector3.zero);
    }

    /// <summary>
    /// Creates the GUI tracking label.
    /// </summary>
    /// <param name="target">The target to track.</param>
    /// <param name="pivotOffset">The pivot point offset from the target in Worldspace coordinates.</param>
    /// <param name="offsetFromPivot">The offset from pivot point in Viewport coordinates.</param>
    /// <returns></returns>
    public static GuiTrackingLabel CreateGuiTrackingLabel(Transform target, Vector3 pivotOffset, Vector3 offsetFromPivot) {
        GameObject guiTrackingLabelPrefab = RequiredPrefabs.Instance.GuiTrackingLabelPrefab.gameObject;
        if (guiTrackingLabelPrefab == null) {
            D.Error("Prefab of Type {0} is not present.".Inject(typeof(GuiTrackingLabel).Name));
            return null;
        }
        GameObject guiTrackingLabelCloneGO = NGUITools.AddChild(DynamicTrackingLabels.Folder.gameObject, guiTrackingLabelPrefab);
        // NGUITools.AddChild handles all scale, rotation, posiition, parent and layer settings
        guiTrackingLabelCloneGO.name = target.name + CommonTerms.Label;  // readable name of runtime instantiated label

        GuiTrackingLabel trackingLabel = guiTrackingLabelCloneGO.GetSafeMonoBehaviourComponent<GuiTrackingLabel>();
        // assign the System as the Target of the tracking label
        trackingLabel.Target = target;
        trackingLabel.TargetPivotOffset = pivotOffset;
        trackingLabel.OffsetFromPivot = offsetFromPivot;
        trackingLabel.Set(target.name);
        NGUITools.SetActive(guiTrackingLabelCloneGO, true);
        //Logger.Log("A new {0} for {1} has been created.".Inject(typeof(GuiTrackingLabel), target.name));
        return trackingLabel;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

