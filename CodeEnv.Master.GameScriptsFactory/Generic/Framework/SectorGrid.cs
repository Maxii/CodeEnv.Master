﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorGrid.cs
// Grid of Sectors. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Grid of Sectors. 
/// </summary>
public class SectorGrid : AMonoBehaviourBaseSingleton<SectorGrid>, IDisposable {

    public float sectorVisibilityDepth = 3F;

    public Vector3 gridSize = new Vector3(5F, 5F, 5F);

    private static GFRectGrid _grid;
    private static Vector3 _gridVertexToBoxOffset = new Vector3(0.5F, 0.5F, 0.5F);

    private IList<Vector3> _gridVertexLocations;
    private IList<Vector3> _worldVertexLocations;
    private IDictionary<Vector3, Index3D> _gridBoxToSectorIndexLookup;
    private IDictionary<Index3D, Vector3> _sectorIndexToGridBoxLookup;
    private IList<Vector3> _worldBoxLocations;
    private static IDictionary<Index3D, Sector> _sectors;

    private GridWireframe _gridWireframe;
    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        _grid = InitializeGrid();
        ConstructSectors();
        Subscribe();
    }

    private GFRectGrid InitializeGrid() {
        GFRectGrid grid = UnityUtility.ValidateMonoBehaviourPresence<GFRectGrid>(gameObject);
        grid.spacing = TempGameValues.SectorSize;
        grid.size = gridSize;
        grid.relativeSize = true;
        grid.renderGrid = false;
        return grid;
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(PlayerViews.Instance.SubscribeToPropertyChanged<PlayerViews, PlayerViewMode>(pv => pv.ViewMode, OnPlayerViewModeChanged));
    }

    private void DynamicallySubscribe(bool toSubscribe) {
        if (toSubscribe) {
            _subscribers.Add(CameraControl.Instance.SubscribeToPropertyChanged<CameraControl, Index3D>(cc => cc.SectorIndex, OnCameraSectorIndexChanged));
        }
        else {
            IDisposable d = _subscribers.Single(s => s as DisposePropertyChangedSubscription<CameraControl> != null);
            _subscribers.Remove(d);
            d.Dispose();
        }
    }

    private void OnPlayerViewModeChanged() {
        PlayerViewMode viewMode = PlayerViews.Instance.ViewMode;
        switch (viewMode) {
            case PlayerViewMode.SectorView:
                //case PlayerViewMode.SectorOrder:
                ShowSectorGrid(true);
                DynamicallySubscribe(true);
                break;
            case PlayerViewMode.NormalView:
                DynamicallySubscribe(false);
                ShowSectorGrid(false);
                break;
            case PlayerViewMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(viewMode));
        }
    }

    private void OnCameraSectorIndexChanged() {
        // Note: not subscribed unless in SectorViewMode so no need to test for it
        if (_gridWireframe != null) {
            Vector3[] gridPoints;
            if (TryGenerateGridPoints(CameraControl.Instance.SectorIndex, out gridPoints)) {
                _gridWireframe.Points = gridPoints;
            }
        }
        else {
            // we are in the right mode, but the wireframe is still null, so continue to try to generate and show it
            ShowSectorGrid(true);
        }
    }

    private bool TryGenerateGridPoints(Index3D cameraSectorIndex, out Vector3[] gridPoints) {
        // per GridFramework: grid needs to be at origin for rendering to align properly with the grid ANY TIME vectrosity points are generated
        Vector3 tempPosition = _transform.position;
        _transform.position = Vector3.zero;
        Vector3 gridLocOfCamera = SectorGrid.GetGridVertexLocation(cameraSectorIndex);

        float xCameraGridLoc = gridLocOfCamera.x;
        float yCameraGridLoc = gridLocOfCamera.y;
        float zCameraGridLoc = gridLocOfCamera.z;

        // construct from and to values, keeping them within the size of the intended grid
        float xRenderFrom = Mathf.Clamp(xCameraGridLoc - sectorVisibilityDepth, -gridSize.x, gridSize.x);
        float yRenderFrom = Mathf.Clamp(yCameraGridLoc - sectorVisibilityDepth, -gridSize.y, gridSize.y);
        float zRenderFrom = Mathf.Clamp(zCameraGridLoc - sectorVisibilityDepth, -gridSize.z, gridSize.z);

        float xRenderTo = Mathf.Clamp(xCameraGridLoc + sectorVisibilityDepth, -gridSize.x, gridSize.x);
        float yRenderTo = Mathf.Clamp(yCameraGridLoc + sectorVisibilityDepth, -gridSize.y, gridSize.y);
        float zRenderTo = Mathf.Clamp(zCameraGridLoc + sectorVisibilityDepth, -gridSize.z, gridSize.z);

        // if any pair of axis values are equal, then only the first plane of vertexes will be showing, so expand it to one box showing
        ConfirmOneBoxDeep(ref xRenderFrom, ref xRenderTo);
        ConfirmOneBoxDeep(ref yRenderFrom, ref yRenderTo);
        ConfirmOneBoxDeep(ref zRenderFrom, ref zRenderTo);

        Vector3 renderFrom = new Vector3(xRenderFrom, yRenderFrom, zRenderFrom);
        Vector3 renderTo = new Vector3(xRenderTo, yRenderTo, zRenderTo);
        D.Log("CameraGridLoc {2}, RenderFrom {0}, RenderTo {1}.", renderFrom, renderTo, gridLocOfCamera);

        gridPoints = _grid.GetVectrosityPoints(renderFrom, renderTo);
        _transform.position = tempPosition;
        bool hasPoints = !gridPoints.IsNullOrEmpty<Vector3>();
        if (!hasPoints) {
            D.Warn("No grid points to render.");
        }
        return hasPoints;
    }

    /// <summary>
    /// Confirms (and modifies if necessary) that there will be a minimum depth of one sector
    /// visible in the grid to render no matter how far away the camera is.
    /// </summary>
    /// <param name="from">The from value.</param>
    /// <param name="to">The to value.</param>
    private void ConfirmOneBoxDeep(ref float from, ref float to) {
        if (from != to) { return; }
        if (to > Constants.ZeroF) {
            from = to - 1F;
        }
        else if (from < Constants.ZeroF) {
            to = from + 1F;
        }
    }

    private void ConstructSectors() {
        _gridVertexLocations = new List<Vector3>();
        _worldVertexLocations = new List<Vector3>();
        _gridBoxToSectorIndexLookup = new Dictionary<Vector3, Index3D>();
        _sectorIndexToGridBoxLookup = new Dictionary<Index3D, Vector3>();
        _worldBoxLocations = new List<Vector3>();
        _sectors = new Dictionary<Index3D, Sector>();

        float xSize = _grid.size.x;
        float ySize = _grid.size.y;
        float zSize = _grid.size.z;
        for (float x = -xSize; x < xSize + 0.1F; x++) {
            for (float y = -ySize; y < ySize + 0.1F; y++) {
                for (float z = -zSize; z < zSize + 0.1F; z++) {
                    Vector3 gridVertexLocation = new Vector3(x, y, z);
                    _gridVertexLocations.Add(gridVertexLocation);
                    _worldVertexLocations.Add(_grid.GridToWorld(gridVertexLocation));
                    if (z.ApproxEquals(zSize) || y.ApproxEquals(ySize) || x.ApproxEquals(xSize)) {
                        // this is an outside vertex of the grid, so the box is outside the grid
                        continue;
                    }

                    Vector3 gridBoxLocation = gridVertexLocation + _gridVertexToBoxOffset;
                    Index3D index = CalculateSectorIndexFromGridLocation(gridBoxLocation);

                    _gridBoxToSectorIndexLookup.Add(gridBoxLocation, index);
                    _sectorIndexToGridBoxLookup.Add(index, gridBoxLocation);

                    Vector3 worldBoxLocation = _grid.GridToWorld(gridBoxLocation);
                    _worldBoxLocations.Add(worldBoxLocation);

                    __AddSector(index, worldBoxLocation);
                }
            }
        }
        D.Log("{0} grid and {1} world vertice found.", _gridVertexLocations.Count, _worldVertexLocations.Count);
        D.Log("{0} grid and {1} world boxes found.", _gridBoxToSectorIndexLookup.Count, _worldBoxLocations.Count);
    }

    /// <summary>
    /// Calculates the sector index from grid location. The grid space location can either be
    /// the location of a vertex or of a box.
    /// </summary>
    /// <param name="gridLoc">The grid loc.</param>
    /// <returns></returns>
    private Index3D CalculateSectorIndexFromGridLocation(Vector3 gridLoc) {
        // Sector indexes will contain no 0 value. The sector index to the left of the origin is -1, to the right +1
        float x = gridLoc.x, y = gridLoc.y, z = gridLoc.z;
        int xIndex = x.ApproxEquals(Constants.ZeroF) ? 1 : (x > Constants.ZeroF ? Mathf.CeilToInt(x + 1F) : Mathf.FloorToInt(x));
        int yIndex = y.ApproxEquals(Constants.ZeroF) ? 1 : (y > Constants.ZeroF ? Mathf.CeilToInt(y + 1F) : Mathf.FloorToInt(y));
        int zIndex = z.ApproxEquals(Constants.ZeroF) ? 1 : (z > Constants.ZeroF ? Mathf.CeilToInt(z + 1F) : Mathf.FloorToInt(z));
        return new Index3D(xIndex, yIndex, zIndex);
    }

    private void __AddSector(Index3D index, Vector3 worldPosition) {
        Sector sectorPrefab = RequiredPrefabs.Instance.SectorPrefab;
        GameObject sectorGO = NGUITools.AddChild(Sectors.Folder.gameObject, sectorPrefab.gameObject);
        Sector sector = sectorGO.GetSafeMonoBehaviourComponent<Sector>();
        sector.SectorIndex = index;
        sectorGO.transform.position = worldPosition;
        _sectors.Add(index, sector);
        //D.Log("Sector added at index {0}.", index);
    }

    public static Vector3 GetGridBoxLocation(Index3D index) {
        Vector3 gridBoxLocation = Vector3.zero;
        if (!Instance._sectorIndexToGridBoxLookup.TryGetValue(index, out gridBoxLocation)) {
            gridBoxLocation = Instance.CalculateGridBoxLocationFromSectorIndex(index);
            Instance._sectorIndexToGridBoxLookup.Add(index, gridBoxLocation);
            Instance._gridBoxToSectorIndexLookup.Add(gridBoxLocation, index);
        }
        return gridBoxLocation;
    }

    private Vector3 CalculateGridBoxLocationFromSectorIndex(Index3D index) {
        int xIndex = index.X, yIndex = index.Y, zIndex = index.Z;
        D.Assert(xIndex != 0 && yIndex != 0 && zIndex != 0, "Illegal Index {0}.".Inject(index));
        float x = xIndex > Constants.Zero ? xIndex - 1F : xIndex;
        float y = yIndex > Constants.Zero ? yIndex - 1F : yIndex;
        float z = zIndex > Constants.Zero ? zIndex - 1F : zIndex;
        return new Vector3(x, y, z);
    }

    public static Vector3 GetGridVertexLocation(Index3D index) {
        return GetGridBoxLocation(index) - _gridVertexToBoxOffset;
    }

    public static Index3D GetSectorIndex(Vector3 worldPoint) {
        Index3D index;
        Vector3 gridClosestBoxLocation = _grid.NearestBoxG(worldPoint);
        if (!Instance._gridBoxToSectorIndexLookup.TryGetValue(gridClosestBoxLocation, out index)) {
            //D.Log("No Index at Grid Box Location {0}.", gridClosestBoxLocation);
            index = Instance.CalculateSectorIndexFromGridLocation(gridClosestBoxLocation);
            Instance._gridBoxToSectorIndexLookup.Add(gridClosestBoxLocation, index);
            Instance._sectorIndexToGridBoxLookup.Add(index, gridClosestBoxLocation);
        }
        return index;
    }

    public static bool TryGetSector(Index3D index, out Sector sector) {
        return _sectors.TryGetValue(index, out sector);
    }

    public static Sector GetSector(Index3D index) {
        Sector sector;
        if (!_sectors.TryGetValue(index, out sector)) {
            D.Warn("No Sector at {0}, returning null.", index);
        }
        return sector;
    }

    public static IList<Index3D> GetNeighbors(Index3D center) {
        IList<Index3D> neighbors = new List<Index3D>();
        int[] xValuePair = CalcNeighborPair(center.X);
        int[] yValuePair = CalcNeighborPair(center.Y);
        int[] zValuePair = CalcNeighborPair(center.Z);
        foreach (var x in xValuePair) {
            foreach (var y in yValuePair) {
                foreach (var z in zValuePair) {
                    Sector unused;
                    Index3D index = new Index3D(x, y, z);
                    if (TryGetSector(index, out unused)) {
                        neighbors.Add(index);
                    }
                }
            }
        }
        return neighbors;
    }

    private static int[] CalcNeighborPair(int center) {
        int[] valuePair = new int[2];
        // no 0 value in my sector indices
        valuePair[0] = center - 1 == 0 ? center - 2 : center - 1;
        valuePair[1] = center + 1 == 0 ? center + 2 : center + 1;
        return valuePair;
    }

    public static IList<Sector> GetSectorNeighbors(Index3D center) {
        IList<Sector> neighborSectors = new List<Sector>();
        foreach (var index in GetNeighbors(center)) {
            neighborSectors.Add(GetSector(index));
        }
        return neighborSectors;
    }

    public void ShowSectorGrid(bool toShow) {
        if (_gridWireframe == null) {
            Vector3[] gridPoints;
            if (TryGenerateGridPoints(CameraControl.Instance.SectorIndex, out gridPoints)) {
                _gridWireframe = new GridWireframe("GridWireframe", gridPoints, DynamicObjects.Folder);
            }
        }

        if (_gridWireframe != null) {
            if (toShow) {
                if (!_gridWireframe.IsShowing) {
                    StartCoroutine(_gridWireframe.Show());
                }
            }
            else if (_gridWireframe.IsShowing) {
                _gridWireframe.Hide();
            }
        }
    }

    private void Unsubscribe() {
        _subscribers.ForAll(s => s.Dispose());
        _subscribers.Clear();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        if (_gridWireframe != null) {
            _gridWireframe.Dispose();
        }
        Unsubscribe();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable
    [DoNotSerialize]
    private bool alreadyDisposed = false;

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
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
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
