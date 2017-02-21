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
using UnityEngine.Profiling;

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
    /// Creates a label on the UI layer that tracks the <c>target</c>. It does not interact with the mouse.
    /// </summary>
    /// <param name="trackedTgt">The tracked target.</param>
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
        //D.Log("{0} made a {1} for {2}.", DebugName, typeof(UITrackingLabel).Name, trackedTgt.DebugName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite on the UI layer that tracks the <c>target</c>. It does not interact with the mouse.
    /// </summary>
    /// <param name="trackedTgt">The tracked target.</param>
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
        //D.Log("{0} made a {1} for {2}.", DebugName, typeof(UITrackingSprite).Name, trackedTgt.DebugName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite whose size doesn't change on the screen. It does interact with the mouse.
    /// <remarks>The approach taken to UIPanel usage will be determined by the DebugControls setting.</remarks>
    /// </summary>
    /// <param name="trackedTgt">The target this sprite will track.</param>
    /// <param name="iconInfo">The info needed to build the sprite.</param>
    /// <returns></returns>
    public IInteractiveWorldTrackingSprite MakeInteractiveWorldTrackingSprite(IWidgetTrackable trackedTgt, IconInfo iconInfo) {
        if (DebugControls.Instance.UseOneUIPanelPerWidget) {
            return MakeInteractiveWorldTrackingSprite_Independent(trackedTgt, iconInfo);
        }
        else {
            return MakeInteractiveWorldTrackingSprite_Common(trackedTgt, iconInfo);
        }
    }

    /// <summary>
    /// Creates a sprite whose size doesn't change on the screen. It does interact with the mouse.
    /// <remarks>This version has its own UIPanel which is parented to the tracked target.</remarks>
    /// </summary>
    /// <param name="trackedTgt">The target this sprite will track.</param>
    /// <param name="iconInfo">The info needed to build the sprite.</param>
    /// <returns></returns>
    public IInteractiveWorldTrackingSprite_Independent MakeInteractiveWorldTrackingSprite_Independent(IWidgetTrackable trackedTgt, IconInfo iconInfo) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingSprite_Independent;
        GameObject trackingWidgetGo = NGUITools.AddChild(trackedTgt.transform.gameObject, trackingPrefabGo);

        Profiler.BeginSample("Proper AddComponent allocation", (trackedTgt as Component).gameObject);
        var trackingSprite = trackingWidgetGo.AddComponent<InteractiveWorldTrackingSprite_Independent>();   // AddComponent() runs Awake before returning
        Profiler.EndSample();

        trackingSprite.Target = trackedTgt;
        trackingSprite.IconInfo = iconInfo;
        ////trackingSprite.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", DebugName, typeof(InteractiveWorldTrackingSprite_Independent).Name, trackedTgt.DebugName);
        return trackingSprite;
    }

    /// <summary>
    /// Creates a sprite whose size doesn't change on the screen. It does interact with the mouse.
    /// <remarks>This version is parented to a common UIPanel for the tracked target's type.</remarks>
    /// </summary>
    /// <param name="trackedTgt">The target this sprite will track.</param>
    /// <param name="iconInfo">The info needed to build the sprite.</param>
    /// <returns></returns>
    public IInteractiveWorldTrackingSprite MakeInteractiveWorldTrackingSprite_Common(IWidgetTrackable trackedTgt, IconInfo iconInfo) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingSprite;
        GameObject trackingWidgetGo = NGUITools.AddChild(null, trackingPrefabGo);
        AttachWidgetAsChildOfParentFolder(trackingWidgetGo, trackedTgt);

        Profiler.BeginSample("Proper AddComponent allocation", (trackedTgt as Component).gameObject);
        var trackingSprite = trackingWidgetGo.AddComponent<InteractiveWorldTrackingSprite>();   // AddComponent() runs Awake before returning
        Profiler.EndSample();

        trackingSprite.Target = trackedTgt;
        trackingSprite.IconInfo = iconInfo;
        ////trackingSprite.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", DebugName, typeof(InteractiveWorldTrackingSprite).Name, trackedTgt.DebugName);
        return trackingSprite;
    }

    /// <summary>
    /// Creates a sprite whose size doesn't change on the screen. It does not interact with the mouse.
    /// <remarks>The approach taken to UIPanel usage will be determined by the DebugControls setting.</remarks>
    /// </summary>
    /// <param name="trackedTgt">The target this sprite will track.</param>
    /// <param name="iconInfo">The info needed to build the sprite.</param>
    /// <returns></returns>
    public IWorldTrackingSprite MakeWorldTrackingSprite(IWidgetTrackable trackedTgt, IconInfo iconInfo) {
        if (DebugControls.Instance.UseOneUIPanelPerWidget) {
            return MakeWorldTrackingSprite_Independent(trackedTgt, iconInfo);
        }
        else {
            return MakeWorldTrackingSprite_Common(trackedTgt, iconInfo);
        }
    }

    /// <summary>
    /// Creates a sprite whose size doesn't change on the screen. It does not interact with the mouse.
    /// <remarks>This version has its own UIPanel which is parented to the tracked target.</remarks>
    /// </summary>
    /// <param name="trackedTgt">The target.</param>
    /// <param name="iconInfo">The icon information.</param>
    /// <returns></returns>
    public IWorldTrackingSprite_Independent MakeWorldTrackingSprite_Independent(IWidgetTrackable trackedTgt, IconInfo iconInfo) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingSprite_Independent;
        GameObject trackingWidgetGo = NGUITools.AddChild(trackedTgt.transform.gameObject, trackingPrefabGo);

        Profiler.BeginSample("Proper AddComponent allocation", (trackedTgt as Component).gameObject);
        var trackingSprite = trackingWidgetGo.AddComponent<WorldTrackingSprite_Independent>();  // AddComponent() runs Awake before returning
        Profiler.EndSample();

        trackingSprite.Target = trackedTgt;
        trackingSprite.IconInfo = iconInfo;
        ////trackingSprite.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", DebugName, typeof(WorldTrackingSprite_Independent).Name, trackedTgt.DebugName);
        return trackingSprite;
    }

    /// <summary>
    /// Creates a sprite whose size doesn't change on the screen. It does not interact with the mouse.
    /// <remarks>This version is parented to a common UIPanel for the tracked target's type.</remarks>
    /// </summary>
    /// <param name="trackedTgt">The tracked target.</param>
    /// <param name="iconInfo">The icon information.</param>
    /// <returns></returns>
    public IWorldTrackingSprite MakeWorldTrackingSprite_Common(IWidgetTrackable trackedTgt, IconInfo iconInfo) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingSprite;
        GameObject trackingWidgetGo = NGUITools.AddChild(null, trackingPrefabGo);
        AttachWidgetAsChildOfParentFolder(trackingWidgetGo, trackedTgt);

        Profiler.BeginSample("Proper AddComponent allocation", (trackedTgt as Component).gameObject);
        var trackingSprite = trackingWidgetGo.AddComponent<WorldTrackingSprite>();  // AddComponent() runs Awake before returning
        Profiler.EndSample();

        trackingSprite.Target = trackedTgt;
        trackingSprite.IconInfo = iconInfo;
        ////trackingSprite.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", DebugName, typeof(WorldTrackingSprite).Name, trackedTgt.DebugName);
        return trackingSprite;
    }

    /// <summary>
    /// Creates a sprite whose size doesn't change on the screen. It does not interact with the mouse.
    /// <remarks>The approach taken to UIPanel usage will be determined by the DebugControls setting.</remarks>
    /// </summary>
    /// <param name="trackedTgt">The target this sprite will track.</param>
    /// <param name="iconInfo">The info needed to build the sprite.</param>
    /// <returns></returns>
    public ITrackingWidget MakeWorldTrackingLabel(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Above) {
        if (DebugControls.Instance.UseOneUIPanelPerWidget) {
            return MakeWorldTrackingLabel_Independent(trackedTgt, placement);
        }
        else {
            return MakeWorldTrackingLabel_Common(trackedTgt, placement);
        }
    }

    /// <summary>
    /// Creates a label whose size doesn't change on the screen. It does not interact with the mouse.
    /// <remarks>This version has its own UIPanel which is parented to the tracked target.</remarks>
    /// </summary>
    /// <param name="trackedTgt">The target.</param>
    /// <param name="placement">The placement.</param>
    /// <returns></returns>
    public ITrackingWidget MakeWorldTrackingLabel_Independent(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Above) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingLabel_Independent;
        GameObject trackingWidgetGo = NGUITools.AddChild(trackedTgt.transform.gameObject, trackingPrefabGo);

        __WarnIfUnexpectedLayers(trackedTgt, trackingPrefabGo);
        Layers trackingPrefabLayer = (Layers)trackingPrefabGo.layer;
        NGUITools.SetLayer(trackingWidgetGo, (int)trackingPrefabLayer);

        Profiler.BeginSample("Proper AddComponent allocation", (trackedTgt as Component).gameObject);
        var trackingWidget = trackingWidgetGo.AddComponent<WorldTrackingLabel_Independent>();   // AddComponent() runs Awake before returning
        Profiler.EndSample();

        trackingWidget.Target = trackedTgt;
        trackingWidget.Placement = placement;
        ////trackingWidget.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", DebugName, typeof(WorldTrackingLabel_Independent).Name, trackedTgt.DebugName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a label whose size doesn't change on the screen. It does not interact with the mouse.
    /// <remarks>This version is parented to a common UIPanel for the tracked target's type.</remarks>
    /// </summary>
    /// <param name="trackedTgt">The tracked target.</param>
    /// <param name="placement">The placement.</param>
    /// <returns></returns>
    public ITrackingWidget MakeWorldTrackingLabel_Common(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Above) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingLabel;
        GameObject trackingWidgetGo = NGUITools.AddChild(null, trackingPrefabGo);
        AttachWidgetAsChildOfParentFolder(trackingWidgetGo, trackedTgt);

        Profiler.BeginSample("Proper AddComponent allocation", (trackedTgt as Component).gameObject);
        var trackingWidget = trackingWidgetGo.AddComponent<WorldTrackingLabel>();   // AddComponent() runs Awake before returning
        Profiler.EndSample();

        trackingWidget.Target = trackedTgt;
        trackingWidget.Placement = placement;
        ////trackingWidget.SetShowDistance(min, max);
        //D.Log("{0} made a {1} for {2}.", DebugName, typeof(WorldTrackingLabel).Name, trackedTgt.DebugName);
        return trackingWidget;
    }

    /// <summary>
    /// Makes and returns an invisible CameraLosChangedListener, parented to and tracking the trackedTgt.
    /// </summary>
    /// <param name="trackedTgt">The tracked target.</param>
    /// <param name="listenerLayer">The listener layer.</param>
    /// <returns></returns>
    public ICameraLosChangedListener MakeInvisibleCameraLosChangedListener(IWidgetTrackable trackedTgt, Layers listenerLayer) {
        GameObject listenerGo = new GameObject(InvisibleListenerName);

        Profiler.BeginSample("Proper AddComponent allocation", trackedTgt.transform.gameObject);
        ICameraLosChangedListener listener = listenerGo.AddComponent<CameraLosChangedListener>();
        Profiler.EndSample();

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

    /// <summary>
    /// Creates a label whose size changes on the screen. It does not interact with the mouse.
    /// <remarks>This version has its own UIPanel which is parented to the tracked target.</remarks>
    /// </summary>
    /// <param name="trackedTgt">The tracked target.</param>
    /// <param name="placement">The placement.</param>
    /// <returns></returns>
    public ITrackingWidget MakeWorldTrackingLabel_IndependentVariableSize(IWidgetTrackable trackedTgt, WidgetPlacement placement = WidgetPlacement.Above) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingLabel_Independent;
        GameObject trackingWidgetGo = NGUITools.AddChild(trackedTgt.transform.gameObject, trackingPrefabGo);

        __WarnIfUnexpectedLayers(trackedTgt, trackingPrefabGo);
        Layers trackingPrefabLayer = (Layers)trackingPrefabGo.layer;
        NGUITools.SetLayer(trackingWidgetGo, (int)trackingPrefabLayer);

        Profiler.BeginSample("Proper AddComponent allocation", (trackedTgt as Component).gameObject);
        var trackingWidget = trackingWidgetGo.AddComponent<WorldTrackingLabel_IndependentVariableSize>();   // AddComponent() runs Awake before returning
        Profiler.EndSample();

        trackingWidget.Target = trackedTgt;
        trackingWidget.Placement = placement;
        ////trackingSprite.SetShowDistance(min);
        //D.Log("{0} made a {1} for {2}.", DebugName, typeof(WorldTrackingLabel_IndependentVariableSize).Name, trackedTgt.DebugName);
        return trackingWidget;
    }

    /// <summary>
    /// Creates a sprite whose size changes on the screen. It does not interact with the mouse.
    /// <remarks>This version has its own UIPanel which is parented to the tracked target.</remarks>
    /// IMPROVE Use of IconInfo deferred until I have a specific usage case.
    /// </summary>
    /// <param name="trackedTgt">The tracked target.</param>
    /// <param name="atlasID">The atlas identifier.</param>
    /// <param name="placement">The placement.</param>
    /// <returns></returns>
    public ITrackingWidget MakeWorldTrackingSprite_IndependentVariableSize(IWidgetTrackable trackedTgt, AtlasID atlasID, WidgetPlacement placement = WidgetPlacement.Above) {
        GameObject trackingPrefabGo = RequiredPrefabs.Instance.worldTrackingSprite_Independent;
        GameObject trackingWidgetGo = NGUITools.AddChild(trackedTgt.transform.gameObject, trackingPrefabGo);

        __WarnIfUnexpectedLayers(trackedTgt, trackingPrefabGo);
        Layers trackingPrefabLayer = (Layers)trackingPrefabGo.layer;
        NGUITools.SetLayer(trackingWidgetGo, (int)trackingPrefabLayer);

        Profiler.BeginSample("Proper AddComponent allocation", (trackedTgt as Component).gameObject);
        var trackingWidget = trackingWidgetGo.AddComponent<WorldTrackingSprite_IndependentVariableSize>();  // AddComponent() runs Awake before returning
        Profiler.EndSample();

        trackingWidget.AtlasID = atlasID;
        trackingWidget.Target = trackedTgt;
        trackingWidget.Placement = placement;
        ////trackingSprite.SetShowDistance(min);
        //D.Log("{0} made a {1} for {2}.", DebugName, typeof(WorldTrackingSprite_IndependentVariableSize).Name, trackedTgt.DebugName);
        return trackingWidget;
    }


    /// <summary>
    /// Attaches the provided widget to the proper parent folder as determined from trackedTgt.
    /// </summary>
    /// <param name="widgetGo">The widget GameObject.</param>
    /// <param name="trackedTgt">The tracked target.</param>
    private void AttachWidgetAsChildOfParentFolder(GameObject widgetGo, IWidgetTrackable trackedTgt) {
        GameObject parentFolder = null;
        if (trackedTgt is AUnitElementItem) {
            parentFolder = ElementIconsFolder.Instance.gameObject;
        }
        else if (trackedTgt is PlanetItem) {
            parentFolder = PlanetIconsFolder.Instance.gameObject;
        }
        else if (trackedTgt is StarItem) {
            parentFolder = StarIconsFolder.Instance.gameObject;
        }
        else {
            D.Assert(trackedTgt is AUnitCmdItem);
            parentFolder = CmdIconsFolder.Instance.gameObject;
        }
        UnityUtility.AttachChildToParent(widgetGo, parentFolder);
    }

    private void __WarnIfUnexpectedLayers(IWidgetTrackable trackedTgt, GameObject trackingWidgetPrefab) {
        Layers targetLayer = (Layers)trackedTgt.transform.gameObject.layer;
        Layers prefabLayer = (Layers)trackingWidgetPrefab.layer;
        if (prefabLayer != Layers.UI && prefabLayer != Layers.TransparentFX && prefabLayer != targetLayer) {
            D.Warn("{0}: Target {1} of Layer {2} being assigned TrackingWidget of Layer {3}.", DebugName, trackedTgt.DebugName, targetLayer.GetValueName(), prefabLayer.GetValueName());
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

