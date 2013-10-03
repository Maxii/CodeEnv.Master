// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Sector.cs
// Operates the context menu and graphics for a sector.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Operates the context menu and graphics for a sector.
/// </summary>
public class Sector : AMonoBehaviourBase, IHasContextMenu {

    public string Name { get; set; }    // UNDONE

    private static Vector3[] _neighborDirections;

    private SelectionManager _selectionMgr;
    private CubeWireframe _sectorWireframe;
    private GameColor _normalViewModeWireframeColor;
    private BoxCollider _collider;

    //private Sector[] _neighbors;

    private Mesh _mesh;
    private Vector3 _size;

    protected override void Awake() {
        base.Awake();
        _collider = UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
        _selectionMgr = SelectionManager.Instance;
        _mesh = gameObject.GetComponentInChildren<MeshFilter>().mesh;
        _size = _mesh.bounds.size;
        _collider.size = _size;
        _collider.enabled = true;
        //EnableSector(true);
        // NOTE: Collider usage: I'm keeping the colliders on all the time for now and using the camera's culling mask and UICamera.EventReceiverMask to ignore them
        InitializeNeighborDirections();
    }


    private void InitializeNeighborDirections() {
        if (_neighborDirections == null) {
            HashSet<Vector3> vectors = new HashSet<Vector3>();
            Vector3[] coreVectors = { Vector3.forward, Vector3.forward + Vector3.right, Vector3.forward + Vector3.left, Vector3.forward + Vector3.up, 
                                        Vector3.forward + Vector3.down, Vector3.forward + Vector3.right + Vector3.up, Vector3.forward + Vector3.right + Vector3.down,
                                        Vector3.forward + Vector3.left + Vector3.up, Vector3.forward + Vector3.left + Vector3.down,

                                        Vector3.right, Vector3.right + Vector3.up, Vector3.right + Vector3.down, 
                                    
                                        Vector3.up};

            foreach (var core in coreVectors) {
                Vector3 normalized = core.normalized;
                vectors.Add(normalized);
                vectors.Add(-normalized);
            }

            _neighborDirections = vectors.ToArray();
            //D.Log("Vector Count = {0}.", _neighborDirections.Length);   // 8 corners, 6 faces, 12 diags = 26
            //foreach (var v in _neighborDirections) {
            //    D.Log("Vector = {0}.", v);   
            //}
        }
    }

    protected override void Start() {
        base.Start();
        __ValidateCtxObjectSettings();
    }

    public void ShowSector(bool toShow) {
        if (!toShow && _sectorWireframe == null) {
            return;
        }
        if (_sectorWireframe == null) {
            _sectorWireframe = new CubeWireframe("SectorWireframe", _transform, _size);
            _sectorWireframe.Parent = DynamicObjects.Folder;
            _normalViewModeWireframeColor = _sectorWireframe.Color;
        }
        _sectorWireframe.Show(toShow);
    }

    private bool _ignoreNextOnHoverFalseEvent;
    void OnHover(bool isOver) {
        // FIXME: This approach to fixing the disappearing sector image when the context menu is showing will work
        // once I know for sure whether the menu actually displayed or not
        if (_ignoreNextOnHoverFalseEvent && !isOver) {
            _ignoreNextOnHoverFalseEvent = false;
            return;
        }
        HighlightWireframe(isOver);
        D.Log("OnHover({0}) received by {1}. Renderer.enabled now {2}.", isOver, gameObject.name, isOver);
    }

    private void HighlightWireframe(bool toHighlight) {
        GameColor wireframeColor = toHighlight ? UnityDebugConstants.SectorHighlightColor : _normalViewModeWireframeColor;
        _sectorWireframe.Color = wireframeColor;
    }

    /// <summary>
    /// Disables this sector and its neighbors. This primarily involves the collider.
    /// </summary>
    //public void DisableNeighbors() {
    //    if (_neighbors == null) {
    //        _neighbors = AcquireNeighbors();
    //    }
    //    D.Assert(ContainsCamera(), "Camera not within {0} boundaries.".Inject(typeof(Sector).Name), this);
    //    EnableSector(false);  // if not already
    //    _neighbors.ForAll(n => n.EnableSector(false));
    //}

    //private Sector[] AcquireNeighbors() {
    //    HashSet<Sector> neighbors = new HashSet<Sector>();
    //    float castDistance = _size.x;   // should reach into each neighbor, but not beyond
    //    foreach (var direction in _neighborDirections) {
    //        RaycastHit hit;
    //        Ray ray = new Ray(_transform.position, direction);
    //        if (_collider.Raycast(ray, out hit, castDistance)) {
    //            // we have encountered a neighbor, so record it
    //            Sector neighbor = hit.collider.gameObject.GetSafeMonoBehaviourComponent<Sector>();
    //            D.Assert(neighbor != null, "{0} collider enountered is not a {1}.".Inject(hit.collider.name, typeof(Sector).Name), this);
    //            neighbors.Add(neighbor);
    //        }
    //    }
    //    return neighbors.ToArray();
    //}

    //public void EnableSector(bool toEnable) {
    //    _collider.enabled = toEnable;   // does collider off mean an OnHover(false) will follow?
    //    if (!toEnable) {
    //        StartCoroutine(EnableWhenCameraGone);
    //    }
    //}

    //private IEnumerator EnableWhenCameraGone() {
    //    while (PlayerViews.Instance.ViewMode == PlayerViewMode.SectorView && NeighborsContainCamera()) {
    //        yield return null;
    //    }
    //    EnableSector(true);
    //}

    //private bool NeighborsContainCamera() {
    //    return ContainsCamera() || _neighbors.Any(n => n.ContainsCamera());
    //}

    //public bool ContainsCamera() {
    //    return _mesh.bounds.Contains(Camera.main.transform.position);
    //}

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IHasContextMenu Members

    public void __ValidateCtxObjectSettings() {
        CtxObject ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        D.Assert(ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

    public void OnPress(bool isDown) {
        FleetManager selectedFleetMgr = _selectionMgr.CurrentSelection as FleetManager;
        //string fleetName = fleetMgr != null ? fleetMgr.name : "null";
        //D.Log("Sector.OnPress({0}), fleetMgr is {1}.", isDown, fleetName);
        if (selectedFleetMgr != null) {
            CameraControl.Instance.ShowContextMenuOnPress(isDown);
            _ignoreNextOnHoverFalseEvent = !isDown;
            string toIgnore = _ignoreNextOnHoverFalseEvent ? "ignored." : "not ignored.";
            D.Log("NextOnHover(false) event is {0}.", toIgnore);
        }
    }

    #endregion

}

