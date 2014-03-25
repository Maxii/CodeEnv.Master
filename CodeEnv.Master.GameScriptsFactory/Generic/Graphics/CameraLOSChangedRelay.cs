// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CameraLOSChangedRelay.cs
// Conveys changes in its Renderer's 'visibility state' (in or out of the camera's line of sight) to 
// one or more client transforms that implement the ICameraLOSChangedClient interface.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using System;
using System.Linq;
using System.Text;

/// <summary>
/// Conveys changes in its Renderer's 'visibility state' (in or out of the camera's line of sight) to 
/// one or more client gameobjects that implement the ICameraLOSChangedClient interface.
///<remarks>Used when I wish to separate a mesh and its renderer from a parent GameObject that does most of the work.</remarks>
/// </summary>
public class CameraLOSChangedRelay : AMonoBase, ICameraLOSChangedRelay, IDisposable {

    public List<Transform> relayTargets;

    private IList<ICameraLOSChangedClient> _iRelayTargets;

    protected override void Awake() {
        base.Awake();
        InitializeRenderer();
        relayTargets = relayTargets ?? new List<Transform>();
        if (relayTargets.Count == 0) {
            Transform relayTarget = _transform.GetSafeTransformWithInterfaceInParents<ICameraLOSChangedClient>();
            if (relayTarget != null) {
                D.Warn("{0} {1} target field is not assigned. Automatically assigning {2} as target.", _transform.name, this.GetType().Name, relayTarget.name);
            }
            else {
                D.Warn("No {0} assigned or found for {1}.", typeof(ICameraLOSChangedClient), _transform.name);
                return;
            }
            relayTargets.Add(relayTarget);
        }

        _iRelayTargets = new List<ICameraLOSChangedClient>();

        foreach (var target in relayTargets) {
            ICameraLOSChangedClient iTarget = target.GetInterface<ICameraLOSChangedClient>();
            if (iTarget == null) {
                D.Warn("{0} is not an {1}.", target.name, typeof(ICameraLOSChangedClient));
                continue;
            }
            _iRelayTargets.Add(iTarget);
        }

        if (!GameStatus.Instance.IsRunning) {
            GameStatus.Instance.onIsRunning_OneShot += OnGameIsRunning;
            enabled = false;
        }
    }

    private void InitializeRenderer() {
        if (renderer != null) {
            renderer.enabled = true;    // renderers do not deliver OnBecameVisible() events if not enabled!!!!!!!!
        }
        else {
            SetupInvisibleBoundsMesh();
        }
    }

    protected override void Start() {
        base.Start();
        InitializeAsVisible();
    }

    /// <summary>
    /// Initializes all ICameraLOSChangedClients with a collection of senders that say they are visible, whether they
    /// are or not. Even though all Clients start with InCameraLOS = true once enabled, this collection of senders
    /// makes sure that the client won't change to InCameraLOS = false until all these senders tell the client they
    /// have become invisible. Without this at startup, a single sender can become invisible, thereby changing
    /// the client to InCameraLOS = false since the count of visible senders would otherwise start at zero.
    /// </summary>
    private void InitializeAsVisible() {
        NotifyClientsOfChange(inLOS: true);
    }

    public void AddTarget(params Transform[] targets) {
        foreach (var target in targets) {
            // validate target is a INotify... and that it is not already present
            var iTarget = target.gameObject.GetSafeInterface<ICameraLOSChangedClient>();
            if (iTarget != null && !relayTargets.Contains(target)) {
                // add it to the lists
                relayTargets.Add(target);
                _iRelayTargets.Add(iTarget);
            }
        }
    }

    private void OnGameIsRunning() {
        enabled = true;
    }

    void OnBecameVisible() {
        //D.Log("{0} CameraLOSChangedRelay has received OnBecameVisible(). IsRunning = {1}, IsInitialization = {2}.", _transform.name, _isRunning, __isInitialization);
        if (ValidateCameraLOSChange(inLOS: true)) {
            NotifyClientsOfChange(inLOS: true);
        }
    }

    void OnBecameInvisible() {
        //D.Log("{0} CameraLOSChangedRelay has received OnBecameInvisible(). IsRunning = {1}.", _transform.name, _isRunning);
        if (ValidateCameraLOSChange(inLOS: false)) {
            NotifyClientsOfChange(inLOS: false);
        }
    }

    // FIXME Recieving a few duplicate OnBecameXXX events during initial scrolling and don't know why
    // It does not seem to be from other cameras in the scene. Don't know about editor scene camera.
    private bool ValidateCameraLOSChange(bool inLOS) {
        if (!enabled) {
            return false;   // see SetupDocs.txt for approach to visibility
        }
        bool isValid = true;
        //string visibility = inLOS ? "Visible" : "Invisible";
        //if (gameObject.activeInHierarchy) {
        //    if (inLOS != renderer.InLineOfSightOf(Camera.main)) {                         // FIXME this test does not reliably work
        //        StringBuilder sb = new StringBuilder("CameraLOSState: ");
        //        foreach (Camera c in Camera.allCameras) {
        //            sb.AppendFormat("{0}.inLOS = {1}, ", c.name, renderer.InLineOfSightOf(c));
        //        }
        //        D.WarnContext("{0}.OnBecame{1}() error. {2}.".Inject(gameObject.name, visibility, sb.ToString()), this);
        //        isValid = false;
        //    }
        //}
        return isValid;
    }

    private void NotifyClientsOfChange(bool inLOS) {
        for (int i = 0; i < relayTargets.Count; i++) {
            Transform t = relayTargets[i];
            if (t && t.gameObject.activeInHierarchy) {  // avoids NullReferenceException during Inspector shutdown
                ICameraLOSChangedClient client = _iRelayTargets[i];
                if (client != null) {
                    LogCameraLOSChange(_transform.name, relayTargets[i].name, inLOS);
                    client.NotifyCameraLOSChanged(_transform, inLOS);
                }
            }
        }
        // more efficient and easier but can't provide the client target name for debug
        //foreach (var iNotify in _iRelayTargets) {    
        //    if (iNotify != null) {
        //        iNotify.NotifyVisibilityChanged(_transform, isVisible: true);
        //        D.Log("{0} has notified a client of becoming Visible.", _transform.name);
        //    }
        //}
    }

    [System.Diagnostics.Conditional("DEBUG_LOG")]
    private void LogCameraLOSChange(string notifier, string client, bool inLOS) {
        if (DebugSettings.Instance.EnableVerboseDebugLog) {
            string iNotifyParentName = _transform.GetSafeTransformWithInterfaceInParents<ICameraLOSChangedClient>().name;
            string visibility = inLOS ? "InCameraLOS" : "OutOfCameraLOS";
            D.Log("{0} of parent {1} is notifying client {2} of becoming {3}.", notifier, iNotifyParentName, client, visibility);
        }
    }

    #region Invisible Mesh System supporting OnBecameVisible/Invisible

    private static IDictionary<string, Mesh> _meshCache;

    /// <summary>
    /// Sets up an invisible bounds mesh that enables OnBecameVisible/Invisible to properly operate,
    /// even without having a pre-installed Renderer. Derived from Vectrosity.VectorManager.
    /// </summary>
    private void SetupInvisibleBoundsMesh() {
        var meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null) {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.enabled = true;    // renderers do not deliver OnBecameVisible() events if not enabled!!!!!!!!

        if (_meshCache == null) {
            _meshCache = new Dictionary<string, Mesh>();
        }

        UISprite sprite = gameObject.GetSafeMonoBehaviourComponent<UISprite>();
        string cacheKey = sprite.localSize.ToString();  // "[0.0, 1.0]"
        if (!_meshCache.ContainsKey(cacheKey)) {
            _meshCache.Add(cacheKey, MakeBoundsMesh(UnityUtility.GetBounds(sprite.localCorners)));
            _meshCache[cacheKey].name = cacheKey + " Invisible Bounds";
        }
        else {
            //D.Log("{0} is reusing {1} mesh.", _transform.name, _meshCache[cacheKey].name);
        }
        meshFilter.mesh = _meshCache[cacheKey];
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

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    private void Unsubscribe() {
        // even though the OneShot will unsubscribe this object once Raised, this object can be destroyed
        // prior to the game starting (when an extra planet is destroyed by SystemCreator) so we need
        // to unsubscribe in case destruction occurs before the game starts running
        GameStatus.Instance.onIsRunning_OneShot -= OnGameIsRunning;
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

        _isDisposing = isDisposing;
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

