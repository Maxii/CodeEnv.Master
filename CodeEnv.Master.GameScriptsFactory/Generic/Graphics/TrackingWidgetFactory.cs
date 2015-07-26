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
/// Singleton Factory that creates preconfigured ITrackingWidgets.
/// </summary>
public class TrackingWidgetFactory : AGenericSingleton<TrackingWidgetFactory>, ITrackingWidgetFactory, IDisposable {
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
    public ITrackingWidget MakeUITrackingLabel(IWidgetTrackable target, WidgetPlacement placement = WidgetPlacement.Over, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject prefab = RequiredPrefabs.Instance.uiTrackingLabel.gameObject;
        var clone = NGUITools.AddChild(DynamicWidgetsFolder.Instance.Folder.gameObject, prefab);

        Layers layerForTrackingWidget = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layerForTrackingWidget);

        var trackingWidget = clone.GetSafeMonoBehaviour<UITrackingLabel>();
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(UITrackingLabel).Name, target.DisplayName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite on the UI layer that tracks the <c>target</c>.
    /// IMPROVE If there is an AtlasID, then their should also be a SpriteName and Color. Use IconInfo?
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="atlasID">The atlas identifier.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget MakeUITrackingSprite(IWidgetTrackable target, AtlasID atlasID, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject prefab = RequiredPrefabs.Instance.uiTrackingSprite.gameObject;
        var clone = NGUITools.AddChild(DynamicWidgetsFolder.Instance.Folder.gameObject, prefab);

        Layers layerForTrackingWidget = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layerForTrackingWidget);

        var trackingWidget = clone.GetSafeMonoBehaviour<UITrackingSprite>();
        trackingWidget.AtlasID = atlasID;
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(UITrackingSprite).Name, target.DisplayName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a tracking sprite which can respond to the mouse.
    /// The sprite's size stays constant, parented to and tracks the <c>target</c>.
    /// </summary>
    /// <param name="target">The target this sprite will track.</param>
    /// <param name="iconInfo">The info needed to build the sprite.</param>
    /// <param name="size">The size of the sprite in pixels.</param>
    /// <param name="placement">The placement of the sprite relative to the target.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public IResponsiveTrackingSprite MakeResponsiveTrackingSprite(IWidgetTrackable target, IconInfo iconInfo, Vector2 size, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject prefab = RequiredPrefabs.Instance.worldTrackingSprite;
        GameObject clone = NGUITools.AddChild(target.Transform.gameObject, prefab);

        Layers layer = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layer);

        var trackingSprite = clone.AddComponent<ResponsiveTrackingSprite>();   // AddComponent() runs Awake before returning
        trackingSprite.__SetDimensions(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));
        trackingSprite.Target = target;
        trackingSprite.IconInfo = iconInfo;
        trackingSprite.Placement = placement;
        trackingSprite.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(ResponsiveTrackingSprite).Name, target.DisplayName);
        return trackingSprite;
    }

    /// <summary>
    /// Creates a label whose size scales with the size of the target, parented to and tracking the <c>target</c>.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget MakeVariableSizeTrackingLabel(IWidgetTrackable target, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF) {
        GameObject prefab = RequiredPrefabs.Instance.worldTrackingLabel;
        var clone = NGUITools.AddChild(target.Transform.gameObject, prefab);

        Layers layer = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layer);

        var trackingWidget = clone.AddComponent<VariableSizeTrackingLabel>();   // AddComponent() runs Awake before returning
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min);
        //D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(VariableSizeTrackingSprite).Name, target.DisplayName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite whose size scales with the size of the target, parented to and tracking the <c>target</c>.
    /// IMPROVE If there is an AtlasID, then their should also be a SpriteName and Color. Use IconInfo?
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="atlasID">The atlas identifier.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget MakeVariableSizeTrackingSprite(IWidgetTrackable target, AtlasID atlasID, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF) {
        GameObject prefab = RequiredPrefabs.Instance.worldTrackingSprite;
        var clone = NGUITools.AddChild(target.Transform.gameObject, prefab);

        Layers layer = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layer);

        var trackingWidget = clone.AddComponent<VariableSizeTrackingSprite>();  // AddComponent() runs Awake before returning
        trackingWidget.AtlasID = atlasID;
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min);
        //D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(VariableSizeTrackingSprite).Name, target.DisplayName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite whose size stays constant, independent of the size of the target, parented to and tracking the <c>target</c>.
    /// IMPROVE If there is an AtlasID, then their should also be a SpriteName and Color. Use IconInfo?
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="atlasID">The atlas identifier.</param>
    /// <param name="__dimensions">The desired dimensions of the sprite in pixels.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget MakeConstantSizeTrackingSprite(IWidgetTrackable target, AtlasID atlasID, Vector2 __dimensions, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject prefab = RequiredPrefabs.Instance.worldTrackingSprite;
        var clone = NGUITools.AddChild(target.Transform.gameObject, prefab);

        Layers layer = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layer);

        var trackingWidget = clone.AddComponent<ConstantSizeTrackingSprite>();  // AddComponent() runs Awake before returning
        trackingWidget.__SetDimensions(Mathf.RoundToInt(__dimensions.x), Mathf.RoundToInt(__dimensions.y));
        trackingWidget.AtlasID = atlasID;
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(ConstantSizeTrackingSprite).Name, target.DisplayName);
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
    public ITrackingWidget MakeConstantSizeTrackingLabel(IWidgetTrackable target, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject prefab = RequiredPrefabs.Instance.worldTrackingLabel;
        var clone = NGUITools.AddChild(target.Transform.gameObject, prefab);

        Layers layer = CheckLayers(target, prefab);
        NGUITools.SetLayer(clone, (int)layer);

        var trackingWidget = clone.AddComponent<ConstantSizeTrackingLabel>();   // AddComponent() runs Awake before returning
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(ConstantSizeTrackingSprite).Name, target.DisplayName);
        return trackingWidget;
    }

    private Layers CheckLayers(IWidgetTrackable target, GameObject trackingWidgetPrefab) {
        Layers targetLayer = (Layers)target.Transform.gameObject.layer;
        Layers prefabLayer = (Layers)trackingWidgetPrefab.layer;
        if (prefabLayer != Layers.UI && prefabLayer != Layers.TransparentFX && prefabLayer != targetLayer) {
            D.Warn("Target {0} of Layer {1} being assigned TrackingWidget of Layer {2}.", target.Transform.name, targetLayer.GetValueName(), prefabLayer.GetValueName());
        }
        return prefabLayer;
    }

    private void Cleanup() {
        OnDispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable
    [DoNotSerialize]
    private bool _alreadyDisposed = false;
    protected bool _isDisposing = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (_alreadyDisposed) {
            return;
        }

        _isDisposing = true;
        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        _alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

}

