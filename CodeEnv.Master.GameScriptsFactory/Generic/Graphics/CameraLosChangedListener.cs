// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraLosChangedListener.cs
// Event Hook class lets you easily add remote Camera LineOfSight Changed event listener functions to an object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Event Hook class lets you easily add remote Camera LineOfSight Changed listener functions to an object.
/// Example usage: CameraLosChangedListener.Get(remoteGameObject).onCameraLosChanged += MyLocalMethodToExecute; 
/// Derived from Ngui's UIEventListener class.
/// </summary>
public class CameraLosChangedListener : AMonoBase, ICameraLosChangedListener {

    /// <summary>
    /// Get or add an event listener to the specified game object.
    /// </summary>
    public static CameraLosChangedListener Get(GameObject go) {
        CameraLosChangedListener listener = go.GetComponent<CameraLosChangedListener>();
        if (listener == null) listener = go.AddComponent<CameraLosChangedListener>();
        return listener;
    }

    /// <summary>
    /// Occurs when Camera Line Of Sight state has changed on this GameObject. The flag
    /// indicates whether the object is now in or out of the camera's LOS.
    /// </summary>
    public event Action<GameObject, bool> onCameraLosChanged;

    protected override void Awake() {
        base.Awake();
        enabled = false;
    }

    protected override void Start() {
        base.Start();
        if (renderer != null) {
            renderer.enabled = true;    // renderers usually start enabled. This is to make sure as renderers do not deliver OnBecameVisible() events if not enabled

            if (!renderer.isVisible) {
                // all subscribers begin with InCameraLOS = true, aka they think they are in the camera's line of sight. When the renderer first wakes up,
                // it sends OnBecameInvisible if it is not visible. If this occurs before this listener is enabled, the listener's subscribers will not receive
                // the notification. This makes sure the notification is repeated when this listener becomes enabled. It is not needed when 
                // using an invisible mesh as the newly installed invisible mesh renderer will immediately notify subscribers of its inCameraLOS state.
                OnBecameInvisible();
            }
        }
        else {
            InitializeInvisibleMesh();
        }
    }

    void OnBecameVisible() {
        if (enabled) {
            //D.LogContext("{0}.{1} has received OnBecameVisible().".Inject(_transform.name, GetType().Name), this);
            if (onCameraLosChanged != null) {
                onCameraLosChanged(gameObject, true);
            }
            else {
                D.WarnContext("{0}.{1} has no subscriber to notify of OnBecameVisible().".Inject(_transform.name, GetType().Name), this);
            }
        }
        else {
            D.WarnContext("{0}.{1}.OnBecameVisible() called while not enabled.".Inject(_transform.name, GetType().Name), this);
        }
    }

    void OnBecameInvisible() {
        if (IsApplicationQuiting || Application.isLoadingLevel) { return; }  // OnBecameInvisible called if already visible when application is quiting
        if (enabled) {
            //D.LogContext("{0}.{1} has received OnBecameInvisible().".Inject(_transform.name, GetType().Name), this);
            if (onCameraLosChanged != null) {
                onCameraLosChanged(gameObject, false);
                return;
            }
            else {
                D.WarnContext("{0}.{1} has no subscriber to notify of OnBecameInvisible().".Inject(_transform.name, GetType().Name), this);
            }
        }
        else {
            D.WarnContext("{0}.{1}.OnBecameInvisible() called while not enabled.".Inject(_transform.name, GetType().Name), this);
        }
    }

    #region Invisible Mesh System supporting OnBecameVisible/Invisible

    private static IDictionary<string, Mesh> _meshCache = new Dictionary<string, Mesh>();

    /// <summary>
    /// Temporary workaround used to filter out all UIWidget.onChange events that aren't caused by a change in Widget Dimensions.
    /// </summary>
    private Vector2 __previousWidgetDimensions;

    private UIWidget _widget;
    private MeshFilter _meshFilter;
    /// <summary>
    /// Sets up an invisible bounds mesh that enables OnBecameVisible/Invisible to properly operate,
    /// even without having a pre-installed Renderer. Derived from Vectrosity.VectorManager.
    /// </summary>
    private void InitializeInvisibleMesh() {
        _meshFilter = gameObject.AddMissingComponent<MeshFilter>();

        var meshRenderer = gameObject.AddMissingComponent<MeshRenderer>();
        meshRenderer.castShadows = false;
        meshRenderer.receiveShadows = false;
        meshRenderer.enabled = true;    // renderers do not deliver OnBecameVisible() events if not enabled!!!!!!!!

        _widget = UnityUtility.ValidateMonoBehaviourPresence<UIWidget>(gameObject);
        _widget.onChange += CheckInvisibleMeshSize;
        CheckInvisibleMeshSize();
    }

    /// <summary>
    /// Checks to see if a different mesh is needed to match Widget.size and if so, provides it. 
    /// This is typically called when the Widget's dimensions change.
    /// </summary>
    private void CheckInvisibleMeshSize() {
        var widgetDimensions = _widget.localSize;
        if (__previousWidgetDimensions == widgetDimensions) {
            //D.Warn("{0} invisible mesh size {1} check without a widget dimension change.", GetType().Name, widgetDimensions);
            return;
        }
        __previousWidgetDimensions = widgetDimensions;

        string cacheKey = widgetDimensions.ToString();  // "[0.0, 1.0]"
        if (!_meshCache.ContainsKey(cacheKey)) {
            _meshCache.Add(cacheKey, MakeBoundsMesh(UnityUtility.GetBounds(_widget.localCorners)));
            _meshCache[cacheKey].name = cacheKey + " Invisible Bounds";
        }
        else {
            //D.Log("{0} is reusing {1} mesh.", _transform.name, _meshCache[cacheKey].name);
        }
        _meshFilter.mesh = _meshCache[cacheKey];
    }

    /// <summary>
    /// Makes an invisible (as there are no triangles) mesh between the vertices of the provided bounds.
    /// Derived from Vectrosity.VectorManager.
    /// </summary>
    /// <param name="bounds">The bounds.</param>
    /// <returns></returns>
    private static Mesh MakeBoundsMesh(Bounds bounds) {

        var mesh = new Mesh();
        mesh.vertices = new[] {bounds.center + new Vector3(-bounds.extents.x,  bounds.extents.y,  bounds.extents.z),
                               bounds.center + new Vector3( bounds.extents.x,  bounds.extents.y,  bounds.extents.z),
                               bounds.center + new Vector3(-bounds.extents.x,  bounds.extents.y, -bounds.extents.z),
                               bounds.center + new Vector3( bounds.extents.x,  bounds.extents.y, -bounds.extents.z),
                               bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y,  bounds.extents.z),
                               bounds.center + new Vector3( bounds.extents.x, -bounds.extents.y,  bounds.extents.z),
                               bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z),
                               bounds.center + new Vector3( bounds.extents.x, -bounds.extents.y, -bounds.extents.z)};
        return mesh;
    }

    #endregion

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

