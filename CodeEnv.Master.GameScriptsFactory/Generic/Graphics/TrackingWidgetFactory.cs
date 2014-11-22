// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TrackingWidgetFactory.cs
// Singleton Factory that creates preconfigured ATrackingWidgets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton Factory that creates preconfigured ATrackingWidgets.
/// </summary>
public class TrackingWidgetFactory : AGenericSingleton<TrackingWidgetFactory> {

    private TrackingWidgetFactory() {
        Initialize();
    }

    protected override void Initialize() { }

    /// <summary>
    /// Creates a label on the UI layer that tracks the <c>target</c>.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget CreateUITrackingLabel(IWidgetTrackable target, WidgetPlacement placement = WidgetPlacement.Over, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject prefab = RequiredPrefabs.Instance.uiTrackingLabel.gameObject;
        var clone = NGUITools.AddChild(DynamicWidgetsFolder.Instance.Folder.gameObject, prefab);

        Layers layerForTrackingWidget = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layerForTrackingWidget);

        var trackingWidget = clone.GetSafeInterface<ITrackingWidget>();
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        trackingWidget.Name = target.Transform.name + CommonTerms.Label;
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite on the UI layer that tracks the <c>target</c>.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget CreateUITrackingSprite(IWidgetTrackable target, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        // IMPROVE add ability to designate the type of atlas - eg. for fleets, starbases, etc. Use switch statement to select the desired prefab from ReqdPrefabs
        GameObject prefab = RequiredPrefabs.Instance.uiTrackingSprite.gameObject;
        var clone = NGUITools.AddChild(DynamicWidgetsFolder.Instance.Folder.gameObject, prefab);

        Layers layerForTrackingWidget = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layerForTrackingWidget);

        var trackingWidget = clone.GetSafeInterface<ITrackingWidget>();
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        trackingWidget.Name = target.Transform.name + CommonTerms.Sprite;
        return trackingWidget;
    }

    /// <summary>
    /// Creates a CmdIcon sprite whose size stays constant, parented to and tracking the <c>cmdTarget</c>.
    /// </summary>
    /// <param name="cmdTarget">The command target.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public CommandTrackingSprite CreateCmdTrackingSprite(IWidgetTrackable cmdTarget, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject prefab = RequiredPrefabs.Instance.worldTrackingSprite;
        GameObject clone = NGUITools.AddChild(cmdTarget.Transform.gameObject, prefab);

        Layers layer = CheckLayers(cmdTarget, prefab);
        NGUITools.SetLayer(clone, (int)layer);

        var trackingWidget = clone.AddComponent<CommandTrackingSprite>();   // AddComponent() runs Awake before returning
        trackingWidget.Target = cmdTarget;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        trackingWidget.Name = cmdTarget.Transform.name + CommonTerms.Sprite;
        //D.Log("CmdTrackingSprite made for {0}.", cmdTarget.Transform.name);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a label whose size scales with the size of the target, parented to and tracking the <c>target</c>.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget CreateVariableSizeTrackingLabel(IWidgetTrackable target, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF) {
        GameObject prefab = RequiredPrefabs.Instance.worldTrackingLabel;
        var clone = NGUITools.AddChild(target.Transform.gameObject, prefab);

        Layers layer = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layer);

        var trackingWidget = clone.AddComponent<VariableSizeTrackingLabel>();   // AddComponent() runs Awake before returning
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min);
        trackingWidget.Name = target.Transform.name + CommonTerms.Label;
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite whose size scales with the size of the target, parented to and tracking the <c>target</c>.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget CreateVariableSizeTrackingSprite(IWidgetTrackable target, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF) {
        // IMPROVE add ability to designate the type of atlas - eg. for fleets, starbases, etc. Use switch statement to select the desired prefab from ReqdPrefabs
        GameObject prefab = RequiredPrefabs.Instance.worldTrackingSprite;
        var clone = NGUITools.AddChild(target.Transform.gameObject, prefab);

        Layers layer = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layer);

        var trackingWidget = clone.AddComponent<VariableSizeTrackingSprite>();  // AddComponent() runs Awake before returning
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min);
        trackingWidget.Name = target.Transform.name + CommonTerms.Sprite;
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite whose size stays constant, independent of the size of the target, parented to and tracking the <c>target</c>.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget CreateConstantSizeTrackingSprite(IWidgetTrackable target, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        // IMPROVE add ability to designate the type of atlas - eg. for fleets, starbases, etc. Use switch statement to select the desired prefab from ReqdPrefabs
        GameObject prefab = RequiredPrefabs.Instance.worldTrackingSprite;
        var clone = NGUITools.AddChild(target.Transform.gameObject, prefab);

        Layers layer = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layer);

        var trackingWidget = clone.AddComponent<ConstantSizeTrackingSprite>();  // AddComponent() runs Awake before returning
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        trackingWidget.Name = target.Transform.name + CommonTerms.Sprite;
        return trackingWidget;
    }

    /// <summary>
    ///  Creates a label whose size stays constant, independent of the size of the target, parented to and tracking the <c>target</c>.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget CreateConstantSizeTrackingLabel(IWidgetTrackable target, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject prefab = RequiredPrefabs.Instance.worldTrackingLabel;
        var clone = NGUITools.AddChild(target.Transform.gameObject, prefab);

        Layers layer = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layer);

        var trackingWidget = clone.AddComponent<ConstantSizeTrackingLabel>();   // AddComponent() runs Awake before returning
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        trackingWidget.Name = target.Transform.name + CommonTerms.Label;
        return trackingWidget;
    }

    private Layers CheckLayers(IWidgetTrackable target, GameObject trackingWidgetPrefab) {
        Layers targetLayer = (Layers)target.Transform.gameObject.layer;
        Layers prefabLayer = (Layers)trackingWidgetPrefab.layer;
        if (prefabLayer != Layers.UI && prefabLayer != Layers.TransparentFX && prefabLayer != targetLayer) {
            D.Warn("Target {0} of Layer {1} being assigned TrackingWidget of Layer {2}.", target.Transform.name, targetLayer.GetName(), prefabLayer.GetName());
        }
        return prefabLayer;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

