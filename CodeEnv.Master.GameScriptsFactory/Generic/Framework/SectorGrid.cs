// --------------------------------------------------------------------------------------------------------------------
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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
public class SectorGrid : AMonoSingleton<SectorGrid>, ISectorGrid {

    public static IList<SectorItem> AllSectors { get { return _instance._sectors.Values.ToList(); } }

    /// <summary>
    /// Readonly. The location of the center of all sectors in world space.
    /// </summary>
    public IList<Vector3> SectorCenters { get { return _worldBoxLocations; } }

    /// <summary>
    ///  Readonly. The location of the corners of all sectors in world space.
    /// </summary>
    public IList<Vector3> SectorCorners { get { return _worldVertexLocations; } }

    public float sectorVisibilityDepth = 2F;

    private GFRectGrid _grid;
    private Vector3 _gridVertexToBoxOffset = new Vector3(0.5F, 0.5F, 0.5F);

    private IList<Vector3> _gridVertexLocations;
    private IList<Vector3> _worldVertexLocations;
    private IDictionary<Vector3, Index3D> _gridBoxToSectorIndexLookup;
    private IDictionary<Index3D, Vector3> _sectorIndexToGridBoxLookup;
    private IList<Vector3> _worldBoxLocations;
    private IDictionary<Index3D, SectorItem> _sectors;

    private SectorFactory _sectorFactory;
    private GridWireframe _gridWireframe;
    private IList<IDisposable> _subscribers;

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.SectorGrid = Instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeGrid();
        ConstructSectors();
        Subscribe();
    }

    private void InitializeGrid() {
        _grid = UnityUtility.ValidateMonoBehaviourPresence<GFRectGrid>(gameObject);
        _grid.spacing = TempGameValues.SectorSize;
        _grid.relativeSize = true;
        _grid.renderGrid = false;
        int sectorCount = Mathf.RoundToInt(Mathf.Pow(2F, 3F) * _grid.size.x * _grid.size.y * _grid.size.z);
        if (sectorCount > 64) {
            D.Warn("Sector count is {0}. Currently, values over 64 can take a long time to scan.", sectorCount);
            // 2x2x2 < 1 sec, 3x3x3 < 5 secs, 5x5x5 > 90 secs
        }
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(PlayerViews.Instance.SubscribeToPropertyChanged<PlayerViews, PlayerViewMode>(pv => pv.ViewMode, OnPlayerViewModeChanged));
    }

    private void DynamicallySubscribe(bool toSubscribe) {
        if (toSubscribe) {
            _subscribers.Add(MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, Index3D>(cc => cc.SectorIndex, OnCameraSectorIndexChanged));
        }
        else {
            IDisposable d = _subscribers.Single(s => s as DisposePropertyChangedSubscription<MainCameraControl> != null);
            _subscribers.Remove(d);
            d.Dispose();
        }
    }

    private void OnPlayerViewModeChanged() {
        PlayerViewMode viewMode = PlayerViews.Instance.ViewMode;
        switch (viewMode) {
            case PlayerViewMode.SectorView:
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
            if (TryGenerateGridPoints(MainCameraControl.Instance.SectorIndex, out gridPoints)) {
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
        Vector3 gridLocOfCamera = GetGridVertexLocation(cameraSectorIndex);

        float xCameraGridLoc = gridLocOfCamera.x;
        float yCameraGridLoc = gridLocOfCamera.y;
        float zCameraGridLoc = gridLocOfCamera.z;

        // construct from and to values, keeping them within the size of the intended grid
        float xRenderFrom = Mathf.Clamp(xCameraGridLoc - sectorVisibilityDepth, -_grid.size.x, _grid.size.x);
        float yRenderFrom = Mathf.Clamp(yCameraGridLoc - sectorVisibilityDepth, -_grid.size.y, _grid.size.y);
        float zRenderFrom = Mathf.Clamp(zCameraGridLoc - sectorVisibilityDepth, -_grid.size.z, _grid.size.z);

        float xRenderTo = Mathf.Clamp(xCameraGridLoc + sectorVisibilityDepth, -_grid.size.x, _grid.size.x);
        float yRenderTo = Mathf.Clamp(yCameraGridLoc + sectorVisibilityDepth, -_grid.size.y, _grid.size.y);
        float zRenderTo = Mathf.Clamp(zCameraGridLoc + sectorVisibilityDepth, -_grid.size.z, _grid.size.z);

        // if any pair of axis values are equal, then only the first plane of vertexes will be showing, so expand it to one box showing
        ConfirmOneBoxDeep(ref xRenderFrom, ref xRenderTo);
        ConfirmOneBoxDeep(ref yRenderFrom, ref yRenderTo);
        ConfirmOneBoxDeep(ref zRenderFrom, ref zRenderTo);

        Vector3 renderFrom = new Vector3(xRenderFrom, yRenderFrom, zRenderFrom);
        Vector3 renderTo = new Vector3(xRenderTo, yRenderTo, zRenderTo);
        //D.Log("CameraGridLoc {2}, RenderFrom {0}, RenderTo {1}.", renderFrom, renderTo, gridLocOfCamera);

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
        _sectorFactory = SectorFactory.Instance;
        _gridVertexLocations = new List<Vector3>();
        _worldVertexLocations = new List<Vector3>();
        _gridBoxToSectorIndexLookup = new Dictionary<Vector3, Index3D>();
        _sectorIndexToGridBoxLookup = new Dictionary<Index3D, Vector3>();
        _worldBoxLocations = new List<Vector3>();
        _sectors = new Dictionary<Index3D, SectorItem>();

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
                        // this is an outside forward, up or right vertex of the grid, so the box would be outside the grid
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
        //D.Log("{0} grid and {1} world vertice found.", _gridVertexLocations.Count, _worldVertexLocations.Count);
        //D.Log("{0} grid and {1} world boxes found.", _gridBoxToSectorIndexLookup.Count, _worldBoxLocations.Count);
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
        var sector = _sectorFactory.MakeInstance(index, worldPosition);

        UnityUtility.WaitOneToExecute(onWaitFinished: (wasKilled) => {
            sector.PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive;
        });

        _sectors.Add(index, sector);
        //D.Log("Sector added at index {0}.", index);
    }

    /// <summary>
    /// Gets the half value (1.5, 2.5, 1.5) location (in the grid coordinate system)
    /// associated with this sector index. This will be the center of the sector box.
    /// </summary>
    /// <param name="index">The sector index.</param>
    /// <returns></returns>
    public Vector3 GetGridBoxLocation(Index3D index) {
        Vector3 gridBoxLocation;
        if (!Instance._sectorIndexToGridBoxLookup.TryGetValue(index, out gridBoxLocation)) {
            gridBoxLocation = Instance.CalculateGridBoxLocationFromSectorIndex(index);
            Instance._sectorIndexToGridBoxLookup.Add(index, gridBoxLocation);
            Instance._gridBoxToSectorIndexLookup.Add(gridBoxLocation, index);
        }
        return gridBoxLocation;
    }

    private Vector3 CalculateGridBoxLocationFromSectorIndex(Index3D index) {
        int xIndex = index.x, yIndex = index.y, zIndex = index.z;
        D.Assert(xIndex != 0 && yIndex != 0 && zIndex != 0, "Illegal Index {0}.".Inject(index));
        float x = xIndex > Constants.Zero ? xIndex - 1F : xIndex;
        float y = yIndex > Constants.Zero ? yIndex - 1F : yIndex;
        float z = zIndex > Constants.Zero ? zIndex - 1F : zIndex;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Generates and returns 8 world space vertices of a box surrounding the center of a sector
    /// where each vertex is <c>distance</c> from the center. The box defined by the vertices
    /// is essentially a box inscribed inside a sphere of radius <c>distance</c> centered on the sector.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="distance">The distance from any vertex to the sector's center in units.</param>
    /// <returns></returns>
    public IList<Vector3> GenerateVerticesOfBoxAroundCenter(Index3D index, float distance) {
        Arguments.ValidateNotNegative(distance);
        var sectorGridBoxLoc = GetGridBoxLocation(index);
        Vector3 sectorCenterWorldLoc = _grid.GridToWorld(sectorGridBoxLoc);
        return UnityUtility.CalcVerticesOfInscribedBoxInsideSphere(sectorCenterWorldLoc, distance);
    }

    /// <summary>
    /// Gets the round number (1.0, 1.0, 2.0) location (in the grid coordinate system)
    /// associated with this sector index. This will be the left, lower, back corner of the 
    /// sector box.
    /// </summary>
    /// <param name="index">The sector index.</param>
    /// <returns></returns>
    public Vector3 GetGridVertexLocation(Index3D index) {
        return GetGridBoxLocation(index) - _gridVertexToBoxOffset;
    }

    public Index3D GetSectorIndex(Vector3 worldPoint) {
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

    public bool TryGetSector(Index3D index, out SectorItem sector) {
        return _sectors.TryGetValue(index, out sector);
    }

    /// <summary>
    /// Gets the sector associated with this index. 
    /// Warning: Can be null while debugging as only a limited number of sectors are 'built'
    /// to reduce the time needed to construct valid paths for pathfinding.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns></returns>
    public SectorItem GetSector(Index3D index) {
        SectorItem sector;
        if (!TryGetSector(index, out sector)) {
            D.Warn("No SectorModel at {0}, returning null.", index);
        }
        return sector;
    }

    /// <summary>
    /// Gets the SpaceTopography value associated with this location in worldspace.
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <returns></returns>
    public Topography GetSpaceTopography(Vector3 worldLocation) {
        Index3D sectorIndex = GetSectorIndex(worldLocation);
        SystemItem system;
        if (SystemCreator.TryGetSystem(sectorIndex, out system)) {
            // the sector containing worldLocation has a system
            if (Vector3.SqrMagnitude(worldLocation - system.Position) < system.Radius * system.Radius) {
                return Topography.System;
            }
        }
        // TODO add Nebula and DeepNebula
        return Topography.OpenSpace;
    }


    /// <summary>
    /// Gets the indexes of the neighbors to this sector index. A sector must
    /// occupy the index location to be included. The sector at center is
    /// not included.
    /// </summary>
    /// <param name="center">The center.</param>
    /// <returns></returns>
    public IList<Index3D> GetNeighbors(Index3D center) {
        IList<Index3D> neighbors = new List<Index3D>();
        int[] xValuePair = CalcNeighborPair(center.x);
        int[] yValuePair = CalcNeighborPair(center.y);
        int[] zValuePair = CalcNeighborPair(center.z);
        foreach (var x in xValuePair) {
            foreach (var y in yValuePair) {
                foreach (var z in zValuePair) {
                    SectorItem unused;
                    Index3D index = new Index3D(x, y, z);
                    if (TryGetSector(index, out unused)) {
                        neighbors.Add(index);
                    }
                }
            }
        }
        return neighbors;
    }

    private int[] CalcNeighborPair(int center) {
        int[] valuePair = new int[2];
        // no 0 value in my sector indices
        valuePair[0] = center - 1 == 0 ? center - 2 : center - 1;
        valuePair[1] = center + 1 == 0 ? center + 2 : center + 1;
        return valuePair;
    }

    /// <summary>
    /// Gets the neighboring sectors to this sector index. A sector must
    /// occupy the index location to be included. The sector at center is
    /// not included.
    /// </summary>
    /// <param name="center">The center.</param>
    /// <returns></returns>
    public IList<SectorItem> GetSectorNeighbors(Index3D center) {
        IList<SectorItem> neighborSectors = new List<SectorItem>();
        foreach (var index in GetNeighbors(center)) {
            neighborSectors.Add(GetSector(index));
        }
        return neighborSectors;
    }

    /// <summary>
    /// Gets the distance in sectors between the center of the sectors located at these two indexes.
    /// Example: the distance between (1, 1, 1) and (1, 1, 2) is 1.0. The distance between 
    /// (1, 1, 1) and (1, 2, 2) is 1.414, and the distance between (-1, 1, 1) and (1, 1, 1) is 1.0
    /// as indexes have no 0 value.
    /// </summary>
    /// <param name="first">The first.</param>
    /// <param name="second">The second.</param>
    /// <returns></returns>
    public float GetDistanceInSectors(Index3D first, Index3D second) {
        Vector3 firstGridBoxLoc = GetGridBoxLocation(first);
        Vector3 secondGridBoxLoc = GetGridBoxLocation(second);
        return Vector3.Distance(firstGridBoxLoc, secondGridBoxLoc);
    }

    public void ShowSectorGrid(bool toShow) {
        if (_gridWireframe == null) {
            Vector3[] gridPoints;
            if (TryGenerateGridPoints(MainCameraControl.Instance.SectorIndex, out gridPoints)) {
                _gridWireframe = new GridWireframe("GridWireframe", gridPoints);
            }
        }

        if (_gridWireframe != null) {
            _gridWireframe.Show(toShow);
        }
    }

    protected override void Cleanup() {
        References.SectorGrid = null;
        if (_gridWireframe != null) {
            _gridWireframe.Dispose();
        }
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll(s => s.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

