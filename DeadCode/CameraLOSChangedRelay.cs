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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
[Obsolete]
public class CameraLOSChangedRelay : AMonoBase, ICameraLOSChangedRelay {

    public List<Transform> relayTargets;

    private IList<ICameraLOSChangedClient> _iRelayTargets;

    protected override void Awake() {
        base.Awake();
        relayTargets = relayTargets ?? new List<Transform>();
        _iRelayTargets = new List<ICameraLOSChangedClient>();
        enabled = false;
    }


    private void InitializeRenderer() {
        if (renderer != null) {
            renderer.enabled = true;    // renderers do not deliver OnBecameVisible() events if not enabled!!!!!!!!
        }
        else {
            InitializeInvisibleMesh();
        }
    }

    protected override void OnEnable() {
        base.OnEnable();
        //D.Log("{0}.{1} has just been enabled.", _transform.name, GetType().Name);
    }

    protected override void Start() {
        base.Start();
        CleanRelayTargetsContent();
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

        foreach (var target in relayTargets) {
            D.Assert(target != null);
            ICameraLOSChangedClient iTarget = target.GetInterface<ICameraLOSChangedClient>();
            if (iTarget == null) {
                D.Warn("{0} is not an {1}.", target.name, typeof(ICameraLOSChangedClient));
                continue;
            }
            _iRelayTargets.Add(iTarget);
        }
        InitializeRenderer();
        InitializeAsVisible();
    }

    /// <summary>
    /// Checks the RelayTargets list for nulls and removes them if found.
    /// </summary>
    private void CleanRelayTargetsContent() {
        int count = relayTargets.Count;
        for (int i = 0; i < count; i++) {
            if (relayTargets[i] == null) {
                relayTargets.RemoveAt(i);
                count--;
            }
        }
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
        //D.Log("{0}.{1} attempting to add targets: {2}.", _transform.name, GetType().Name, targets.Concatenate<Transform>());
        foreach (var target in targets) {
            // validate target is a I...Client. and that it is not already present
            var iTarget = target.gameObject.GetSafeInterface<ICameraLOSChangedClient>();
            if (iTarget != null) {
                if (!relayTargets.Contains(target)) {
                    // add it to the lists
                    relayTargets.Add(target);
                    _iRelayTargets.Add(iTarget);
                }
                else {
                    //D.WarnContext("{0} Target {1} is already present.".Inject(GetType().Name, target.name), this);
                }
            }
            else {
                D.ErrorContext("Target {0} is not a {1}.".Inject(target.name, typeof(ICameraLOSChangedClient).Name), this);
            }
        }
        //if (relayTargets.Count > 1) {
        //    D.Log("{0} has multiple targets: {1}.", _transform.name, relayTargets.Select(rt => rt.name).Concatenate());
        //}
    }

    void OnBecameVisible() {
        D.Log("{0}.CameraLOSChangedRelay has received OnBecameVisible().", _transform.name);
        if (ValidateCameraLOSChange(inLOS: true)) {
            NotifyClientsOfChange(inLOS: true);
        }
    }

    void OnBecameInvisible() {
        D.Log("{0}.CameraLOSChangedRelay has received OnBecameInvisible().", _transform.name);
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

    private int __count;

    private UISprite _sprite;
    private MeshFilter _meshFilter;
    private Vector2 _previousSpriteDimensions;
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

        if (_meshCache == null) {
            _meshCache = new Dictionary<string, Mesh>();
        }

        _sprite = UnityUtility.ValidateMonoBehaviourPresence<UISprite>(gameObject);
        _sprite.onChange = CheckInvisibleMesh;
        CheckInvisibleMesh();
    }

    private void CheckInvisibleMesh() {
        __count++;
        if (__count > 1) {
            D.Log("{0}.CheckInvisibleMesh() called {1} times.", _transform.name, __count);
        }
        var spriteDimensions = _sprite.localSize;
        if (_previousSpriteDimensions == spriteDimensions) {
            // dimensions haven't changed
            return;
        }
        _previousSpriteDimensions = spriteDimensions;
        string cacheKey = spriteDimensions.ToString();  // "[0.0, 1.0]"
        if (!_meshCache.ContainsKey(cacheKey)) {
            _meshCache.Add(cacheKey, MakeBoundsMesh(UnityUtility.GetBounds(_sprite.localCorners)));
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

    //private UISprite _sprite;

    //private bool isVisible;
    //protected override void Update() {
    //    base.Update();
    //    if (_sprite != null) {
    //        if (isVisible == _sprite.isVisible) {
    //            return;
    //        }
    //        isVisible = _sprite.isVisible;
    //        string vis = isVisible ? "visible" : "not visible";
    //        D.Warn("Sprite {0} visibility changed to {1}.", _sprite.name, vis);

    //    }

    //}

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

