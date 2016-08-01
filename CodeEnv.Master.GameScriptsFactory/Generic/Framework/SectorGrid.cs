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
using GridFramework.Extensions.Nearest;
using GridFramework.Extensions.Vectrosity;
using GridFramework.Grids;
using GridFramework.Renderers.Rectangular;
using UnityEngine;

/// <summary>
/// Grid of Sectors. Sector indexes contain no 0 value. The sector index to the left of the origin is -1, to the right +1.
/// </summary>
public class SectorGrid : AMonoSingleton<SectorGrid>, ISectorGrid {   // has Custom Editor

    private const string SectorNameFormat = "Sector {0}";

    public float sectorVisibilityDepth = 2F;

    public bool enableGridSizeLimit = true;

    public Vector3 debugMaxGridSize = new Vector3(10, 10, 10);

    public IEnumerable<Sector> AllSectors { get { return _sectorIndexToSectorLookup.Values; } }

    /// <summary>
    /// Read-only. The location of the center of all sectors in world space.
    /// </summary>
    public IEnumerable<Vector3> SectorCenters { get { return _sectorIndexToWorldBoxLocationLookup.Values; } }

    /// <summary>
    ///  Read-only. The location of the corners of all sectors in world space.
    /// </summary>
    private IList<Vector3> SectorCorners { get { return _worldVertexLocations; } }

    private bool IsGridWireframeShowing { get { return _gridWireframe != null && _gridWireframe.IsShowing; } }

    private RectGrid _grid;
    private Parallelepiped _gridRenderer;
    private Vector3 _gridVertexToBoxOffset = new Vector3(0.5F, 0.5F, 0.5F);

    private IList<Vector3> _gridVertexLocations;
    private IList<Vector3> _worldVertexLocations;
    private IDictionary<Vector3, Index3D> _gridBoxToSectorIndexLookup;
    private IDictionary<Index3D, Vector3> _sectorIndexToGridBoxLookup;
    private IDictionary<Index3D, Vector3> _sectorIndexToWorldBoxLocationLookup;
    private IDictionary<Index3D, Sector> _sectorIndexToSectorLookup;

    /// <summary>
    /// The size of the universe in grid cells where the outer cells
    /// along a grid axis are fully contained inside the universe.
    /// </summary>
    private Vector3 _universeGridSize;
    private GridWireframe _gridWireframe;
    private IList<IDisposable> _subscriptions;
    private GameManager _gameMgr;

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.SectorGrid = Instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeLocalReferencesAndValues();
        InitializeGrid();
        Subscribe();
    }

    private void InitializeLocalReferencesAndValues() {
        _gameMgr = GameManager.Instance;
    }

    private void InitializeGrid() {
        _grid = UnityUtility.ValidateComponentPresence<RectGrid>(gameObject);
        _grid.Spacing = TempGameValues.SectorSize;
        // In version 2 RectGrid size is always relative //_grid.relativeSize = true;
        _gridRenderer = UnityUtility.ValidateComponentPresence<Parallelepiped>(gameObject);
        _gridRenderer.enabled = false;        //_grid.renderGrid = false;
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(PlayerViews.Instance.SubscribeToPropertyChanged<PlayerViews, PlayerViewMode>(pv => pv.ViewMode, PlayerViewModePropChangedHandler));
        _gameMgr.gameStateChanged += GameStateChangedEventHandler;
    }

    protected override void Start() {
        base.Start();
        BlockGameStateProgressionBeyondBuilding();
    }

    private void BlockGameStateProgressionBeyondBuilding() {
        _gameMgr.RecordGameStateProgressionReadiness(Instance, GameState.Building, isReady: false);
    }

    private void DynamicallySubscribe(bool toSubscribe) {
        if (toSubscribe) {
            _subscriptions.Add(MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, Index3D>(cc => cc.SectorIndex, CameraSectorIndexPropChangedHandler));
        }
        else {
            IDisposable d = _subscriptions.Single(s => s as DisposePropertyChangedSubscription<MainCameraControl> != null);
            _subscriptions.Remove(d);
            d.Dispose();
        }
    }

    private void HandleGameStateBuildingBegun() {
        InitializeGridSize();
        ConstructSectors();
        //__ValidateWorldSectorCorners();   // can take multiple seconds
        _gameMgr.RecordGameStateProgressionReadiness(Instance, GameState.Building, isReady: true);
    }

    private void InitializeGridSize() {
        int cellCountInsideUniverseAlongAGridAxis = CalcCellCountInsideUniverseAlongAGridAxis();
        D.Assert(cellCountInsideUniverseAlongAGridAxis % 2 == Constants.Zero, "{0}: CellCount {1} can't be odd.", GetType().Name, cellCountInsideUniverseAlongAGridAxis);
        _universeGridSize = Vector3.one * cellCountInsideUniverseAlongAGridAxis;
        if (enableGridSizeLimit) {
            D.Assert(debugMaxGridSize.x.ApproxEquals(debugMaxGridSize.y) && debugMaxGridSize.y.ApproxEquals(debugMaxGridSize.z), "{0}: {1} must be cube.", GetType().Name, debugMaxGridSize);
            D.Assert((debugMaxGridSize.x % 2F).ApproxEquals(Constants.ZeroF), "{0}: {1} must use even values.", GetType().Name, debugMaxGridSize);
            if (_universeGridSize.sqrMagnitude > debugMaxGridSize.sqrMagnitude) {
                // only use DebugMaxGridSize if the Universe sized grid is bigger
                _universeGridSize = debugMaxGridSize;
            }
        }
        _gridRenderer.From = -_universeGridSize / 2F;
        _gridRenderer.To = -_gridRenderer.From;
        D.Log("{0}: Universe Grid Size = {1}.", GetType().Name, _universeGridSize);
    }

    private int CalcCellCountInsideUniverseAlongAGridAxis() {
        float universeRadius = _gameMgr.GameSettings.UniverseSize.Radius();
        float cellCountAlongGridPosAxisInsideUniverse = universeRadius / TempGameValues.SectorSideLength;
        return Mathf.FloorToInt(cellCountAlongGridPosAxisInsideUniverse) * 2;
    }

    #region Event and Property Change Handlers

    private void GameStateChangedEventHandler(object sender, EventArgs e) {
        var gameState = _gameMgr.CurrentState;
        if (gameState == GameState.Building) {
            HandleGameStateBuildingBegun();
        }
        if (gameState == GameState.RunningCountdown_1) {
            AllSectors.ForAll(s => s.CommenceOperations());
        }
    }

    private void PlayerViewModePropChangedHandler() {
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

    private void CameraSectorIndexPropChangedHandler() {
        D.Assert(PlayerViews.Instance.ViewMode == PlayerViewMode.SectorView);   // not subscribed unless in SectorViewMode
        D.Assert(IsGridWireframeShowing);
        //D.Log("{0}: CameraSectorIndex has changed. Generating new grid points.", GetType().Name);
        _gridWireframe.Points = GenerateWireframeGridPoints(MainCameraControl.Instance.SectorIndex);
    }

    #endregion

    private List<Vector3> GenerateWireframeGridPoints(Index3D cameraSectorIndex) {
        // per GridFramework: grid needs to be at origin for rendering to align properly with the grid ANY TIME vectrosity points are generated
        D.Assert(Mathfx.Approx(transform.position, Vector3.zero, .01F), "{0} must be located at origin.", GetType().Name);
        Vector3 gridLocOfCamera = GetGridVertexLocation(cameraSectorIndex);

        float xCameraGridLoc = gridLocOfCamera.x;
        float yCameraGridLoc = gridLocOfCamera.y;
        float zCameraGridLoc = gridLocOfCamera.z;

        // construct from and to values, keeping them within the size of the intended grid
        float xRenderFrom = Mathf.Clamp(xCameraGridLoc - sectorVisibilityDepth, _gridRenderer.From.x, _gridRenderer.To.x);
        float yRenderFrom = Mathf.Clamp(yCameraGridLoc - sectorVisibilityDepth, _gridRenderer.From.y, _gridRenderer.To.y);
        float zRenderFrom = Mathf.Clamp(zCameraGridLoc - sectorVisibilityDepth, _gridRenderer.From.z, _gridRenderer.To.z);

        float xRenderTo = Mathf.Clamp(xCameraGridLoc + sectorVisibilityDepth, _gridRenderer.From.x, _gridRenderer.To.x);
        float yRenderTo = Mathf.Clamp(yCameraGridLoc + sectorVisibilityDepth, _gridRenderer.From.y, _gridRenderer.To.y);
        float zRenderTo = Mathf.Clamp(zCameraGridLoc + sectorVisibilityDepth, _gridRenderer.From.z, _gridRenderer.To.z);

        // if any pair of axis values are equal, then only the first plane of vertexes will be showing, so expand it to one box showing
        ConfirmOneBoxDeep(ref xRenderFrom, ref xRenderTo);
        ConfirmOneBoxDeep(ref yRenderFrom, ref yRenderTo);
        ConfirmOneBoxDeep(ref zRenderFrom, ref zRenderTo);

        Vector3 renderFrom = new Vector3(xRenderFrom, yRenderFrom, zRenderFrom);
        Vector3 renderTo = new Vector3(xRenderTo, yRenderTo, zRenderTo);
        //D.Log("CameraGridLoc {2}, RenderFrom {0}, RenderTo {1}.", renderFrom, renderTo, gridLocOfCamera);

        // reqd as Version 2 GetVectrosityPoints() directly derives from Renderer's From and To
        // IMPROVE this will render grid cells outside the SPHERICAL universe
        _gridRenderer.From = renderFrom;
        _gridRenderer.To = renderTo;
        List<Vector3> gridPoints = _gridRenderer.GetVectrosityPoints();

        D.Assert(gridPoints.Any(), "{0}: No grid points to render.", GetType().Name);
        return gridPoints;
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
        System.DateTime startTime = System.DateTime.UtcNow;
        int gridBoxQty = Mathf.FloorToInt(_universeGridSize.magnitude);
        _gridVertexLocations = new List<Vector3>(gridBoxQty);
        _worldVertexLocations = new List<Vector3>(gridBoxQty);
        _gridBoxToSectorIndexLookup = new Dictionary<Vector3, Index3D>(gridBoxQty);
        _sectorIndexToGridBoxLookup = new Dictionary<Index3D, Vector3>(gridBoxQty);
        _sectorIndexToWorldBoxLocationLookup = new Dictionary<Index3D, Vector3>(gridBoxQty);
        _sectorIndexToSectorLookup = new Dictionary<Index3D, Sector>(gridBoxQty);

        float xPosAxisSize = _universeGridSize.x / 2F;
        float yPosAxisSize = _universeGridSize.y / 2F;
        float zPosAxisSize = _universeGridSize.z / 2F;
        for (float x = -xPosAxisSize; x < xPosAxisSize + 0.1F; x++) {
            for (float y = -yPosAxisSize; y < yPosAxisSize + 0.1F; y++) {
                for (float z = -zPosAxisSize; z < zPosAxisSize + 0.1F; z++) {
                    Vector3 gridVertexLocation = new Vector3(x, y, z);
                    _gridVertexLocations.Add(gridVertexLocation);
                    Vector3 worldVertexLocation = _grid.GridToWorld(gridVertexLocation);
                    _worldVertexLocations.Add(worldVertexLocation);

                    if (z.ApproxEquals(zPosAxisSize) || y.ApproxEquals(yPosAxisSize) || x.ApproxEquals(xPosAxisSize)) {
                        // this is an outside forward, up or right vertex of the grid, so the box would be outside the grid
                        continue;
                    }

                    Vector3 gridBoxLocation = gridVertexLocation + _gridVertexToBoxOffset;
                    Index3D index = CalculateSectorIndexFromGridLocation(gridBoxLocation);

                    _gridBoxToSectorIndexLookup.Add(gridBoxLocation, index);
                    _sectorIndexToGridBoxLookup.Add(index, gridBoxLocation);

                    Vector3 worldBoxLocation = _grid.GridToWorld(gridBoxLocation);
                    _sectorIndexToWorldBoxLocationLookup.Add(index, worldBoxLocation);

                    __AddSector(index, worldBoxLocation);
                }
            }
        }
        //D.Log("{0} grid and {1} world vertices found.", _gridVertexLocations.Count, _worldVertexLocations.Count);
        float elapsedTime = (float)(System.DateTime.UtcNow - startTime).TotalSeconds;
        D.Log("{0} spent {1:0.####} seconds building {2} sectors.", GetType().Name, elapsedTime, _sectorIndexToSectorLookup.Keys.Count);
    }

    /// <summary>
    /// Calculates the sector index from grid location. The grid space location can either be
    /// the location of a vertex or of a box.
    /// </summary>
    /// <param name="gridLoc">The grid location.</param>
    /// <returns></returns>
    private Index3D CalculateSectorIndexFromGridLocation(Vector3 gridLoc) {
        // Sector indexes will contain no 0 value. The sector index to the left of the origin is -1, to the right +1
        float x = gridLoc.x, y = gridLoc.y, z = gridLoc.z;
        int xIndex = x.ApproxEquals(Constants.ZeroF) ? 1 : (x > Constants.ZeroF ? Mathf.CeilToInt(x) : Mathf.FloorToInt(x));
        int yIndex = y.ApproxEquals(Constants.ZeroF) ? 1 : (y > Constants.ZeroF ? Mathf.CeilToInt(y) : Mathf.FloorToInt(y));
        int zIndex = z.ApproxEquals(Constants.ZeroF) ? 1 : (z > Constants.ZeroF ? Mathf.CeilToInt(z) : Mathf.FloorToInt(z));
        var index = new Index3D(xIndex, yIndex, zIndex);
        //D.Log("{0}: Sector GridLoc = {1}, resulting SectorIndex = {2}.", GetType().Name, gridLoc, index);
        return index;
    }

    private void __AddSector(Index3D index, Vector3 worldPosition) {
        Sector sector = MakeSectorInstance(index, worldPosition);
        _sectorIndexToSectorLookup.Add(index, sector);
        //D.Log("Sector added at index {0}.", index);
    }

    /// <summary>
    /// Gets the half value (1.5, 2.5, 1.5) location (in the grid coordinate system)
    /// associated with this sector index. This will be the center of the sector box.
    /// </summary>
    /// <param name="sectorIndex">The sector index.</param>
    /// <returns></returns>
    public Vector3 GetGridBoxLocation(Index3D sectorIndex) {
        D.Assert(sectorIndex != default(Index3D), "{0}: SectorIndex of {1} is illegal.", GetType().Name, sectorIndex);
        Vector3 gridBoxLocation;
        if (!_sectorIndexToGridBoxLookup.TryGetValue(sectorIndex, out gridBoxLocation)) {
            gridBoxLocation = CalculateGridBoxLocationFromSectorIndex(sectorIndex);
            _sectorIndexToGridBoxLookup.Add(sectorIndex, gridBoxLocation);
            _gridBoxToSectorIndexLookup.Add(gridBoxLocation, sectorIndex);
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
    /// Gets the round number (1.0, 1.0, 2.0) location (in the grid coordinate system)
    /// associated with this sector index. This will be the left, lower, back corner of the 
    /// sector box.
    /// </summary>
    /// <param name="sectorIndex">The sector index.</param>
    /// <returns></returns>
    public Vector3 GetGridVertexLocation(Index3D sectorIndex) {
        D.Assert(sectorIndex != default(Index3D), "{0}: SectorIndex of {1} is illegal.", GetType().Name, sectorIndex);
        return GetGridBoxLocation(sectorIndex) - _gridVertexToBoxOffset;
    }

    public Index3D GetSectorIndex(Vector3 worldPoint) {
        Index3D index;
        Vector3 gridClosestBoxLocation = _grid.NearestCell(worldPoint, RectGrid.CoordinateSystem.Grid);
        if (!_gridBoxToSectorIndexLookup.TryGetValue(gridClosestBoxLocation, out index)) {
            //D.Log("No Index at Grid Box Location {0}.", gridClosestBoxLocation);
            index = Instance.CalculateSectorIndexFromGridLocation(gridClosestBoxLocation);
            _gridBoxToSectorIndexLookup.Add(gridClosestBoxLocation, index);
            _sectorIndexToGridBoxLookup.Add(index, gridClosestBoxLocation);
        }
        return index;
    }

    /// <summary>
    /// Returns <c>true</c> if the sector's world position was acquired, <c>false</c> otherwise.
    /// Warning: While debugging, only a limited number of sectors are 'built' to
    /// reduce the time needed to construct valid paths for pathfinding.
    /// </summary>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <param name="worldPosition">The world position of the sector's center.</param>
    /// <returns></returns>
    public bool __TryGetSectorPosition(Index3D sectorIndex, out Vector3 worldPosition) {
        D.Assert(sectorIndex != default(Index3D), "{0}: SectorIndex of {1} is illegal.", GetType().Name, sectorIndex);
        bool isSectorFound = _sectorIndexToWorldBoxLocationLookup.TryGetValue(sectorIndex, out worldPosition);
        D.Warn(!isSectorFound, "{0} could not find a sector at Index {1}.", GetType().Name, sectorIndex);
        return isSectorFound;
    }

    /// <summary>
    /// Returns <c>true</c> if the sector was acquired, <c>false</c> otherwise.
    /// Warning: While debugging, only a limited number of sectors are 'built' to
    /// reduce the time needed to construct valid paths for pathfinding.
    /// </summary>
    /// <param name="sectorIndex">The index.</param>
    /// <param name="sector">The sector.</param>
    /// <returns></returns>
    public bool __TryGetSector(Index3D sectorIndex, out Sector sector) {
        D.Assert(sectorIndex != default(Index3D), "{0}: SectorIndex of {1} is illegal.", GetType().Name, sectorIndex);
        return _sectorIndexToSectorLookup.TryGetValue(sectorIndex, out sector);
    }

    /// <summary>
    /// Returns <c>true</c> if a sector is present at this index, <c>false</c> otherwise.
    /// Warning: While debugging, only a limited number of sectors are 'built' to
    /// reduce the time needed to construct valid paths for pathfinding.
    /// </summary>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <returns></returns>
    public bool __IsSectorPresentAt(Index3D sectorIndex) {
        return _sectorIndexToSectorLookup.ContainsKey(sectorIndex);
    }

    /// <summary>
    /// Gets the sector associated with this index. 
    /// Warning: Can be null while debugging as only a limited number of sectors are 'built'
    /// to reduce the time needed to construct valid paths for pathfinding.
    /// </summary>
    /// <param name="sectorIndex">The index.</param>
    /// <returns></returns>
    public Sector GetSector(Index3D sectorIndex) {
        D.Assert(sectorIndex != default(Index3D), "{0}: SectorIndex of {1} is illegal.", GetType().Name, sectorIndex);
        Sector sector;
        if (!_sectorIndexToSectorLookup.TryGetValue(sectorIndex, out sector)) {
            D.Warn("{0}: No Sector at {1}, returning null.", GetType().Name, sectorIndex);
        }
        return sector;
    }

    /// <summary>
    /// Gets the sector containing the provided worldPoint.
    /// Warning: Can be null while debugging as only a limited number of sectors are 'built'
    /// to reduce the time needed to construct valid paths for pathfinding.
    /// </summary>
    /// <param name="worldPoint">The world point.</param>
    /// <returns></returns>
    public Sector GetSectorContaining(Vector3 worldPoint) {
        var index = GetSectorIndex(worldPoint);
        return GetSector(index);
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
        //TODO add Nebula and DeepNebula
        return Topography.OpenSpace;
    }

    /// <summary>
    /// Gets the indexes of the neighbors to this sector index. A sector must
    /// occupy the index location to be included. The sector at center is not included.
    /// </summary>
    /// <param name="center">The center.</param>
    /// <returns></returns>
    public IList<Index3D> GetNeighboringIndices(Index3D center) {
        IList<Index3D> neighbors = new List<Index3D>();
        int[] xValuePair = CalcNeighborPair(center.x);
        int[] yValuePair = CalcNeighborPair(center.y);
        int[] zValuePair = CalcNeighborPair(center.z);
        foreach (var x in xValuePair) {
            foreach (var y in yValuePair) {
                foreach (var z in zValuePair) {
                    Index3D index = new Index3D(x, y, z);
                    if (__IsSectorPresentAt(index)) {
                        neighbors.Add(index);
                    }
                }
            }
        }
        return neighbors;
    }

    private int[] CalcNeighborPair(int center) {
        int[] valuePair = new int[2];
        // no 0 value in my sector indexes
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
    public IEnumerable<ISector_Ltd> GetNeighboringSectors(Index3D center) {
        IList<ISector_Ltd> neighborSectors = new List<ISector_Ltd>();
        foreach (var index in GetNeighboringIndices(center)) {
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
        D.Assert(first != default(Index3D), "{0}: SectorIndex of {1} is illegal.", GetType().Name, first);
        D.Assert(second != default(Index3D), "{0}: SectorIndex of {1} is illegal.", GetType().Name, second);
        Vector3 firstGridBoxLoc = GetGridBoxLocation(first);
        Vector3 secondGridBoxLoc = GetGridBoxLocation(second);
        return Vector3.Distance(firstGridBoxLoc, secondGridBoxLoc);
    }

    public void ShowSectorGrid(bool toShow) {
        if (IsGridWireframeShowing == toShow) {
            return;
        }
        if (toShow) {
            D.Assert(!IsGridWireframeShowing);
            List<Vector3> gridPoints = GenerateWireframeGridPoints(MainCameraControl.Instance.SectorIndex);
            _gridWireframe = new GridWireframe("GridWireframe", gridPoints);
            _gridWireframe.Show(true);
        }
        else {
            D.Assert(IsGridWireframeShowing);
            _gridWireframe.Show(false);
            _gridWireframe.Dispose();
            _gridWireframe = null;
        }
        //string msg = toShow ? "making new GridWireframe" : "destroying existing GridWireframe";
        //D.Log("{0} is {1}.", GetType().Name, msg);
    }

    /// <summary>
    /// Validates that no world sector corner is a duplicate
    /// or almost duplicate of another. 
    /// <remarks>Warning! 1300 corners takes 8+ secs.</remarks>
    /// </summary>
    private void __ValidateWorldSectorCorners() {
        System.DateTime startTime = System.DateTime.UtcNow;
        // verify no duplicate corners
        var worldCorners = SectorCorners;
        int cornerCount = worldCorners.Count;
        Vector3 iCorner, jCorner;
        for (int i = 0; i < cornerCount; i++) {
            iCorner = worldCorners[i];
            for (int j = i; j < cornerCount; j++) {
                if (i == j) { continue; }
                jCorner = worldCorners[j];
                D.Assert(!Mathfx.Approx(iCorner, jCorner, 10F), "{0} == {1}.", iCorner.ToPreciseString(), jCorner.ToPreciseString());
            }
        }

        float elapsedTime = (float)(System.DateTime.UtcNow - startTime).TotalSeconds;
        D.Log("{0} spent {1:0.####} secs validating {2} sector corners.", GetType().Name, elapsedTime, SectorCorners.Count);
    }

    /// <summary>
    /// Returns <c>true</c> if the sector indicated by sectorIndex contains a System, <c>false</c> otherwise.
    /// HACK pending creating a collection for each custom type in the universe, organized by sectorIndex.
    /// </summary>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <param name="system">The system if present in the sector.</param>
    /// <returns></returns>
    public bool TryGetSystem(Index3D sectorIndex, out ISystem system) {
        D.Assert(sectorIndex != default(Index3D), "{0}: SectorIndex of {1} is illegal.", GetType().Name, sectorIndex);
        SystemItem sys = null;
        if (SystemCreator.TryGetSystem(sectorIndex, out sys)) {
            system = sys as ISystem;
            return true;
        }
        system = null;
        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if the sector indicated by sectorIndex contains one or more Starbases, <c>false</c> otherwise.
    /// HACK pending creating a collection for each custom type in the universe, organized by sectorIndex.
    /// </summary>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <param name="starbasesInSector">The resulting starbases in sector.</param>
    /// <returns></returns>
    public bool TryGetStarbases(Index3D sectorIndex, out IEnumerable<IStarbaseCmd> starbasesInSector) {
        D.Assert(sectorIndex != default(Index3D), "{0}: SectorIndex of {1} is illegal.", GetType().Name, sectorIndex);
        IEnumerable<StarbaseCmdItem> sBases;
        if (StarbaseUnitCreator.TryGetStarbases(sectorIndex, out sBases)) {
            starbasesInSector = sBases.Cast<IStarbaseCmd>();
            return true;
        }
        starbasesInSector = Enumerable.Empty<IStarbaseCmd>();
        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if the sector indicated by sectorIndex contains one or more Starbases, <c>false</c> otherwise.
    /// HACK pending creating a collection for each custom type in the universe, organized by sectorIndex.
    /// </summary>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <param name="starbasesInSector">The resulting starbases in sector.</param>
    /// <returns></returns>
    public bool TryGetFleets(Index3D sectorIndex, out IEnumerable<IFleetCmd> fleetsInSector) {
        D.Assert(sectorIndex != default(Index3D), "{0}: SectorIndex of {1} is illegal.", GetType().Name, sectorIndex);
        IEnumerable<FleetCmdItem> fleets;
        if (FleetUnitCreator.TryGetFleets(sectorIndex, out fleets)) {
            fleetsInSector = fleets.Cast<IFleetCmd>();
            return true;
        }
        fleetsInSector = Enumerable.Empty<IFleetCmd>();
        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if the sector indicated by sectorIndex contains one or more ISectorViewHighlightables, <c>false</c> otherwise.
    /// HACK pending creating a collection for each custom type in the universe, organized by sectorIndex.
    /// </summary>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <param name="highlightablesInSector">The highlightables in sector.</param>
    /// <returns></returns>
    public bool TryGetSectorViewHighlightables(Index3D sectorIndex, out IEnumerable<ISectorViewHighlightable> highlightablesInSector) {
        D.Assert(sectorIndex != default(Index3D), "{0}: SectorIndex of {1} is illegal.", GetType().Name, sectorIndex);
        List<ISectorViewHighlightable> sectorHighlightables = new List<ISectorViewHighlightable>();
        ISystem system;
        if (TryGetSystem(sectorIndex, out system)) {
            ISectorViewHighlightable sys = system as ISectorViewHighlightable;
            D.Assert(sys != null);
            sectorHighlightables.Add(sys);
        }
        IEnumerable<IStarbaseCmd> starbases;
        if (TryGetStarbases(sectorIndex, out starbases)) {
            IEnumerable<ISectorViewHighlightable> highlightableStarbases = starbases.Cast<ISectorViewHighlightable>();
            D.Assert(!highlightableStarbases.IsNullOrEmpty());
            sectorHighlightables.AddRange(highlightableStarbases);
        }
        IEnumerable<IFleetCmd> fleets;
        if (TryGetFleets(sectorIndex, out fleets)) {
            IEnumerable<ISectorViewHighlightable> highlightableFleets = fleets.Cast<ISectorViewHighlightable>();
            D.Assert(!highlightableFleets.IsNullOrEmpty());
            sectorHighlightables.AddRange(highlightableFleets);
        }

        if (sectorHighlightables.Any()) {
            highlightablesInSector = sectorHighlightables;
            return true;
        }
        highlightablesInSector = null;
        return false;
    }

    private Sector MakeSectorInstance(Index3D sectorIndex, Vector3 worldLocation) {
        Sector sector = new Sector(worldLocation);

        sector.Name = SectorNameFormat.Inject(sectorIndex);
        SectorData data = new SectorData(sector, sectorIndex) {
            //Density = 1F  the concept of space density is now attached to Topography, not Sectors. Density is relative and affects only drag
        };
        sector.Data = data;
        // IMPROVE use data values in place of sector values
        return sector;
    }

    protected override void Cleanup() {
        References.SectorGrid = null;
        if (_gridWireframe != null) {
            _gridWireframe.Dispose();
        }
        foreach (var sector in AllSectors) {
            sector.Dispose();
        }
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(s => s.Dispose());
        _subscriptions.Clear();
        _gameMgr.gameStateChanged -= GameStateChangedEventHandler;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

