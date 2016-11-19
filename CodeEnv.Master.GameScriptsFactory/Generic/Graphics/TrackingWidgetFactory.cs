// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TrackingWidgetFactory.cs
// Singleton Factory that creates pre-configured ITrackingWidgets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton Factory that creates pre-configured ITrackingWidgets.
/// </summary>
public class TrackingWidgetFactory : AGenericSingleton<TrackingWidgetFactory>, ITrackingWidgetFactory, IDisposable {
    // Note: no reason to dispose of _instance during scene transition as all its references persist across scenes

    private static readonly string InvisibleListenerName = "Invisible{0}".Inject(typeof(CameraLosChangedListener).Name);

    private TrackingWidgetFactory() {
        Initialize();
    }

    protected sealed override void Initialize() { }

    /// <summary>
    /// Creates a label on the UI layer that tracks the <c>target</c>.
    /// </summary>
    /// <param name="trackedTgt">The target.</param>
    /// <param name="placement">The placement.</param>
    /// <param name="min">The minimum show distance from the camera.</param>
    /// <param name="max">The maximum show distance from the camera.</param>
    /// <returns></returns>
    public ITrackingWidget MakeUITrackingLabel(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Over, float min = Constants.ZeroF, float max = Mathf.Infinity) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.uiTrackingLabel.gameObject;
        GameObject trackingWidgetGo = NGUITools.AddChild(DynamicWidgetsFolder.Instance.Folder.gameObject, trackingPrefabGo);

        __WarnIfUnexpectedLayers(trackedTgt, trackingPrefabGo);
        Layers trackingPrefabLayer = (Layers)trackingPrefabGo.layer;
        NGUITools.SetLayer(trackingWidgetGo, (int)trackingPrefabLayer);

        var trackingWidget = trackingWidgetGo.GetComponent<UITrackingLabel>();
        trackingWidget.Target = trackedTgt;
        trackingWidget.Placement = placement;
        trackingWidget.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", Name, typeof(UITrackingLabel).Name, target.DisplayName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite on the UI layer that tracks the <c>target</c>.
    /// IMPROVE Use IconInfo?
    /// </summary>
    /// <param name="trackedTgt">The target.</param>
    /// <param name="atlasID">The atlas identifier.</param>
    /// <param name="placement">The placement.</param>
    /// <returns></returns>
    public ITrackingWidget MakeUITrackingSprite(IWidgetTrackable trackedTgt, AtlasID atlasID, WidgetPlacement placement = WidgetPlacement.Above) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.uiTrackingSprite.gameObject;
        GameObject trackingWidgetGo = NGUITools.AddChild(DynamicWidgetsFolder.Instance.Folder.gameObject, trackingPrefabGo);

        __WarnIfUnexpectedLayers(trackedTgt, trackingPrefabGo);
        Layers trackingPrefabLayer = (Layers)trackingPrefabGo.layer;
        NGUITools.SetLayer(trackingWidgetGo, (int)trackingPrefabLayer);

        var trackingWidget = trackingWidgetGo.GetComponent<UITrackingSprite>();
        trackingWidget.AtlasID = atlasID;
        trackingWidget.Target = trackedTgt;
        trackingWidget.Placement = placement;
        ////trackingSprite.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", Name, typeof(UITrackingSprite).Name, target.DisplayName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a tracking sprite which can respond to the mouse.
    /// The sprite's size stays constant, parented to and tracks the <c>target</c>.
    /// </summary>
    /// <param name="trackedTgt">The target this sprite will track.</param>
    /// <param name="iconInfo">The info needed to build the sprite.</param>
    /// <param name="min">The minimum show distance.</param>
    /// <param name="max">The maximum show distance.</param>
    /// <returns></returns>
    public IResponsiveTrackingSprite MakeResponsiveTrackingSprite(IWidgetTrackable trackedTgt, IconInfo iconInfo) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingSprite;
        GameObject trackingWidgetGo = NGUITools.AddChild(trackedTgt.transform.gameObject, trackingPrefabGo);

        var trackingSprite = trackingWidgetGo.AddComponent<ResponsiveTrackingSprite>();   // AddComponent() runs Awake before returning
        trackingSprite.Target = trackedTgt;
        trackingSprite.IconInfo = iconInfo;
        ////trackingSprite.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", Name, typeof(ResponsiveTrackingSprite).Name, target.DisplayName);
        return trackingSprite;
    }

    /// <summary>
    /// Creates a label whose size scales with the size of the target, parented to and tracking the <c>target</c>.
    /// </summary>
    /// <param name="trackedTgt">The target.</param>
    /// <param name="placement">The placement.</param>
    /// <returns></returns>
    public ITrackingWidget MakeVariableSizeTrackingLabel(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Above) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingLabel;
        GameObject trackingWidgetGo = NGUITools.AddChild(trackedTgt.transform.gameObject, trackingPrefabGo);

        __WarnIfUnexpectedLayers(trackedTgt, trackingPrefabGo);
        Layers trackingPrefabLayer = (Layers)trackingPrefabGo.layer;
        NGUITools.SetLayer(trackingWidgetGo, (int)trackingPrefabLayer);

        var trackingWidget = trackingWidgetGo.AddComponent<VariableSizeTrackingLabel>();   // AddComponent() runs Awake before returning
        trackingWidget.Target = trackedTgt;
        trackingWidget.Placement = placement;
        ////trackingSprite.SetShowDistance(min);
        //D.Log("{0} made a {1} for {2}.", Name, typeof(VariableSizeTrackingSprite).Name, target.DisplayName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite whose size scales with the size of the target, parented to and tracking the <c>target</c>.
    /// IMPROVE Use of IconInfo deferred until I have a specific usage case.
    /// </summary>
    /// <param name="trackedTgt">The target.</param>
    /// <param name="atlasID">The atlas identifier.</param>
    /// <param name="placement">The placement.</param>
    /// <returns></returns>
    public ITrackingWidget MakeVariableSizeTrackingSprite(IWidgetTrackable trackedTgt, AtlasID atlasID, WidgetPlacement placement = WidgetPlacement.Above) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingSprite;
        GameObject trackingWidgetGo = NGUITools.AddChild(trackedTgt.transform.gameObject, trackingPrefabGo);

        __WarnIfUnexpectedLayers(trackedTgt, trackingPrefabGo);
        Layers trackingPrefabLayer = (Layers)trackingPrefabGo.layer;
        NGUITools.SetLayer(trackingWidgetGo, (int)trackingPrefabLayer);

        var trackingWidget = trackingWidgetGo.AddComponent<VariableSizeTrackingSprite>();  // AddComponent() runs Awake before returning
        trackingWidget.AtlasID = atlasID;
        trackingWidget.Target = trackedTgt;
        trackingWidget.Placement = placement;
        ////trackingSprite.SetShowDistance(min);
        //D.Log("{0} made a {1} for {2}.", Name, typeof(VariableSizeTrackingSprite).Name, target.DisplayName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite whose size stays constant, independent of the size of the target, parented to and tracking the <c>target</c>.
    /// </summary>
    /// <param name="trackedTgt">The target.</param>
    /// <param name="iconInfo">The icon information.</param>
    /// <returns></returns>
    public ITrackingSprite MakeConstantSizeTrackingSprite(IWidgetTrackable trackedTgt, IconInfo iconInfo) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingSprite;
        GameObject trackingWidgetGo = NGUITools.AddChild(trackedTgt.transform.gameObject, trackingPrefabGo);

        var trackingSprite = trackingWidgetGo.AddComponent<ConstantSizeTrackingSprite>();  // AddComponent() runs Awake before returning
        trackingSprite.Target = trackedTgt;
        trackingSprite.IconInfo = iconInfo;
        ////trackingSprite.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", Name, typeof(ConstantSizeTrackingSprite).Name, target.DisplayName);
        return trackingSprite;
    }

    /// <summary>
    /// Creates a label whose size stays constant, independent of the size of the target, parented to and tracking the <c>target</c>.
    /// </summary>
    /// <param name="trackedTgt">The target.</param>
    /// <param name="placement">The placement.</param>
    /// <returns></returns>
    public ITrackingWidget MakeConstantSizeTrackingLabel(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Above) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingLabel;
        GameObject trackingWidgetGo = NGUITools.AddChild(trackedTgt.transform.gameObject, trackingPrefabGo);

        __WarnIfUnexpectedLayers(trackedTgt, trackingPrefabGo);
        Layers trackingPrefabLayer = (Layers)trackingPrefabGo.layer;
        NGUITools.SetLayer(trackingWidgetGo, (int)trackingPrefabLayer);

        var trackingWidget = trackingWidgetGo.AddComponent<ConstantSizeTrackingLabel>();   // AddComponent() runs Awake before returning
        trackingWidget.Target = trackedTgt;
        trackingWidget.Placement = placement;
        ////trackingWidget.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", Name, typeof(ConstantSizeTrackingSprite).Name, target.DisplayName);
        return trackingWidget;
    }

    /// <summary>
    /// Makes and returns an invisible CameraLosChangedListener, parented to and tracking the trackedTgt.
    /// </summary>
    /// <param name="trackedTgt">The tracked TGT.</param>
    /// <param name="listenerLayer">The listener layer.</param>
    /// <returns></returns>
    public ICameraLosChangedListener MakeInvisibleCameraLosChangedListener(IWidgetTrackable trackedTgt, Layers listenerLayer) {
        GameObject listenerGo = new GameObject(InvisibleListenerName);
        ICameraLosChangedListener listener = listenerGo.AddComponent<CameraLosChangedListener>();
        UnityUtility.AttachChildToParent(listenerGo, trackedTgt.transform.gameObject);
        listenerGo.layer = (int)listenerLayer;
        return listener;
    }

    /// <summary>
    /// Makes an IWidgetTrackable location useful for hosting an invisible CameraLosChangedListener.
    /// </summary>
    /// <param name="parent">The parent.</param>
    /// <returns></returns>
    public IWidgetTrackable MakeTrackableLocation(GameObject parent) {
        GameObject trackableLocGo = new GameObject(typeof(WidgetTrackableLocation).Name);
        WidgetTrackableLocation wtLoc = trackableLocGo.AddComponent<WidgetTrackableLocation>();
        UnityUtility.AttachChildToParent(trackableLocGo, parent);
        return wtLoc;
    }

    private void __WarnIfUnexpectedLayers(IWidgetTrackable trackedTgt, GameObject trackingWidgetPrefab) {
        Layers targetLayer = (Layers)trackedTgt.transform.gameObject.layer;
        Layers prefabLayer = (Layers)trackingWidgetPrefab.layer;
        if (prefabLayer != Layers.UI && prefabLayer != Layers.TransparentFX && prefabLayer != targetLayer) {
            D.Warn("{0}: Target {1} of Layer {2} being assigned TrackingWidget of Layer {3}.", Name, trackedTgt.transform.name, targetLayer.GetValueName(), prefabLayer.GetValueName());
        }
    }

    private void Cleanup() { }

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
            CallOnDispose();
        }

        // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
        // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
        // called Dispose(false) to cleanup unmanaged resources

        _alreadyDisposed = true;
    }

    #endregion


}

