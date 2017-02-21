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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
/// Grid of Sectors. SectorIDs contain no 0 value. The sectorID to the left of the origin is -1, to the right +1.
/// </summary>
public class SectorGrid : AMonoSingleton<SectorGrid>, ISectorGrid {

    private const string SectorNameFormat = "Sector {0}";

    /// <summary>
    /// The offset values in grid coordinate space that, when added to a cell coordinate (0.5, 1.5, -2.0)
    /// gives all eight corner grid coordinate values for that cell.
    /// </summary>
    private static Vector3[] GridCellCornerOffsets = new Vector3[] {
                                                                    new Vector3(-0.5F, -0.5F, -0.5F),
                                                                    new Vector3(-0.5F, -0.5F, 0.5F),
                                                                    new Vector3(-0.5F, 0.5F, -0.5F),
                                                                    new Vector3(-0.5F, 0.5F, 0.5F),
                                                                    new Vector3(0.5F, -0.5F, -0.5F),
                                                                    new Vector3(0.5F, -0.5F, 0.5F),
                                                                    new Vector3(0.5F, 0.5F, -0.5F),
                                                                    new Vector3(0.5F, 0.5F, 0.5F),
                                                                };

    private string DebugName { get { return GetType().Name; } }

    [Tooltip("Controls how many layers of sectors are visible when in SectorViewMode. 0 = ShowAll.")]
    [SerializeField]
    private int _sectorVisibilityDepth = 3;

    /// <summary>
    /// The size of the grid of cells that encompasses the universe. Many of these cells are outside
    /// the radius of the universe and therefore do not result in the creation of a sector.
    /// <remarks>Always a cube of even integer values, aka 10x10x10, 4x4x4, etc.</remarks>
    /// </summary>
    [Tooltip("Size of the grid in sectors")]
    [SerializeField]    // Serialized to let the editor show the size while running
    private Vector3 _gridSize;

    public IEnumerable<Sector> Sectors { get { return _sectorIdToSectorLookup.Values; } }

    public IEnumerable<IntVector3> SectorIDs { get { return _sectorIdToSectorLookup.Keys; } }

    public IEnumerable<IntVector3> NonPeripheralSectorIDs { get { return _nonPeripheralSectorIDs; } }

    /// <summary>
    /// Read-only. The location of the center of all sectors in world space.
    /// </summary>
    public IEnumerable<Vector3> SectorCenters { get { return _sectorIdToCellWorldLocationLookup.Values; } }

    [Obsolete]
    public Sector RandomSector { get { return RandomExtended.Choice(_sectorIdToSectorLookup.Values); } }

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
    /// Lookup that translates a cell's GridCoordinates into the appropriate sectorID.
    /// Only contains cells inside the radius of the universe.
    /// <remarks>Cell GridCoordinates are always half values, aka Vector3(0.5F, 2.0F, -3.5F).
    /// They mark the center of each cell in the GridCoordinate system.</remarks>
    /// </summary>
    private IDictionary<Vector3, IntVector3> _cellGridCoordinatesToSectorIdLookup;

    /// <summary>
    /// Lookup that translates sector indices into the GridCoordinates of the cell/sector. 
    /// Only contains cells inside the radius of the universe.
    /// <remarks>Cell GridCoordinates are always half values, aka Vector3(0.5F, 2.0F, -3.5F).</remarks>
    /// </summary>
    private IDictionary<IntVector3, Vector3> _sectorIdToCellGridCoordinatesLookup;

    /// <summary>
    /// Lookup that translates sector indices into the world location of the cell/sector.
    /// Only contains cells inside the radius of the universe.
    /// </summary>
    private IDictionary<IntVector3, Vector3> _sectorIdToCellWorldLocationLookup;

    /// <summary>
    /// The sectorID to sector lookup. Only contains sectors inside the radius of the universe.
    /// </summary>
    private IDictionary<IntVector3, Sector> _sectorIdToSectorLookup;

    private IList<IntVector3> _nonPeripheralSectorIDs;

    /// <summary>
    /// The grid coordinates of the cell vertex furthest from the center along the positive axes of the grid.
    /// Its counterpart along the negative axes is the inverse.
    /// </summary>
    private Vector3 _outermostCellVertexGridCoordinates;

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
            MainCameraControl.Instance.sectorIDChanged += CameraSectorIDChangedEventHandler;
        }
        else {
            MainCameraControl.Instance.sectorIDChanged -= CameraSectorIDChangedEventHandler;
        }
    }

    public void BuildSectors() {
        InitializeGridSize();
        ConstructSectors();
        //__ValidateWorldSectorCorners();   // can take multiple seconds
    }

    private void InitializeGridSize() {
        int cellCountInsideUniverseAlongAGridAxis = CalcCellCountInsideUniverseAlongAGridAxis();
        D.Assert(cellCountInsideUniverseAlongAGridAxis % 2 == Constants.Zero, "CellCount can't be odd.");
        _gridSize = Vector3.one * cellCountInsideUniverseAlongAGridAxis;

        // grid must be a cube of even value
        D.AssertApproxEqual(_gridSize.x, _gridSize.y, _gridSize.ToString());
        D.AssertApproxEqual(_gridSize.y, _gridSize.z, _gridSize.ToString());
        D.AssertApproxEqual(Constants.ZeroF, _gridSize.x % 2F, _gridSize.ToString());

        _outermostCellVertexGridCoordinates = _gridSize / 2F;
        //D.Log("{0}: Universe Grid Size = {1}.", DebugName, _gridSize);
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

    private void CameraSectorIDChangedEventHandler(object sender, EventArgs e) {
        D.AssertEqual(PlayerViewMode.SectorView, PlayerViews.Instance.ViewMode);   // not subscribed unless in SectorViewMode
        D.Assert(IsGridWireframeShowing);
        HandleCameraSectorIDChanged();
    }

    private void HandleCameraSectorIDChanged() {
        IntVector3 cameraSectorID;
        bool isCameraInsideUniverse = MainCameraControl.Instance.TryGetSectorID(out cameraSectorID);
        //D.Log("{0}: CameraSectorID has changed to {1}. Generating new grid points.", DebugName, cameraSectorID);
        if (!isCameraInsideUniverse || _sectorVisibilityDepth == Constants.Zero) {
            // Camera has just moved outside the universe or its moved within the universe but we are supposed to show all 
            _gridWireframe.Points = GenerateAllWireframeGridPoints();
        }
        else {
            // Camera has just moved within the universe, or from outside in and we aren't supposed to show all
            _gridWireframe.Points = GenerateWireframeGridPoints(cameraSectorID);
        }
    }

    #endregion

    private List<Vector3> GenerateAllWireframeGridPoints() {
        // per GridFramework: grid needs to be at origin for rendering to align properly with the grid ANY TIME vectrosity points are generated
        D.Assert(Mathfx.Approx(transform.position, Vector3.zero, .01F), transform.position.ToString());

        // construct from and to values
        float xRenderFrom = -_outermostCellVertexGridCoordinates.x;
        float yRenderFrom = -_outermostCellVertexGridCoordinates.y;
        float zRenderFrom = -_outermostCellVertexGridCoordinates.z;

        float xRenderTo = _outermostCellVertexGridCoordinates.x;
        float yRenderTo = _outermostCellVertexGridCoordinates.y;
        float zRenderTo = _outermostCellVertexGridCoordinates.z;

        Vector3 renderFrom = new Vector3(xRenderFrom, yRenderFrom, zRenderFrom);
        Vector3 renderTo = new Vector3(xRenderTo, yRenderTo, zRenderTo);
        //D.Log("{0}: RenderFrom {1}, RenderTo {2}.", DebugName,  renderFrom, renderTo);

        // reqd as Version 2 GetVectrosityPoints() directly derives from Renderer's From and To
        _gridRenderer.From = renderFrom;
        _gridRenderer.To = renderTo;
        List<Vector3> gridPoints = _gridRenderer.GetVectrosityPoints();

        D.Assert(gridPoints.Any(), "No grid points to render.");
        return gridPoints;
    }

    private List<Vector3> GenerateWireframeGridPoints(IntVector3 cameraSectorID) {
        // per GridFramework: grid needs to be at origin for rendering to align properly with the grid ANY TIME vectrosity points are generated
        D.Assert(Mathfx.Approx(transform.position, Vector3.zero, .01F), transform.position.ToString());
        D.AssertNotEqual(Constants.Zero, _sectorVisibilityDepth);
        D.AssertNotDefault(cameraSectorID);
        Vector3 cameraCellVertexGridCoordinates = GetCellVertexGridCoordinatesFrom(cameraSectorID);

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
        //D.Log("{0} after depth adjust: CameraCellVertexGridCoordinates {1}, RenderFrom {2}, RenderTo {3}.", DebugName, cameraCellVertexGridCoordinates, renderFrom, renderTo);

        // reqd as Version 2 GetVectrosityPoints() directly derives from Renderer's From and To
        _gridRenderer.From = renderFrom;
        _gridRenderer.To = renderTo;
        List<Vector3> gridPoints = _gridRenderer.GetVectrosityPoints();

        D.Assert(gridPoints.Any(), "No grid points to render.");
        return gridPoints;
    }

    /// <summary>
    /// Confirms (and modifies if necessary) that there will be a minimum depth of one sector
    /// visible in the grid to render.
    /// </summary>
    /// <param name="from">The from value.</param>
    /// <param name="to">The to value.</param>
    private void ConfirmOneBoxDeep(ref float from, ref float to) {
        if (from.ApproxEquals(to)) {
            if (to >= Constants.ZeroF) {
                from = to - 1F;
            }
            else if (from <= Constants.ZeroF) {
                to = from + 1F;
            }
        }
    }

    private void ConstructSectors() {
        __RecordDurationStartTime();

        int cellQty = Mathf.RoundToInt(_gridSize.magnitude); // OPTIMIZE cell qty >> sector qty
        _cellVertexWorldLocations = new List<Vector3>(cellQty);
        _cellGridCoordinatesToSectorIdLookup = new Dictionary<Vector3, IntVector3>(cellQty, Vector3EqualityComparer.Default);
        _sectorIdToCellGridCoordinatesLookup = new Dictionary<IntVector3, Vector3>(cellQty);
        _sectorIdToCellWorldLocationLookup = new Dictionary<IntVector3, Vector3>(cellQty);
        _sectorIdToSectorLookup = new Dictionary<IntVector3, Sector>(cellQty);
        _nonPeripheralSectorIDs = new List<IntVector3>();

        int universeRadiusInSectors = _gameMgr.GameSettings.UniverseSize.RadiusInSectors();
        int universeRadiusInSectorsSqrd = universeRadiusInSectors * universeRadiusInSectors;

        int inspectedCellCount = Constants.Zero;
        int peripheryCellCount = Constants.Zero;

        // all axes have the same range of lowest to highest values so just pick x
        int highestCellVertexGridCoordinateAxisValue = Mathf.RoundToInt(_outermostCellVertexGridCoordinates.x); // float values are effectively ints
        int lowestCellVertexGridCoordinateAxisValue = -highestCellVertexGridCoordinateAxisValue;

        // x, y and z are their respective axis's cell vertex grid coordinate
        for (int x = lowestCellVertexGridCoordinateAxisValue; x < highestCellVertexGridCoordinateAxisValue; x++) {    // '<=' adds vertices on outer edge but no sectors
            for (int y = lowestCellVertexGridCoordinateAxisValue; y < highestCellVertexGridCoordinateAxisValue; y++) {
                for (int z = lowestCellVertexGridCoordinateAxisValue; z < highestCellVertexGridCoordinateAxisValue; z++) {
                    Vector3 cellVertexGridCoordinates = new Vector3(x, y, z);
                    Vector3 cellGridCoordinates = cellVertexGridCoordinates + _cellVertexGridCoordinatesToCellGridCoordinatesOffset; // (0.5, 1.5, 6.5)

                    inspectedCellCount++;
                    bool isCellOnPeriphery = false;
                    if (TryDetermineCellUseAsSector(cellGridCoordinates, universeRadiusInSectorsSqrd, out isCellOnPeriphery)) {
                        IntVector3 sectorID = CalculateSectorIDFromCellGridCoordindates(cellGridCoordinates);

                        Vector3 cellVertexWorldLocation = _grid.GridToWorld(cellVertexGridCoordinates);
                        _cellVertexWorldLocations.Add(cellVertexWorldLocation);
                        _cellGridCoordinatesToSectorIdLookup.Add(cellGridCoordinates, sectorID);
                        _sectorIdToCellGridCoordinatesLookup.Add(sectorID, cellGridCoordinates);

                        Vector3 cellWorldLocation = _grid.GridToWorld(cellGridCoordinates);
                        _sectorIdToCellWorldLocationLookup.Add(sectorID, cellWorldLocation);

                        if (isCellOnPeriphery) {
                            peripheryCellCount++;
                        }
                        else {
                            _nonPeripheralSectorIDs.Add(sectorID);
                        }
                        __AddSector(sectorID, cellWorldLocation, isCellOnPeriphery);
                    }
                    else {
                        //D.Log("Cell @ GridCoordinate {0} is completely outside Universe.", cellGridCoordinates);
                    }
                }
            }
        }

        int gridCellQty = Mathf.RoundToInt(_gridSize.x * _gridSize.y * _gridSize.z);
        if (inspectedCellCount != gridCellQty) {
            D.Error("{0}: inspected cell count {1} should equal {2} cells in grid.", DebugName, inspectedCellCount, gridCellQty);
        }
        D.Log("{0} inspected {1} grid cells, creating {2} sectors of which {3} are non-periphery.",
        DebugName, inspectedCellCount, _sectorIdToSectorLookup.Keys.Count, _sectorIdToSectorLookup.Keys.Count - peripheryCellCount);
        __LogDuration("{0}.ConstructSectors()".Inject(DebugName));
    }

    /// <summary>
    /// Returns <c>true</c> if the provided cell coordinates represents a valid cell that should be used as a sector, <c>false</c> otherwise.
    /// <remarks>A cell should be used as a sector if any part of it is inside the universe. The cell is a peripheral cell overlapping
    /// the edge of the universe if it should be used as a sector, AND some part of the cell is outside the universe.</remarks>
    /// </summary>
    /// <param name="cellGridCoordinates">The cell grid coordinates.</param>
    /// <param name="universeRadiusInSectorsSqrd">The universe radius in sectors SQRD.</param>
    /// <param name="isValidCellOnPeriphery">if <c>true</c> the valid cell has a portion of its volume outside the universe.</param>
    /// <returns></returns>
    private bool TryDetermineCellUseAsSector(Vector3 cellGridCoordinates, int universeRadiusInSectorsSqrd, out bool isValidCellOnPeriphery) {
        bool isCellValidSector = false;
        bool isACellCornerOutsideUniverse = false;
        float distanceInSectorsFromOriginToCellCornerSqrd;

        Vector3[] cellCorners = GetGridCellCorners(cellGridCoordinates);
        foreach (var corner in cellCorners) {
            distanceInSectorsFromOriginToCellCornerSqrd = corner.sqrMagnitude;
            // Avoid using == as this would put a corner right on the universe edge. If used as the only corner to 
            // qualify a sector as valid, no part of that sector would actually be inside the universe. If used as
            // the only corner that counts as 'outside', all of the sector would be inside and touching the edge
            // which doesn't need to be marked as peripheral.
            if (distanceInSectorsFromOriginToCellCornerSqrd < universeRadiusInSectorsSqrd) {
                isCellValidSector = true;
            }
            else if (distanceInSectorsFromOriginToCellCornerSqrd > universeRadiusInSectorsSqrd) {
                isACellCornerOutsideUniverse = true;
            }

            if (isCellValidSector && isACellCornerOutsideUniverse) {
                // no need to complete loop as I know what I need to return
                break;
            }
        }

        isValidCellOnPeriphery = isCellValidSector && isACellCornerOutsideUniverse;
        return isCellValidSector;
    }

    /// <summary>
    /// Calculates the sectorID from the provided cellGridCoordinates, aka the center of a cell in
    /// the GridCoordinate system.
    /// </summary>
    /// <param name="cellGridCoordinates">The cell's coordinates in the GridCoordindate System.</param>
    /// <returns></returns>
    private IntVector3 CalculateSectorIDFromCellGridCoordindates(Vector3 cellGridCoordinates) {
        // SectorIDs will contain no 0 value. The sectorID to the left of the origin is -1, to the right +1
        float x = cellGridCoordinates.x, y = cellGridCoordinates.y, z = cellGridCoordinates.z;
        int xID = x.ApproxEquals(Constants.ZeroF) ? 1 : (x > Constants.ZeroF ? Mathf.CeilToInt(x) : Mathf.FloorToInt(x));
        int yID = y.ApproxEquals(Constants.ZeroF) ? 1 : (y > Constants.ZeroF ? Mathf.CeilToInt(y) : Mathf.FloorToInt(y));
        int zID = z.ApproxEquals(Constants.ZeroF) ? 1 : (z > Constants.ZeroF ? Mathf.CeilToInt(z) : Mathf.FloorToInt(z));
        var sectorID = new IntVector3(xID, yID, zID);
        //D.Log("{0}: CellGridCoordinates = {1}, resulting SectorID = {2}.", DebugName, cellGridCoordinates, sectorID);
        return sectorID;
    }

    private void __AddSector(IntVector3 sectorID, Vector3 worldPosition, bool isOnPeriphery) {
        Sector sector = MakeSectorInstance(sectorID, worldPosition, isOnPeriphery);
        _sectorIdToSectorLookup.Add(sectorID, sector);
        //D.Log("Sector added at SectorID {0}.", sectorID);
    }

    /// <summary>
    /// Gets the half value (1.5, 2.5, 1.5) location (in the grid coordinate system)
    /// associated with this sectorID. This will be the center of the cell in the
    /// GridCoordinate system.
    /// </summary>
    /// <param name="sectorID">The sectorID.</param>
    /// <returns></returns>
    private Vector3 GetCellGridCoordinatesFor(IntVector3 sectorID) {
        Vector3 cellGridCoordinates;
        if (!_sectorIdToCellGridCoordinatesLookup.TryGetValue(sectorID, out cellGridCoordinates)) {
            cellGridCoordinates = CalculateCellGridCoordinatesFrom(sectorID);
            _sectorIdToCellGridCoordinatesLookup.Add(sectorID, cellGridCoordinates);
            _cellGridCoordinatesToSectorIdLookup.Add(cellGridCoordinates, sectorID);
        }
        return cellGridCoordinates;
    }

    private Vector3 CalculateCellGridCoordinatesFrom(IntVector3 sectorID) {
        int xID = sectorID.x, yID = sectorID.y, zID = sectorID.z;
        D.AssertNotDefault(sectorID);
        float x = xID > Constants.Zero ? xID - 1F : xID;
        float y = yID > Constants.Zero ? yID - 1F : yID;
        float z = zID > Constants.Zero ? zID - 1F : zID;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Gets the round number (1.0, 1.0, 2.0) location (in the grid coordinate system)
    /// associated with this sectorID. This will be the left, lower, back corner of the cell.
    /// </summary>
    /// <param name="sectorID">The sectorID.</param>
    /// <returns></returns>
    private Vector3 GetCellVertexGridCoordinatesFrom(IntVector3 sectorID) {
        D.AssertNotDefault(sectorID);
        return GetCellGridCoordinatesFor(sectorID) - _cellVertexGridCoordinatesToCellGridCoordinatesOffset;
    }

    /// <summary>
    /// Returns the SectorID that contains the provided world location.
    /// Throws an error if <c>worldLocation</c> is not within the universe.
    /// <remarks>Provided as a convenience when the client knows the location provided is inside the universe.
    /// If this is not certain, use TryGetSectorIDThatContains(worldLocation) instead.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <returns></returns>
    public IntVector3 GetSectorIDThatContains(Vector3 worldLocation) {
        IntVector3 sectorID;
        Vector3 nearestCellGridCoordinates = _grid.NearestCell(worldLocation, RectGrid.CoordinateSystem.Grid);
        bool isSectorIDPresent = _cellGridCoordinatesToSectorIdLookup.TryGetValue(nearestCellGridCoordinates, out sectorID);
        D.Assert(isSectorIDPresent, "{0}: No SectorID stored at CellGridCoordinates {1}.".Inject(DebugName, nearestCellGridCoordinates));
        return sectorID;
    }

    /// <summary>
    /// Gets the world space position of the sector indicated by the provided sectorID.
    /// Throws an error if no sector is present at that sectorID.
    /// </summary>
    /// <param name="sectorID">ID of the sector.</param>
    /// <returns></returns>
    public Vector3 GetSectorPosition(IntVector3 sectorID) {
        D.AssertNotDefault(sectorID);
        Vector3 worldPosition;
        bool isSectorFound = _sectorIdToCellWorldLocationLookup.TryGetValue(sectorID, out worldPosition);
        D.Assert(isSectorFound, sectorID.ToString());
        return worldPosition;
    }

    /// <summary>
    /// Returns <c>true</c> if a sectorID has been assigned containing this worldLocation, <c>false</c> otherwise.
    /// <remarks>Locations inside the universe have assigned SectorIDs, while those outside do not.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <param name="sectorID">The resulting sectorID.</param>
    /// <returns></returns>
    public bool TryGetSectorIDThatContains(Vector3 worldLocation, out IntVector3 sectorID) {
        Vector3 nearestCellGridCoordinates = _grid.NearestCell(worldLocation, RectGrid.CoordinateSystem.Grid);
        return _cellGridCoordinatesToSectorIdLookup.TryGetValue(nearestCellGridCoordinates, out sectorID);
    }

    /// <summary>
    /// Gets the sector associated with this sectorID. 
    /// </summary>
    /// <param name="sectorID">The sectorID.</param>
    /// <returns></returns>
    public Sector GetSector(IntVector3 sectorID) {
        D.AssertNotDefault(sectorID);
        Sector sector;
        if (!_sectorIdToSectorLookup.TryGetValue(sectorID, out sector)) {
            D.Error("{0}: No Sector at {1}.", DebugName, sectorID);
        }
        return sector;
    }

    /// <summary>
    /// Returns <c>true</c> if a sector has been assigned containing this worldLocation, <c>false</c> otherwise.
    /// <remarks>Locations inside the universe have assigned Sectors, while those outside do not.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <param name="sectorID">The sector.</param>
    /// <returns></returns>
    public bool TryGetSectorContaining(Vector3 worldLocation, out Sector sector) {
        IntVector3 sectorID;
        if (TryGetSectorIDThatContains(worldLocation, out sectorID)) {
            sector = GetSector(sectorID);
            return true;
        }
        sector = null;
        return false;
    }

    /// <summary>
    /// Gets the nearest SectorID to the provided <c>worldLocation</c>.
    /// <remarks>Expensive.</remarks>
    /// <remarks>2.11.17 Not currently used.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <returns></returns>
    public IntVector3 GetNearestSectorIDTo(Vector3 worldLocation) {
        Vector3 nearestCellToWorldLocation = _grid.NearestCell(worldLocation, RectGrid.CoordinateSystem.Grid);
        Vector3 nearestUCell = default(Vector3);    // the nearest cell grid coordinates within the universe defined by universeRadius
        float smallestSqrDistanceToUCell = float.MaxValue;

        // all the cell grid coordinates located within the universe defined by universeRadius
        var allUniverseCells = _cellGridCoordinatesToSectorIdLookup.Keys;
        float sqrDistance;
        foreach (var uCell in allUniverseCells) {
            if ((sqrDistance = Vector3.SqrMagnitude(nearestCellToWorldLocation - uCell)) < smallestSqrDistanceToUCell) {
                nearestUCell = uCell;
                smallestSqrDistanceToUCell = sqrDistance;
            }
        }
        D.Assert(nearestUCell != default(Vector3));
        return _cellGridCoordinatesToSectorIdLookup[nearestUCell];
    }

    /// <summary>
    /// Gets the sector containing the provided worldLocation.
    /// Throws an error if <c>worldLocation</c> is not within the universe.
    /// <remarks>Provided as a convenience when the client knows the location provided is inside the universe.
    /// If this is not certain, use TryGetSectorContaining(worldLocation) instead.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world point.</param>
    /// <returns></returns>
    public Sector GetSectorContaining(Vector3 worldLocation) {
        var sectorID = GetSectorIDThatContains(worldLocation);
        return GetSector(sectorID);
    }

    /// <summary>
    /// Tries to get a random sectorID, returning <c>true</c> if successful.
    /// </summary>
    /// <param name="sectorID">Resulting sectorID.</param>
    /// <param name="includePeriphery">if set to <c>true</c> [include periphery].</param>
    /// <param name="excludedIDs">The excluded sectorIDs.</param>
    /// <returns></returns>
    public bool TryGetRandomSectorID(out IntVector3 sectorID, bool includePeriphery = false, IEnumerable<IntVector3> excludedIDs = null) {
        IEnumerable<IntVector3> idsToChooseFrom = includePeriphery ? _sectorIdToSectorLookup.Keys : _nonPeripheralSectorIDs;
        if (excludedIDs != null) {
            idsToChooseFrom = idsToChooseFrom.Except(excludedIDs);
        }
        if (idsToChooseFrom.Any()) {
            sectorID = RandomExtended.Choice(idsToChooseFrom);
            return true;
        }
        sectorID = default(IntVector3);
        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if this sectorID is a peripheral sector in the universe.
    /// Throws an error if not found within the universe.
    /// </summary>
    /// <param name="sectorID">The sector identifier.</param>
    /// <returns></returns>
    public bool IsSectorOnPeriphery(IntVector3 sectorID) {
        if (_nonPeripheralSectorIDs.Contains(sectorID)) {
            return false;
        }
        D.Assert(_sectorIdToSectorLookup.ContainsKey(sectorID), "SectorID {0} is not within Universe.".Inject(sectorID));
        return true;
    }

    /// <summary>
    /// Returns the sectorIDs surrounding <c>centerID</c> that are between <c>minDistance</c> and <c>maxDistance</c>
    /// from <c>centerID</c>. The UOM for both distance values is sectors.
    /// <remarks>All SectorIDs returned will be within the universe.</remarks>
    /// </summary>
    /// <param name="centerID">The center.</param>
    /// <param name="minDistance">The minimum distance in sectors.</param>
    /// <param name="maxDistance">The maximum distance in sectors.</param>
    /// <param name="includePeriphery">if set to <c>true</c> [include peripheral sectors].</param>
    /// <returns></returns>
    public IList<IntVector3> GetSurroundingSectorIDsBetween(IntVector3 centerID, int minDistance, int maxDistance, bool includePeriphery = true) {
        D.AssertNotDefault(centerID);
        D.Assert(_sectorIdToSectorLookup.ContainsKey(centerID));
        Utility.ValidateForRange(minDistance, 0, maxDistance - 1);
        Utility.ValidateForRange(maxDistance, minDistance + 1, _gameMgr.GameSettings.UniverseSize.RadiusInSectors() * 2);

        __RecordDurationStartTime();

        int minDistanceSqrd = minDistance * minDistance;
        int maxDistanceSqrd = maxDistance * maxDistance;

        Vector3 centerCell = _sectorIdToCellGridCoordinatesLookup[centerID];

        Vector3[] allCandidateGridCells = _cellGridCoordinatesToSectorIdLookup.Keys.Except(centerCell).ToArray();
        int gridCellQty = allCandidateGridCells.Length;
        IList<IntVector3> resultingSectorIDs = new List<IntVector3>(gridCellQty);

        for (int i = 0; i < gridCellQty; i++) {
            Vector3 candidateCell = allCandidateGridCells[i];
            Vector3 vectorFromCenterToCandidateCell = candidateCell - centerCell;
            float vectorSqrMagnitude = vectorFromCenterToCandidateCell.sqrMagnitude;
            if (vectorSqrMagnitude >= minDistanceSqrd && vectorSqrMagnitude <= maxDistanceSqrd) {
                IntVector3 candidateSectorID = _cellGridCoordinatesToSectorIdLookup[candidateCell];
                bool toAdd = includePeriphery || !IsSectorOnPeriphery(candidateSectorID);
                if (toAdd) {
                    resultingSectorIDs.Add(candidateSectorID);
                }
            }
        }

        if ((Utility.SystemTime - __durationStartTime).TotalSeconds > 0.1F) {
            __LogDuration("{0}.GetSurroundingSectorIdsBetween()".Inject(DebugName));
        }
        return resultingSectorIDs;
    }

    /// <summary>
    /// Gets the IDs of the neighbors to the sector indicated by centerID.
    /// The sector at centerID can be a peripheral sector.
    /// The ID of the sector at centerID is not included.
    /// </summary>
    /// <param name="centerID">The center.</param>
    /// <param name="includePeriphery">if set to <c>true</c> [include peripheral sectors].</param>
    /// <returns></returns>
    public IList<IntVector3> GetNeighboringSectorIDs(IntVector3 centerID, bool includePeriphery = true) {
        IList<IntVector3> neighbors = new List<IntVector3>();
        int[] xValuePair = CalcNeighborPair(centerID.x);
        int[] yValuePair = CalcNeighborPair(centerID.y);
        int[] zValuePair = CalcNeighborPair(centerID.z);
        foreach (var x in xValuePair) {
            foreach (var y in yValuePair) {
                foreach (var z in zValuePair) {
                    IntVector3 sectorID = new IntVector3(x, y, z);
                    if (_sectorIdToSectorLookup.ContainsKey(sectorID)) {
                        bool toAdd = includePeriphery || !IsSectorOnPeriphery(sectorID);
                        if (toAdd) {
                            neighbors.Add(sectorID);
                        }
                    }
                }
            }
        }
        return neighbors;
    }

    private int[] CalcNeighborPair(int center) {
        int[] valuePair = new int[2];
        // no 0 value in my sectorIDs
        valuePair[0] = center - 1 == 0 ? center - 2 : center - 1;
        valuePair[1] = center + 1 == 0 ? center + 2 : center + 1;
        return valuePair;
    }

    /// <summary>
    /// Gets the neighboring sectors to the sector at centerID.
    /// The sector at centerID can be on the periphery.
    /// The sector at centerID is not included.
    /// </summary>
    /// <param name="centerID">The centerID.</param>
    /// <param name="includePeriphery">if set to <c>true</c> [include peripheral sectors].</param>
    /// <returns></returns>
    public IEnumerable<ISector_Ltd> GetNeighboringSectors(IntVector3 centerID, bool includePeriphery = true) {
        IList<ISector_Ltd> neighborSectors = new List<ISector_Ltd>();
        foreach (var sectorID in GetNeighboringSectorIDs(centerID, includePeriphery)) {
            neighborSectors.Add(GetSector(sectorID));
        }
        return neighborSectors;
    }

    /// <summary>
    /// Gets the distance in sectors between the center of the sectors located at these two sectorIDs.
    /// Example: the distance between (1, 1, 1) and (1, 1, 2) is 1.0. The distance between 
    /// (1, 1, 1) and (1, 2, 2) is 1.414, and the distance between (-1, 1, 1) and (1, 1, 1) is 1.0
    /// as sectorIDs have no 0 value.
    /// </summary>
    /// <param name="firstSectorID">The first.</param>
    /// <param name="secondSectorID">The second.</param>
    /// <returns></returns>
    public float GetDistanceInSectors(IntVector3 firstSectorID, IntVector3 secondSectorID) {
        D.AssertNotDefault(firstSectorID);
        D.AssertNotDefault(secondSectorID);
        Vector3 firstCellGridCoordinates = GetCellGridCoordinatesFor(firstSectorID);
        Vector3 secondCellGridCoordindates = GetCellGridCoordinatesFor(secondSectorID);
        return Vector3.Distance(firstCellGridCoordinates, secondCellGridCoordindates);
    }

    /// <summary>
    /// Gets the distance in sectors from the origin, aka the location of the UniverseCenter.
    /// </summary>
    /// <param name="sectorID">ID of the sector.</param>
    /// <returns></returns>
    public float GetDistanceInSectorsFromOrigin(IntVector3 sectorID) {
        Vector3 cellGridCoordinates = GetCellGridCoordinatesFor(sectorID);
        return Vector3.Distance(cellGridCoordinates, GameConstants.UniverseOrigin);
    }

    private void ShowSectorGrid(bool toShow) {
        if (IsGridWireframeShowing == toShow) {
            return;
        }
        if (toShow) {
            D.Assert(!IsGridWireframeShowing);
            IntVector3 cameraSectorID;
            bool isCameraInsideUniverse = MainCameraControl.Instance.TryGetSectorID(out cameraSectorID);

            List<Vector3> gridPoints;
            if (!isCameraInsideUniverse || _sectorVisibilityDepth == Constants.Zero) {
                // Camera is outside the universe or we are supposed to show all 
                gridPoints = GenerateAllWireframeGridPoints();
            }
            else {
                // Camera is inside the universe, or we aren't supposed to show all
                gridPoints = GenerateWireframeGridPoints(cameraSectorID);
            }
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
        //D.Log("{0} is {1}.", DebugName, msg);
    }

    /// <summary>
    /// Returns the corners of the cell in Grid Coordinate space whose center is at cellGridCoordinates.
    /// </summary>
    /// <param name="cellGridCoordinates">The cell grid coordinates, aka (0.5, 1.0, -2).</param>
    /// <returns></returns>
    private Vector3[] GetGridCellCorners(Vector3 cellGridCoordinates) {
        Vector3[] gridCellCorners = new Vector3[8];
        for (int i = 0; i < 8; i++) {
            gridCellCorners[i] = cellGridCoordinates + GridCellCornerOffsets[i];
        }
        return gridCellCorners;
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
                if (Mathfx.Approx(iCorner, jCorner, 10F)) {
                    D.Error("Duplicate Corners: {0} & {1}.", iCorner.ToPreciseString(), jCorner.ToPreciseString());
                }
            }
        }
        D.Log("{0} validated {1} sector corners.", DebugName, SectorCorners.Count);
        __LogDuration("{0}.__ValidateWorldSectorCorners()".Inject(DebugName));
    }

    private Sector MakeSectorInstance(IntVector3 sectorID, Vector3 worldLocation, bool isOnPeriphery) {
        Sector sector = new Sector(worldLocation, isOnPeriphery);

        sector.Name = SectorNameFormat.Inject(sectorID);
        SectorData data = new SectorData(sector, sectorID) {
            //Density = 1F  the concept of space density is now attached to Topography, not Sectors. Density is relative and affects only drag
        };
        sector.Data = data;
        // IMPROVE use data values in place of sector values
        return sector;
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
    [Obsolete]
    private bool TryDetermineCellVertexInsideUniverse(Vector3 cellVertexWorldLoc, float universeRadiusSqrd, float onPeripheryRadiusSqrd, out bool isCellOnPeriphery) {
        isCellOnPeriphery = false;
        bool isCellVertexInsideUniverse = false;
        float distanceFromUniverseCenterToCellVertexSqrd = Vector3.SqrMagnitude(cellVertexWorldLoc);

        // Note: use of '<' excludes negative vertices on the edge of the universe. 
        // Positive vertices on the edge are already excluded prior to this method as the cell, by definition would be outside the universe

        // Negative cells with its vertex outside can still have part of the cell inside which means a lot of the universe on
        // the periphery has no assigned cell,  aka Sector
        if (distanceFromUniverseCenterToCellVertexSqrd <= universeRadiusSqrd) {
            isCellOnPeriphery = distanceFromUniverseCenterToCellVertexSqrd > onPeripheryRadiusSqrd;
            isCellVertexInsideUniverse = true;
        }
        return isCellVertexInsideUniverse;
    }

    [Obsolete]
    public IntVector3 GetSectorIdThatContains(Vector3 worldLocation) {
        IntVector3 sectorID;
        Vector3 nearestCellGridCoordinates = _grid.NearestCell(worldLocation, RectGrid.CoordinateSystem.Grid);
        if (!_cellGridCoordinatesToSectorIdLookup.TryGetValue(nearestCellGridCoordinates, out sectorID)) {
            //// 8.9.16 Should never occur unless debugMaxGridSize is used as all worldLocations inside Universe should be 
            //// encompassed by a cell. Currently using debugMaxGridSize always results in Camera initial location warning
            ////D.Warn(!_enableGridSizeLimit, "No Index stored at CellGridCoordinates {0}. Adding.", nearestCellGridCoordinates);
            Vector3 peripheralCellGridCoordinates = _outermostCellVertexGridCoordinates + _cellVertexGridCoordinatesToCellGridCoordinatesOffset;
            D.Warn("{0}: No SectorID stored at CellGridCoordinates {1}. Adding. Peripheral CellGridCoordinates = {2}",
                DebugName, nearestCellGridCoordinates, peripheralCellGridCoordinates);
            sectorID = CalculateSectorIDFromCellGridCoordindates(nearestCellGridCoordinates);
            _cellGridCoordinatesToSectorIdLookup.Add(nearestCellGridCoordinates, sectorID);
            _sectorIdToCellGridCoordinatesLookup.Add(sectorID, nearestCellGridCoordinates);
        }
        return sectorID;
    }

    /// <summary>
    /// Returns <c>true</c> if the sector's world position was acquired, <c>false</c> otherwise.
    //// Warning: While debugging, only a limited number of sectors are 'built' to
    //// reduce the time needed to construct valid paths for pathfinding.
    /// </summary>
    /// <param name="sectorID">ID of the sector.</param>
    /// <param name="worldPosition">The world position of the sector's center.</param>
    /// <returns></returns>
    [Obsolete]
    public bool __TryGetSectorPosition(IntVector3 sectorID, out Vector3 worldPosition) {
        D.AssertNotDefault(sectorID);
        bool isSectorFound = _sectorIdToCellWorldLocationLookup.TryGetValue(sectorID, out worldPosition);
        if (!isSectorFound) {
            D.Warn("{0} could not find a sector at sectorID {1}.", DebugName, sectorID);
        }
        return isSectorFound;
    }

    /// <summary>
    /// Gets a random sectorID.
    /// </summary>
    /// <param name="includePeriphery">if set to <c>true</c> [include periphery].</param>
    /// <param name="excludedIds">The excluded sectorIDs.</param>
    /// <returns></returns>
    [Obsolete]
    public IntVector3 GetRandomSectorID(bool includePeriphery = false, IEnumerable<IntVector3> excludedIds = null) {
        IEnumerable<IntVector3> sectorIdsToChooseFrom = includePeriphery ? _sectorIdToSectorLookup.Keys : _nonPeripheralSectorIDs;
        if (excludedIds != null) {
            sectorIdsToChooseFrom = sectorIdsToChooseFrom.Except(excludedIds);
        }
        return RandomExtended.Choice(sectorIdsToChooseFrom);
    }

    /// <summary>
    /// Returns <c>true</c> if the sector was acquired, <c>false</c> otherwise.
    //// Warning: While debugging, only a limited number of sectors are 'built' to
    //// reduce the time needed to construct valid paths for pathfinding.
    /// </summary>
    /// <param name="sectorID">The sectorID.</param>
    /// <param name="sector">The sector.</param>
    /// <returns></returns>
    [Obsolete]
    public bool __TryGetSector(IntVector3 sectorID, out Sector sector) {
        D.AssertNotDefault(sectorID);
        return _sectorIdToSectorLookup.TryGetValue(sectorID, out sector);
    }

    /// <summary>
    /// Returns <c>true</c> if a sector is present at this sectorID, <c>false</c> otherwise.
    //// Warning: While debugging, only a limited number of sectors are 'built' to
    //// reduce the time needed to construct valid paths for pathfinding.
    /// <remarks>DebugInfo uses this to properly construct CameraLocation text as the
    /// camera can move to the edge of the universe even if sectors are not present.</remarks>
    /// </summary>
    /// <param name="sectorID">ID of the sector.</param>
    /// <returns></returns>
    [Obsolete]
    public bool __IsSectorPresentAt(IntVector3 sectorID) {
        return _sectorIdToSectorLookup.ContainsKey(sectorID);
    }

    [Obsolete]
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
        if (MainCameraControl.Instance != null) {
            MainCameraControl.Instance.sectorIDChanged -= CameraSectorIDChangedEventHandler;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Archive

    // Dynamic subscription approach using Property Changed
    //private void DynamicallySubscribe(bool toSubscribe) {
    //    if (toSubscribe) {
    //        _subscriptions.Add(MainCameraControl.Instance.SubscribeToPropertyChanged<MainCameraControl, IntVector3>(cc => cc.SectorID, CameraSectorIdPropChangedHandler));
    //    }
    //    else {
    //        IDisposable d = _subscriptions.Single(s => s as DisposePropertyChangedSubscription<MainCameraControl> != null);
    //        _subscriptions.Remove(d);
    //        d.Dispose();
    //    }
    //}

    #endregion

    #region ISectorGrid Members

    IEnumerable<ISector> ISectorGrid.AllSectors { get { return Sectors.Cast<ISector>(); } }

    #endregion

}

