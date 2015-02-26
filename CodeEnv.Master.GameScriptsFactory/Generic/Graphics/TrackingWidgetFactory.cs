// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TrackingWidgetFactory.cs
// Singleton Factory that creates preconfigured ITrackingWidgets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton Factory that creates preconfigured ITrackingWidgets.
/// </summary>
public class TrackingWidgetFactory : AGenericSingleton<TrackingWidgetFactory> {
    // Note: no reason to dispose of _instance during scene transition as all its references persist across scenes

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

        //var trackingWidget = clone.GetSafeInterface<ITrackingWidget>();
        var trackingWidget = clone.GetSafeMonoBehaviourComponent<UITrackingLabel>();
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(UITrackingLabel).Name, target.DisplayName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite on the UI layer that tracks the <c>target</c>.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="atlasID">The atlas identifier.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget CreateUITrackingSprite(IWidgetTrackable target, IconAtlasID atlasID, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject prefab = RequiredPrefabs.Instance.uiTrackingSprite.gameObject;
        var clone = NGUITools.AddChild(DynamicWidgetsFolder.Instance.Folder.gameObject, prefab);

        Layers layerForTrackingWidget = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layerForTrackingWidget);

        UISprite sprite = clone.GetSafeMonoBehaviourComponentInChildren<UISprite>();
        sprite.atlas = GetAtlas(atlasID);

        //var trackingWidget = clone.GetSafeInterface<ITrackingWidget>();
        var trackingWidget = clone.GetSafeMonoBehaviourComponent<UITrackingSprite>();
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(UITrackingSprite).Name, target.DisplayName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a tracking sprite which can interact with the mouse.
    /// The sprite's size stays constant, parented to and tracks the <c>target</c>.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="atlasID">The atlas identifier.</param>
    /// <param name="__dimensions">The desired dimensions of the sprite in pixels.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public InteractableTrackingSprite CreateInteractableTrackingSprite(IWidgetTrackable target, IconAtlasID atlasID, Vector2 __dimensions, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject prefab = RequiredPrefabs.Instance.worldTrackingSprite;
        GameObject clone = NGUITools.AddChild(target.Transform.gameObject, prefab);

        Layers layer = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layer);

        UISprite sprite = clone.GetSafeMonoBehaviourComponentInChildren<UISprite>();
        sprite.atlas = GetAtlas(atlasID);

        var trackingWidget = clone.AddComponent<InteractableTrackingSprite>();   // AddComponent() runs Awake before returning
        trackingWidget.__SetDimensions(Mathf.RoundToInt(__dimensions.x), Mathf.RoundToInt(__dimensions.y));
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(InteractableTrackingSprite).Name, target.DisplayName);
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
        D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(VariableSizeTrackingSprite).Name, target.DisplayName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite whose size scales with the size of the target, parented to and tracking the <c>target</c>.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="atlasID">The atlas identifier.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget CreateVariableSizeTrackingSprite(IWidgetTrackable target, IconAtlasID atlasID, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF) {
        GameObject prefab = RequiredPrefabs.Instance.worldTrackingSprite;
        var clone = NGUITools.AddChild(target.Transform.gameObject, prefab);

        Layers layer = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layer);

        UISprite sprite = clone.GetSafeMonoBehaviourComponentInChildren<UISprite>();
        sprite.atlas = GetAtlas(atlasID);

        var trackingWidget = clone.AddComponent<VariableSizeTrackingSprite>();  // AddComponent() runs Awake before returning
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min);
        D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(VariableSizeTrackingSprite).Name, target.DisplayName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite whose size stays constant, independent of the size of the target, parented to and tracking the <c>target</c>.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="atlasID">The atlas identifier.</param>
    /// <param name="__dimensions">The desired dimensions of the sprite in pixels.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget CreateConstantSizeTrackingSprite(IWidgetTrackable target, IconAtlasID atlasID, Vector2 __dimensions, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject prefab = RequiredPrefabs.Instance.worldTrackingSprite;
        var clone = NGUITools.AddChild(target.Transform.gameObject, prefab);

        Layers layer = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layer);

        UISprite sprite = clone.GetSafeMonoBehaviourComponentInChildren<UISprite>();
        sprite.atlas = GetAtlas(atlasID);

        var trackingWidget = clone.AddComponent<ConstantSizeTrackingSprite>();  // AddComponent() runs Awake before returning
        trackingWidget.__SetDimensions(Mathf.RoundToInt(__dimensions.x), Mathf.RoundToInt(__dimensions.y));
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(ConstantSizeTrackingSprite).Name, target.DisplayName);
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
        D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(ConstantSizeTrackingSprite).Name, target.DisplayName);
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

    private UIAtlas GetAtlas(IconAtlasID atlasID) {
        switch (atlasID) {
            case IconAtlasID.Fleet:
                return RequiredPrefabs.Instance.fleetIconAtlas;
            case IconAtlasID.Contextual:
                return RequiredPrefabs.Instance.contextualAtlas;
            case IconAtlasID.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(atlasID));
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    public enum IconAtlasID {

        None,

        Fleet,

        Contextual

    }

}

