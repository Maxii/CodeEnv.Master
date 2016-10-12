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
public class SectorGrid : AMonoSingleton<SectorGrid>, ISectorGrid {

    private const string SectorNameFormat = "Sector {0}";

    private string Name { get { return GetType().Name; } }

    [Tooltip("Controls how many layers of sectors are visible when in SectorViewMode")]
    [Range(2F, 5F)]
    [SerializeField]
    private float _sectorVisibilityDepth = 3F;

    [Tooltip("Limits grid size to Max Grid Size")]
    [SerializeField]
    private bool _enableGridSizeLimit = true;

    [Tooltip("Max size of the grid in sectors. Must be cube of even values")]
    [SerializeField]
    private Vector3 _debugMaxGridSize = new Vector3(10, 10, 10);

    public IEnumerable<Sector> Sectors { get { return _sectorIndexToSectorLookup.Values; } }

    public IEnumerable<IntVector3> SectorIndices { get { return _sectorIndexToSectorLookup.Keys; } }

    public IEnumerable<IntVector3> NonPeripheralSectorIndices { get { return _nonPeripheralSectorIndices; } }

    /// <summary>
    /// Read-only. The location of the center of all sectors in world space.
    /// </summary>
    public IEnumerable<Vector3> SectorCenters { get { return _sectorIndexToCellWorldLocationLookup.Values; } }

    [Obsolete]
    public Sector RandomSector { get { return RandomExtended.Choice(_sectorIndexToSectorLookup.Values); } }

    /// <summary>
    ///  Read-only. The location of the corners of all sectors in world space.
    /// </summary>
    private IList<Vector3> SectorCorners { get { return _cellVertexWorldLocations; } }

    private bool IsGridWireframeShowing { get { return _gridWireframe != null && _gridWireframe.IsShowing; } }

    /// <summary>
    /// The offset that converts cellVertex grid coordinates into cell grid coordinates.
    /// </summary>
    private Vector3 _cellVertexGridCoordinatesToCellGridCoordinatesOffset = new Vector3(0.5F, 0.5F, 0.5F);

    /// <summary>
    /// The world location of each cell vertex. Only contains cells inside the radius of the universe.
    /// <remarks>A cell vertex is effectively the cell index in the grid coordinate system, aka Vector3(1, -1, 0).</remarks>
    /// </summary>
    private IList<Vector3> _cellVertexWorldLocations;

    /// <summary>
    /// Lookup that translates a cell's GridCoordinates into the appropriate sector index.
    /// Only contains cells inside the radius of the universe.
    /// <remarks>Cell GridCoordinates are always half values, aka Vector3(0.5F, 2.0F, -3.5F).
    /// They mark the center of each cell in the GridCoordinate system.</remarks>
    /// </summary>
    private IDictionary<Vector3, IntVector3> _cellGridCoordinatesToSectorIndexLookup;

    /// <summary>
    /// Lookup that translates sector indices into the GridCoordinates of the cell/sector. 
    /// Only contains cells inside the radius of the universe.
    /// <remarks>Cell GridCoordinates are always half values, aka Vector3(0.5F, 2.0F, -3.5F).</remarks>
    /// </summary>
    private IDictionary<IntVector3, Vector3> _sectorIndexToCellGridCoordinatesLookup;

    /// <summary>
    /// Lookup that translates sector indices into the world location of the cell/sector.
    /// Only contains cells inside the radius of the universe.
    /// </summary>
    private IDictionary<IntVector3, Vector3> _sectorIndexToCellWorldLocationLookup;

    /// <summary>
    /// The sector index to sector lookup. Only contains sectors inside the radius of the universe.
    /// </summary>
    private IDictionary<IntVector3, Sector> _sectorIndexToSectorLookup;

    private IList<IntVector3> _nonPeripheralSectorIndices;

    /// <summary>
    /// The grid coordinates of the cell vertex furthest from the center along the positive axes of the grid.
    /// Its counterpart along the negative axes is the inverse.
    /// </summary>
    private Vector3 _outermostCellVertexGridCoordinates;

    /// <summary>
    /// The size of the grid of cells that encompasses the universe. Many of these cells are outside
    /// the radius of the universe and therefore do not result in the creation of a sector.
    /// <remarks>Always a cube of even integer values, aka 10x10x10, 4x4x4, etc.</remarks>
    /// </summary>
    private Vector3 _gridSize;
    private RectGrid _grid;
    private Parallelepiped _gridRenderer;
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
    }

    // 8.16.16 Control of GameState progression when sectors are built now handled by UniverseBuilder

    private void DynamicallySubscribe(bool toSubscribe) {
        if (toSubscribe) {
            _subscriptions.Add(MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, IntVector3>(cc => cc.SectorIndex, CameraSectorIndexPropChangedHandler));
        }
        else {
            IDisposable d = _subscriptions.Single(s => s as DisposePropertyChangedSubscription<MainCameraControl> != null);
            _subscriptions.Remove(d);
            d.Dispose();
        }
    }

    public void BuildSectors() {
        InitializeGridSize();
        ConstructSectors();
        //__ValidateWorldSectorCorners();   // can take multiple seconds
    }

    private void InitializeGridSize() {
        int cellCountInsideUniverseAlongAGridAxis = CalcCellCountInsideUniverseAlongAGridAxis();
        D.Assert(cellCountInsideUniverseAlongAGridAxis % 2 == Constants.Zero, "{0}: CellCount {1} can't be odd.", Name, cellCountInsideUniverseAlongAGridAxis);
        _gridSize = Vector3.one * cellCountInsideUniverseAlongAGridAxis;
        if (_enableGridSizeLimit) {
            if (_gridSize.sqrMagnitude > _debugMaxGridSize.sqrMagnitude) {
                // only use DebugMaxGridSize if the Universe sized grid is bigger
                _gridSize = _debugMaxGridSize;
            }
        }
        D.Assert(_gridSize.x.ApproxEquals(_gridSize.y) && _gridSize.y.ApproxEquals(_gridSize.z), "{0}: {1} must be cube.", Name, _gridSize);
        D.Assert((_gridSize.x % 2F).ApproxEquals(Constants.ZeroF), "{0}: {1} must use even values.", Name, _gridSize);

        _outermostCellVertexGridCoordinates = _gridSize / 2F;
        D.Log("{0}: Universe Grid Size = {1}.", Name, _gridSize);
    }

    private int CalcCellCountInsideUniverseAlongAGridAxis() {
        float universeRadius = _gameMgr.GameSettings.UniverseSize.Radius();
        float cellCountAlongGridPosAxisInsideUniverse = universeRadius / TempGameValues.SectorSideLength;
        return Mathf.RoundToInt(cellCountAlongGridPosAxisInsideUniverse) * 2;
    }

    #region Event and Property Change Handlers

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

    private List<Vector3> GenerateWireframeGridPoints(IntVector3 cameraSectorIndex) {
        // per GridFramework: grid needs to be at origin for rendering to align properly with the grid ANY TIME vectrosity points are generated
        D.Assert(Mathfx.Approx(transform.position, Vector3.zero, .01F), "{0} must be located at origin.", Name);
        Vector3 cameraCellVertexGridCoordinates = GetCellVertexGridCoordinatesFrom(cameraSectorIndex);

        float cameraCellVertexGridCoordinate_X = cameraCellVertexGridCoordinates.x;
        float cameraCellVertexGridCoordinate_Y = cameraCellVertexGridCoordinates.y;
        float cameraCellVertexGridCoordinate_Z = cameraCellVertexGridCoordinates.z;

        // construct from and to values, keeping them within the limits of the grid of cells
        float xRenderFrom = Mathf.Clamp(cameraCellVertexGridCoordinate_X - _sectorVisibilityDepth, -_outermostCellVertexGridCoordinates.x, _outermostCellVertexGridCoordinates.x);
        float yRenderFrom = Mathf.Clamp(cameraCellVertexGridCoordinate_Y - _sectorVisibilityDepth, -_outermostCellVertexGridCoordinates.y, _outermostCellVertexGridCoordinates.y);
        float zRenderFrom = Mathf.Clamp(cameraCellVertexGridCoordinate_Z - _sectorVisibilityDepth, -_outermostCellVertexGridCoordinates.z, _outermostCellVertexGridCoordinates.z);

        float xRenderTo = Mathf.Clamp(cameraCellVertexGridCoordinate_X + _sectorVisibilityDepth, -_outermostCellVertexGridCoordinates.x, _outermostCellVertexGridCoordinates.x);
        float yRenderTo = Mathf.Clamp(cameraCellVertexGridCoordinate_Y + _sectorVisibilityDepth, -_outermostCellVertexGridCoordinates.y, _outermostCellVertexGridCoordinates.y);
        float zRenderTo = Mathf.Clamp(cameraCellVertexGridCoordinate_Z + _sectorVisibilityDepth, -_outermostCellVertexGridCoordinates.z, _outermostCellVertexGridCoordinates.z);

        // if any pair of axis values are equal, then only the first plane of vertices will be showing, so expand it to one box showing
        ConfirmOneBoxDeep(ref xRenderFrom, ref xRenderTo);
        ConfirmOneBoxDeep(ref yRenderFrom, ref yRenderTo);
        ConfirmOneBoxDeep(ref zRenderFrom, ref zRenderTo);

        Vector3 renderFrom = new Vector3(xRenderFrom, yRenderFrom, zRenderFrom);
        Vector3 renderTo = new Vector3(xRenderTo, yRenderTo, zRenderTo);
        //D.Log("{0} after depth adjust: CameraCellVertexGridCoordinates {1}, RenderFrom {2}, RenderTo {3}.", Name, cameraCellVertexGridCoordinates, renderFrom, renderTo);

        // reqd as Version 2 GetVectrosityPoints() directly derives from Renderer's From and To
        _gridRenderer.From = renderFrom;
        _gridRenderer.To = renderTo;
        List<Vector3> gridPoints = _gridRenderer.GetVectrosityPoints();

        D.Assert(gridPoints.Any(), "{0}: No grid points to render.", Name);
        return gridPoints;
    }

    /// <summary>
    /// Confirms (and modifies if necessary) that there will be a minimum depth of one sector
    /// visible in the grid to render no matter how far away the camera is.
    /// </summary>
    /// <param name="from">The from value.</param>
    /// <param name="to">The to value.</param>
    private void ConfirmOneBoxDeep(ref float from, ref float to) {
        if (from.ApproxEquals(to)) {
            return;
        }
        if (to > Constants.ZeroF) {
            from = to - 1F;
        }
        else if (from < Constants.ZeroF) {
            to = from + 1F;
        }
    }

    private void ConstructSectors() {
        __RecordDurationStartTime();

        int cellQty = Mathf.RoundToInt(_gridSize.magnitude); // OPTIMIZE cell qty >> sector qty
        _cellVertexWorldLocations = new List<Vector3>(cellQty);
        _cellGridCoordinatesToSectorIndexLookup = new Dictionary<Vector3, IntVector3>(cellQty);
        _sectorIndexToCellGridCoordinatesLookup = new Dictionary<IntVector3, Vector3>(cellQty);
        _sectorIndexToCellWorldLocationLookup = new Dictionary<IntVector3, Vector3>(cellQty);
        _sectorIndexToSectorLookup = new Dictionary<IntVector3, Sector>(cellQty);
        _nonPeripheralSectorIndices = new List<IntVector3>();

        float universeRadius = _gameMgr.GameSettings.UniverseSize.Radius();
        float universeRadiusSqrd = universeRadius * universeRadius;
        float cellOnPeripheryThresholdDistance = universeRadius - TempGameValues.SectorSideLength;
        float cellOnPeripheryThresholdDistanceSqrd = cellOnPeripheryThresholdDistance * cellOnPeripheryThresholdDistance;

        int peripheryCellCount = Constants.Zero;

        // all axes have the same range of lowest to highest values so just pick x
        int highestCellVertexGridCoordinateAxisValue = Mathf.RoundToInt(_outermostCellVertexGridCoordinates.x); // float values are effectively ints
        int lowestCellVertexGridCoordinateAxisValue = -highestCellVertexGridCoordinateAxisValue;

        // x, y and z are their respective axis's cell vertex grid coordinate
        for (int x = lowestCellVertexGridCoordinateAxisValue; x < highestCellVertexGridCoordinateAxisValue; x++) {    // '<=' adds vertices on outer edge but no sectors
            for (int y = lowestCellVertexGridCoordinateAxisValue; y < highestCellVertexGridCoordinateAxisValue; y++) {
                for (int z = lowestCellVertexGridCoordinateAxisValue; z < highestCellVertexGridCoordinateAxisValue; z++) {
                    Vector3 cellVertexGridCoordinates = new Vector3(x, y, z);
                    Vector3 cellVertexWorldLocation = _grid.GridToWorld(cellVertexGridCoordinates);

                    bool isCellOnPeriphery = false;
                    if (TryDetermineWhetherCellVertexIsInsideUniverse(cellVertexWorldLocation, universeRadiusSqrd, cellOnPeripheryThresholdDistanceSqrd, out isCellOnPeriphery)) {
                        Vector3 cellGridCoordinates = cellVertexGridCoordinates + _cellVertexGridCoordinatesToCellGridCoordinatesOffset; // (0.5, 1.5, 6.5)
                        IntVector3 sectorIndex = CalculateSectorIndexFromCellGridCoordindates(cellGridCoordinates);

                        _cellVertexWorldLocations.Add(cellVertexWorldLocation);
                        _cellGridCoordinatesToSectorIndexLookup.Add(cellGridCoordinates, sectorIndex);
                        _sectorIndexToCellGridCoordinatesLookup.Add(sectorIndex, cellGridCoordinates);

                        Vector3 cellWorldLocation = _grid.GridToWorld(cellGridCoordinates);
                        _sectorIndexToCellWorldLocationLookup.Add(sectorIndex, cellWorldLocation);

                        if (isCellOnPeriphery) {
                            peripheryCellCount++;
                        }
                        else {
                            _nonPeripheralSectorIndices.Add(sectorIndex);
                        }
                        __AddSector(sectorIndex, cellWorldLocation, isCellOnPeriphery);
                    }
                }
            }
        }

        D.Log("{0} found {1} grid/world vertices and created {2} sectors/cells of which {3} are non-periphery.",
        Name, _cellVertexWorldLocations.Count, _sectorIndexToSectorLookup.Keys.Count, _sectorIndexToSectorLookup.Keys.Count - peripheryCellCount);
        __LogDuration("{0}.ConstructSectors()".Inject(Name));
    }

    /// <summary>
    /// Returns <c>true</c> if the provided cell vertex world location is contained within the radius of the universe, <c>false</c> otherwise.
    /// </summary>
    /// <param name="cellVertexWorldLoc">The cell vertex world location.</param>
    /// <param name="universeRadiusSqrd">The universe radius SQRD.</param>
    /// <param name="onPeripheryRadiusSqrd">The on periphery radius SQRD.</param>
    /// <param name="isCellOnPeriphery">if set to <c>true</c> the cell represented by this cellVertex location is on the periphery of the universe,
    /// aka its the outer cell along the universe border.</param>
    /// <returns></returns>
    private bool TryDetermineWhetherCellVertexIsInsideUniverse(Vector3 cellVertexWorldLoc, float universeRadiusSqrd, float onPeripheryRadiusSqrd, out bool isCellOnPeriphery) {
        isCellOnPeriphery = false;
        bool isCellVertexInsideUniverse = false;
        float sqrDistanceFromUniverseCenterToCellVertex = Vector3.SqrMagnitude(cellVertexWorldLoc);

        // Note: use of '<=' would add all vertices on the edge of the universe, but cells would all be outside it
        if (sqrDistanceFromUniverseCenterToCellVertex < universeRadiusSqrd) {
            isCellOnPeriphery = sqrDistanceFromUniverseCenterToCellVertex > onPeripheryRadiusSqrd;
            isCellVertexInsideUniverse = true;
        }
        return isCellVertexInsideUniverse;
    }

    /// <summary>
    /// Calculates the sector index from the provided cellGridCoordinates, aka the center of a cell in
    /// the GridCoordinate system.
    /// </summary>
    /// <param name="cellGridCoordinates">The cell's coordinates in the GridCoordindate System.</param>
    /// <returns></returns>
    private IntVector3 CalculateSectorIndexFromCellGridCoordindates(Vector3 cellGridCoordinates) {
        // Sector indexes will contain no 0 value. The sector index to the left of the origin is -1, to the right +1
        float x = cellGridCoordinates.x, y = cellGridCoordinates.y, z = cellGridCoordinates.z;
        int xIndex = x.ApproxEquals(Constants.ZeroF) ? 1 : (x > Constants.ZeroF ? Mathf.CeilToInt(x) : Mathf.FloorToInt(x));
        int yIndex = y.ApproxEquals(Constants.ZeroF) ? 1 : (y > Constants.ZeroF ? Mathf.CeilToInt(y) : Mathf.FloorToInt(y));
        int zIndex = z.ApproxEquals(Constants.ZeroF) ? 1 : (z > Constants.ZeroF ? Mathf.CeilToInt(z) : Mathf.FloorToInt(z));
        var index = new IntVector3(xIndex, yIndex, zIndex);
        //D.Log("{0}: CellGridCoordinates = {1}, resulting SectorIndex = {2}.", Name, cellGridCoordinates, index);
        return index;
    }

    private void __AddSector(IntVector3 index, Vector3 worldPosition, bool isOnPeriphery) {
        Sector sector = MakeSectorInstance(index, worldPosition, isOnPeriphery);
        _sectorIndexToSectorLookup.Add(index, sector);
        //D.Log("Sector added at index {0}.", index);
    }

    /// <summary>
    /// Gets the half value (1.5, 2.5, 1.5) location (in the grid coordinate system)
    /// associated with this sector index. This will be the center of the cell in the
    /// GridCoordinate system.
    /// </summary>
    /// <param name="sectorIndex">The sector index.</param>
    /// <returns></returns>
    private Vector3 GetCellGridCoordinatesFor(IntVector3 sectorIndex) {
        Vector3 cellGridCoordinates;
        if (!_sectorIndexToCellGridCoordinatesLookup.TryGetValue(sectorIndex, out cellGridCoordinates)) {
            cellGridCoordinates = CalculateCellGridCoordinatesFromSectorIndex(sectorIndex);
            _sectorIndexToCellGridCoordinatesLookup.Add(sectorIndex, cellGridCoordinates);
            _cellGridCoordinatesToSectorIndexLookup.Add(cellGridCoordinates, sectorIndex);
        }
        return cellGridCoordinates;
    }

    private Vector3 CalculateCellGridCoordinatesFromSectorIndex(IntVector3 sectorIndex) {
        int xIndex = sectorIndex.x, yIndex = sectorIndex.y, zIndex = sectorIndex.z;
        D.Assert(xIndex != 0 && yIndex != 0 && zIndex != 0, "Illegal Index {0}.".Inject(sectorIndex));
        float x = xIndex > Constants.Zero ? xIndex - 1F : xIndex;
        float y = yIndex > Constants.Zero ? yIndex - 1F : yIndex;
        float z = zIndex > Constants.Zero ? zIndex - 1F : zIndex;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Gets the round number (1.0, 1.0, 2.0) location (in the grid coordinate system)
    /// associated with this sector index. This will be the left, lower, back corner of the cell.
    /// </summary>
    /// <param name="sectorIndex">The sector index.</param>
    /// <returns></returns>
    private Vector3 GetCellVertexGridCoordinatesFrom(IntVector3 sectorIndex) {
        D.Assert(sectorIndex != default(IntVector3), "{0}: SectorIndex of {1} is illegal.", Name, sectorIndex);
        return GetCellGridCoordinatesFor(sectorIndex) - _cellVertexGridCoordinatesToCellGridCoordinatesOffset;
    }

    /// <summary>
    /// Returns the Sector Index that contains the provided world location.
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <returns></returns>
    public IntVector3 GetSectorIndexThatContains(Vector3 worldLocation) {
        IntVector3 sectorIndex;
        Vector3 nearestCellGridCoordinates = _grid.NearestCell(worldLocation, RectGrid.CoordinateSystem.Grid);
        if (!_cellGridCoordinatesToSectorIndexLookup.TryGetValue(nearestCellGridCoordinates, out sectorIndex)) {
            // 8.9.16 Should never occur unless debugMaxGridSize is used as all worldLocations inside Universe should be 
            // encompassed by a cell. Currently using debugMaxGridSize always results in Camera initial location warning
            D.Warn(!_enableGridSizeLimit, "No Index stored at CellGridCoordinates {0}. Adding.", nearestCellGridCoordinates);
            //D.Error("No Index stored at CellGridCoordinates {0}.", nearestCellGridCoordinates);
            sectorIndex = Instance.CalculateSectorIndexFromCellGridCoordindates(nearestCellGridCoordinates);
            _cellGridCoordinatesToSectorIndexLookup.Add(nearestCellGridCoordinates, sectorIndex);
            _sectorIndexToCellGridCoordinatesLookup.Add(sectorIndex, nearestCellGridCoordinates);
        }
        return sectorIndex;
    }

    /// <summary>
    /// Returns <c>true</c> if the sector's world position was acquired, <c>false</c> otherwise.
    /// Warning: While debugging, only a limited number of sectors are 'built' to
    /// reduce the time needed to construct valid paths for pathfinding.
    /// </summary>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <param name="worldPosition">The world position of the sector's center.</param>
    /// <returns></returns>
    public bool __TryGetSectorPosition(IntVector3 sectorIndex, out Vector3 worldPosition) {
        D.Assert(sectorIndex != default(IntVector3), "{0}: SectorIndex of {1} is illegal.", Name, sectorIndex);
        bool isSectorFound = _sectorIndexToCellWorldLocationLookup.TryGetValue(sectorIndex, out worldPosition);
        D.Warn(!isSectorFound, "{0} could not find a sector at Index {1}.", Name, sectorIndex);
        return isSectorFound;
    }

    /// <summary>
    /// Gets the world space position of the sector indicated by the provided sectorIndex.
    /// Throws an error if no sector is present at that index.
    /// </summary>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <returns></returns>
    public Vector3 GetSectorPosition(IntVector3 sectorIndex) {
        D.Assert(sectorIndex != default(IntVector3), "{0}: SectorIndex of {1} is illegal.", Name, sectorIndex);
        Vector3 worldPosition;
        bool isSectorFound = _sectorIndexToCellWorldLocationLookup.TryGetValue(sectorIndex, out worldPosition);
        D.Assert(isSectorFound, "{0} could not find a sector at Index {1}.", Name, sectorIndex);
        return worldPosition;
    }

    /// <summary>
    /// Returns <c>true</c> if the sector was acquired, <c>false</c> otherwise.
    /// Warning: While debugging, only a limited number of sectors are 'built' to
    /// reduce the time needed to construct valid paths for pathfinding.
    /// </summary>
    /// <param name="sectorIndex">The index.</param>
    /// <param name="sector">The sector.</param>
    /// <returns></returns>
    public bool __TryGetSector(IntVector3 sectorIndex, out Sector sector) {
        D.Assert(sectorIndex != default(IntVector3), "{0}: SectorIndex of {1} is illegal.", Name, sectorIndex);
        return _sectorIndexToSectorLookup.TryGetValue(sectorIndex, out sector);
    }

    /// <summary>
    /// Returns <c>true</c> if a sector is present at this index, <c>false</c> otherwise.
    /// Warning: While debugging, only a limited number of sectors are 'built' to
    /// reduce the time needed to construct valid paths for pathfinding.
    /// <remarks>DebugInfo uses this to properly construct CameraLocation text as the
    /// camera can move to the edge of the universe even if sectors are not present.</remarks>
    /// </summary>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <returns></returns>
    public bool __IsSectorPresentAt(IntVector3 sectorIndex) {
        return _sectorIndexToSectorLookup.ContainsKey(sectorIndex);
    }

    /// <summary>
    /// Gets the sector associated with this index. 
    /// Warning: Can be null while debugging as only a limited number of sectors are 'built'
    /// to reduce the time needed to construct valid paths for pathfinding.
    /// </summary>
    /// <param name="sectorIndex">The index.</param>
    /// <returns></returns>
    public Sector GetSector(IntVector3 sectorIndex) {
        D.Assert(sectorIndex != default(IntVector3), "{0}: SectorIndex of {1} is illegal.", Name, sectorIndex);
        Sector sector;
        if (!_sectorIndexToSectorLookup.TryGetValue(sectorIndex, out sector)) {
            D.Warn("{0}: No Sector at {1}, returning null.", Name, sectorIndex);
        }
        return sector;
    }

    /// <summary>
    /// Gets the sector containing the provided worldPoint.
    /// Warning: Can be null while debugging as only a limited number of sectors are 'built'
    /// to reduce the time needed to construct valid paths for pathfinding.
    /// </summary>
    /// <param name="worldLocation">The world point.</param>
    /// <returns></returns>
    public Sector GetSectorContaining(Vector3 worldLocation) {
        var index = GetSectorIndexThatContains(worldLocation);
        return GetSector(index);
    }

    /// <summary>
    /// Gets a random sector index.
    /// </summary>
    /// <param name="includePeriphery">if set to <c>true</c> [include periphery].</param>
    /// <param name="excludedIndices">The excluded indices.</param>
    /// <returns></returns>
    public IntVector3 GetRandomSectorIndex(bool includePeriphery = false, IEnumerable<IntVector3> excludedIndices = null) {
        IEnumerable<IntVector3> indicesToChooseFrom = includePeriphery ? _sectorIndexToSectorLookup.Keys : _nonPeripheralSectorIndices;
        if (excludedIndices != null) {
            indicesToChooseFrom = indicesToChooseFrom.Except(excludedIndices);
        }
        return RandomExtended.Choice(indicesToChooseFrom);
    }

    /// <summary>
    /// Returns the indices surrounding <c>center</c> that are between <c>minDistance</c> and <c>maxDistance</c>
    /// from <c>center</c>. The UOM for both distance values is sectors.
    /// </summary>
    /// <param name="center">The center.</param>
    /// <param name="minDistance">The minimum distance in sectors.</param>
    /// <param name="maxDistance">The maximum distance in sectors.</param>
    /// <returns></returns>
    public IList<IntVector3> GetSurroundingIndicesBetween(IntVector3 center, int minDistance, int maxDistance) {
        D.Assert(center != default(IntVector3), "{0}: SectorIndex of {1} is illegal.", Name, center);
        D.Assert(__IsSectorPresentAt(center));
        Utility.ValidateForRange(minDistance, 0, maxDistance - 1);
        Utility.ValidateForRange(maxDistance, minDistance + 1, _gameMgr.GameSettings.UniverseSize.RadiusInSectors() * 2);

        __RecordDurationStartTime();

        int minDistanceSqrd = minDistance * minDistance;
        int maxDistanceSqrd = maxDistance * maxDistance;

        Vector3 centerCell = _sectorIndexToCellGridCoordinatesLookup[center];

        Vector3[] allCandidateGridCells = _cellGridCoordinatesToSectorIndexLookup.Keys.Except(centerCell).ToArray();
        int gridCellQty = allCandidateGridCells.Length;
        IList<IntVector3> result = new List<IntVector3>(gridCellQty);

        for (int i = 0; i < gridCellQty; i++) {
            Vector3 candidateCell = allCandidateGridCells[i];
            Vector3 vectorFromCenterToCandidateCell = candidateCell - centerCell;
            float vectorSqrMagnitude = vectorFromCenterToCandidateCell.sqrMagnitude;
            if (vectorSqrMagnitude >= minDistanceSqrd && vectorSqrMagnitude <= maxDistanceSqrd) {
                IntVector3 candidateCellIndex = _cellGridCoordinatesToSectorIndexLookup[candidateCell];
                result.Add(candidateCellIndex);
            }
        }
        __LogDuration("{0}.GetSurroundingIndicesBetween()".Inject(Name));
        return result;
    }

    /// <summary>
    /// Gets the indexes of the neighbors to this sector index. 
    /// A sector must be present at the index location to be included. The sector at center is not included.
    /// </summary>
    /// <param name="center">The center.</param>
    /// <returns></returns>
    public IList<IntVector3> GetNeighboringIndices(IntVector3 center) {
        IList<IntVector3> neighbors = new List<IntVector3>();
        int[] xValuePair = CalcNeighborPair(center.x);
        int[] yValuePair = CalcNeighborPair(center.y);
        int[] zValuePair = CalcNeighborPair(center.z);
        foreach (var x in xValuePair) {
            foreach (var y in yValuePair) {
                foreach (var z in zValuePair) {
                    IntVector3 index = new IntVector3(x, y, z);
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
    /// Gets the neighboring sectors to this sector index. 
    /// A sector must be present at the index location to be included. The sector at center is not included.
    /// </summary>
    /// <param name="center">The center.</param>
    /// <returns></returns>
    public IEnumerable<ISector_Ltd> GetNeighboringSectors(IntVector3 center) {
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
    /// <param name="firstSectorIndex">The first.</param>
    /// <param name="secondSectorIndex">The second.</param>
    /// <returns></returns>
    public float GetDistanceInSectors(IntVector3 firstSectorIndex, IntVector3 secondSectorIndex) {
        D.Assert(firstSectorIndex != default(IntVector3), "{0}: SectorIndex of {1} is illegal.", Name, firstSectorIndex);
        D.Assert(secondSectorIndex != default(IntVector3), "{0}: SectorIndex of {1} is illegal.", Name, secondSectorIndex);
        Vector3 firstCellGridCoordinates = GetCellGridCoordinatesFor(firstSectorIndex);
        Vector3 secondCellGridCoordindates = GetCellGridCoordinatesFor(secondSectorIndex);
        return Vector3.Distance(firstCellGridCoordinates, secondCellGridCoordindates);
    }

    /// <summary>
    /// Gets the distance in sectors from the origin, aka the location of the UniverseCenter.
    /// </summary>
    /// <param name="sectorIndex">Index of the sector.</param>
    /// <returns></returns>
    public float GetDistanceInSectorsFromOrigin(IntVector3 sectorIndex) {
        Vector3 cellGridCoordinates = GetCellGridCoordinatesFor(sectorIndex);
        return Vector3.Distance(cellGridCoordinates, Vector3.zero);
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
        //D.Log("{0} is {1}.", Name, msg);
    }

    /// <summary>
    /// Validates that no world sector corner is a duplicate
    /// or almost duplicate of another. 
    /// <remarks>Warning! 1300 corners takes 8+ secs.</remarks>
    /// </summary>
    private void __ValidateWorldSectorCorners() {
        __RecordDurationStartTime();
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
        D.Log("{0} validated {1} sector corners.", Name, SectorCorners.Count);
        __LogDuration("{0}.__ValidateWorldSectorCorners()".Inject(Name));
    }

    private Sector MakeSectorInstance(IntVector3 sectorIndex, Vector3 worldLocation, bool isOnPeriphery) {
        Sector sector = new Sector(worldLocation, isOnPeriphery);

        sector.Name = SectorNameFormat.Inject(sectorIndex);
        SectorData data = new SectorData(sector, sectorIndex) {
            //Density = 1F  the concept of space density is now attached to Topography, not Sectors. Density is relative and affects only drag
        };
        sector.Data = data;
        // IMPROVE use data values in place of sector values
        return sector;
    }

    public void CommenceSectorOperations() {
        Sectors.ForAll(s => {
            s.FinalInitialize();
        });
        Sectors.ForAll(s => {
            s.CommenceOperations();
        });
    }

    protected override void Cleanup() {
        References.SectorGrid = null;
        if (_gridWireframe != null) {
            _gridWireframe.Dispose();
        }
        foreach (var sector in Sectors) {
            sector.Dispose();
        }
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(s => s.Dispose());
        _subscriptions.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ISectorGrid Members

    IEnumerable<ISector> ISectorGrid.AllSectors { get { return Sectors.Cast<ISector>(); } }

    #endregion

}

