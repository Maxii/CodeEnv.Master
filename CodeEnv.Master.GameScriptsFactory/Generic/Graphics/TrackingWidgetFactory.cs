﻿// --------------------------------------------------------------------------------------------------------------------
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
public class TrackingWidgetFactory : AGenericSingleton<TrackingWidgetFactory>, ITrackingWidgetFactory, IDisposable {
    // Note: no reason to dispose of _instance during scene transition as all its references persist across scenes

    private TrackingWidgetFactory() {
        Initialize();
    }

    protected sealed override void Initialize() { }

    /// <summary>
    /// Creates a label on the UI layer that tracks the <c>target</c>.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget MakeUITrackingLabel(IWidgetTrackable target, WidgetPlacement placement = WidgetPlacement.Over, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.uiTrackingLabel.gameObject;
        GameObject trackingWidgetGo = NGUITools.AddChild(DynamicWidgetsFolder.Instance.Folder.gameObject, trackingPrefabGo);

        __WarnIfUnexpectedLayers(target, trackingPrefabGo);
        Layers trackingPrefabLayer = (Layers)trackingPrefabGo.layer;
        NGUITools.SetLayer(trackingWidgetGo, (int)trackingPrefabLayer);

        var trackingWidget = trackingWidgetGo.GetSafeComponent<UITrackingLabel>();
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
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.uiTrackingSprite.gameObject;
        GameObject trackingWidgetGo = NGUITools.AddChild(DynamicWidgetsFolder.Instance.Folder.gameObject, trackingPrefabGo);

        __WarnIfUnexpectedLayers(target, trackingPrefabGo);
        Layers trackingPrefabLayer = (Layers)trackingPrefabGo.layer;
        NGUITools.SetLayer(trackingWidgetGo, (int)trackingPrefabLayer);

        var trackingWidget = trackingWidgetGo.GetSafeComponent<UITrackingSprite>();
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
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public IResponsiveTrackingSprite MakeResponsiveTrackingSprite(IWidgetTrackable target, IconInfo iconInfo, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingSprite;
        GameObject trackingWidgetGo = NGUITools.AddChild(target.transform.gameObject, trackingPrefabGo);

        var trackingSprite = trackingWidgetGo.AddComponent<ResponsiveTrackingSprite>();   // AddComponent() runs Awake before returning
        trackingSprite.Target = target;
        trackingSprite.IconInfo = iconInfo;
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
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingLabel;
        GameObject trackingWidgetGo = NGUITools.AddChild(target.transform.gameObject, trackingPrefabGo);

        __WarnIfUnexpectedLayers(target, trackingPrefabGo);
        Layers trackingPrefabLayer = (Layers)trackingPrefabGo.layer;
        NGUITools.SetLayer(trackingWidgetGo, (int)trackingPrefabLayer);

        var trackingWidget = trackingWidgetGo.AddComponent<VariableSizeTrackingLabel>();   // AddComponent() runs Awake before returning
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min);
        //D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(VariableSizeTrackingSprite).Name, target.DisplayName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite whose size scales with the size of the target, parented to and tracking the <c>target</c>.
    /// IMPROVE If there is an AtlasID, then there should also be a SpriteName and Color. Use IconInfo?
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="atlasID">The atlas identifier.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <returns></returns>
    public ITrackingWidget MakeVariableSizeTrackingSprite(IWidgetTrackable target, AtlasID atlasID, WidgetPlacement placement = WidgetPlacement.Above, float min = Constants.ZeroF) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingSprite;
        GameObject trackingWidgetGo = NGUITools.AddChild(target.transform.gameObject, trackingPrefabGo);

        __WarnIfUnexpectedLayers(target, trackingPrefabGo);
        Layers trackingPrefabLayer = (Layers)trackingPrefabGo.layer;
        NGUITools.SetLayer(trackingWidgetGo, (int)trackingPrefabLayer);

        var trackingWidget = trackingWidgetGo.AddComponent<VariableSizeTrackingSprite>();  // AddComponent() runs Awake before returning
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
    /// <param name="iconInfo">The icon information.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public ITrackingSprite MakeConstantSizeTrackingSprite(IWidgetTrackable target, IconInfo iconInfo, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingSprite;
        GameObject trackingWidgetGo = NGUITools.AddChild(target.transform.gameObject, trackingPrefabGo);

        var trackingSprite = trackingWidgetGo.AddComponent<ConstantSizeTrackingSprite>();  // AddComponent() runs Awake before returning
        trackingSprite.Target = target;
        trackingSprite.IconInfo = iconInfo;
        trackingSprite.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(ConstantSizeTrackingSprite).Name, target.DisplayName);
        return trackingSprite;
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
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingLabel;
        GameObject trackingWidgetGo = NGUITools.AddChild(target.transform.gameObject, trackingPrefabGo);

        __WarnIfUnexpectedLayers(target, trackingPrefabGo);
        Layers trackingPrefabLayer = (Layers)trackingPrefabGo.layer;
        NGUITools.SetLayer(trackingWidgetGo, (int)trackingPrefabLayer);

        var trackingWidget = trackingWidgetGo.AddComponent<ConstantSizeTrackingLabel>();   // AddComponent() runs Awake before returning
        trackingWidget.Target = target;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", GetType().Name, typeof(ConstantSizeTrackingSprite).Name, target.DisplayName);
        return trackingWidget;
    }

    private void __WarnIfUnexpectedLayers(IWidgetTrackable target, GameObject trackingWidgetPrefab) {
        Layers targetLayer = (Layers)target.transform.gameObject.layer;
        Layers prefabLayer = (Layers)trackingWidgetPrefab.layer;
        if (prefabLayer != Layers.UI && prefabLayer != Layers.TransparentFX && prefabLayer != targetLayer) {
            D.Warn("Target {0} of Layer {1} being assigned TrackingWidget of Layer {2}.", target.transform.name, targetLayer.GetValueName(), prefabLayer.GetValueName());
        }
    }

    private void Cleanup() {
        CallOnDispose();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable

    private bool _alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {

        Dispose(true);

        // This object is being cleaned up by you explicitly calling Dispose() so take this object off
        // the finalization queue and prevent finalization code from 'disposing' a second time
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isExplicitlyDisposing) {
        if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
            D.Warn("{0} has already been disposed.", GetType().Name);
            return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        }

        if (isExplicitlyDisposing) {
            // Dispose of managed resources here as you have called Dispose() explicitly
            Cleanup();
        }

        // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
        // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
        // called Dispose(false) to cleanup unmanaged resources

        _alreadyDisposed = true;
    }

    #endregion


}

