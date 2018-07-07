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
using MoreLinq;
using UnityEngine;

/// <summary>
/// Grid of Sectors. SectorIDs contain no 0 value. The sectorID to the left of the origin is -1, to the right +1.
/// </summary>
public class SectorGrid : AMonoSingleton<SectorGrid>, ISectorGrid {

    private const string SectorNameFormat = "Sector {0}";

    /// <summary>
    /// The offset that converts cellVertex grid coordinates into cell grid coordinates.
    /// </summary>
    private static Vector3 CellVertexGridCoordinatesToCellGridCoordinatesOffset = new Vector3(0.5F, 0.5F, 0.5F);

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

    [Tooltip("The minimum number of vertices reqd inside the Universe for a cell to be considered for a RimSector.")]
    [SerializeField]
    private int _rimCellVertexThreshold = 2;

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
    private Vector3 _gridSize;  // 7.4.18 Small 8x8x8, URadius 4800, UVolume ~ 4.6e12

    /// <summary>
    /// Returns all CoreSectors and RimSectors.
    /// <remarks>CoreSectors are 100% contained within the UniverseRadius. RimSectors have some percentage
    /// of their volume protruding outside the universe.</remarks>
    /// </summary>
    public IEnumerable<ASector> Sectors { get { return _sectorIdToSectorLookup.Values; } }

    /// <summary>
    /// Returns the sectorIDs of all sectors inside the universe
    /// </summary>
    public IEnumerable<IntVector3> SectorIDs { get { return _sectorIdToSectorLookup.Keys; } }

    private IList<IntVector3> _coreSectorIDs;
    public IEnumerable<IntVector3> CoreSectorIDs { get { return _coreSectorIDs; } }

    private IEnumerable<Sector> _coreSectors;
    public IEnumerable<Sector> CoreSectors {
        get {
            _coreSectors = _coreSectors ?? _sectorIdToSectorLookup.Values.Where(sector => sector is Sector).Cast<Sector>();
            return _coreSectors;
        }
    }

    /// <summary>
    /// Read-only. The location of the center of all sectors in world space.
    /// <remarks>For RimSectors, the location returned may not be the equivalent of RimSector.Position.</remarks>
    /// </summary>
    public IEnumerable<Vector3> SectorCenters { get { return _sectorIdToCellWorldLocationLookup.Values; } }

    /// <summary>
    ///  Read-only. The location of the corners of all sectors in world space.
    /// </summary>
    [Obsolete]
    private IList<Vector3> SectorCorners { get { return _cellVertexWorldLocations; } }

    private bool IsGridWireframeShowing { get { return _gridWireframe != null && _gridWireframe.IsShowing; } }

    /// <summary>
    /// The world location of each cell vertex. Only contains cells inside the radius of the universe.
    /// <remarks>A cell vertex is effectively the cell index in the grid coordinate system, aka Vector3(1, -1, 0).</remarks>
    /// </summary>
    private IList<Vector3> _cellVertexWorldLocations;   // OPTIMIZE not really used except for validation

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
    private IDictionary<IntVector3, ASector> _sectorIdToSectorLookup;

    /// <summary>
    /// The grid coordinates of the cell vertex furthest from the center along the positive axes of the grid.
    /// Its counterpart along the negative axes is the inverse.
    /// </summary>
    private Vector3 _outermostCellVertexGridCoordinates;
    private RectGrid _grid;
    private Parallelepiped _gridRenderer;
    private GridWireframe _gridWireframe;
    private IList<IDisposable> _subscriptions;
    private MainCameraControl _mainCameraCntl;
    private PlayerViews _playerViews;
    private GameManager _gameMgr;

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        GameReferences.SectorGrid = Instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeValuesAndReferences();
        InitializeGrid();
        Subscribe();
    }

    private void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        _mainCameraCntl = MainCameraControl.Instance;
        _playerViews = PlayerViews.Instance;
        _cellVertexWorldLocations = new List<Vector3>();
        _cellGridCoordinatesToSectorIdLookup = new Dictionary<Vector3, IntVector3>(Vector3EqualityComparer.Default);
        _sectorIdToCellGridCoordinatesLookup = new Dictionary<IntVector3, Vector3>();
        _sectorIdToCellWorldLocationLookup = new Dictionary<IntVector3, Vector3>();
        _sectorIdToSectorLookup = new Dictionary<IntVector3, ASector>();
        _coreSectorIDs = new List<IntVector3>();
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
        _mainCameraCntl.sectorIDChanged += CameraSectorIDChangedEventHandler;
    }

    // 8.16.16 Control of GameState progression when sectors are built now handled by UniverseBuilder

    public void BuildSectors() {
        InitializeGridSize();
        ConstructSectors();
        //__ValidateWorldSectorCorners();   // can take multiple seconds
    }

    private void InitializeGridSize() {
        _gridSize = Vector3.one * _gameMgr.GameSettings.UniverseSize.RadiusInSectors() * 2;
        // by definition, grid is a cube of even value
        _outermostCellVertexGridCoordinates = _gridSize / 2F;
    }

    #region Event and Property Change Handlers

    private void PlayerViewModePropChangedHandler() {
        PlayerViewMode viewMode = _playerViews.ViewMode;
        switch (viewMode) {
            case PlayerViewMode.SectorView:
                ShowSectorGrid(true);
                break;
            case PlayerViewMode.NormalView:
                ShowSectorGrid(false);
                break;
            case PlayerViewMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(viewMode));
        }
    }

    private void CameraSectorIDChangedEventHandler(object sender, EventArgs e) {
        if (_playerViews.ViewMode == PlayerViewMode.SectorView) {
            D.Assert(IsGridWireframeShowing);
            HandleCameraSectorIDChanged();
        }
    }

    #endregion

    private void HandleCameraSectorIDChanged() {
        IntVector3 cameraSectorID;
        bool isCameraInsideUniverse = _mainCameraCntl.TryGetSectorID(out cameraSectorID);
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

    #region Show SectorView Grid

    private void ShowSectorGrid(bool toShow) {
        if (IsGridWireframeShowing == toShow) {
            return;
        }
        if (toShow) {
            D.Assert(!IsGridWireframeShowing);
            IntVector3 cameraSectorID;
            bool isCameraInsideUniverse = _mainCameraCntl.TryGetSectorID(out cameraSectorID);

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

    /// <summary>
    /// Gets the round number (1.0, 1.0, 2.0) location (in the grid coordinate system)
    /// associated with this sectorID. This will be the left, lower, back corner of the cell.
    /// </summary>
    /// <param name="sectorID">The sectorID.</param>
    /// <returns></returns>
    private Vector3 GetCellVertexGridCoordinatesFrom(IntVector3 sectorID) {
        D.AssertNotDefault(sectorID);
        return GetCellGridCoordinatesFor(sectorID) - CellVertexGridCoordinatesToCellGridCoordinatesOffset;
    }

    #endregion

    #region Construct Sectors

    private void ConstructSectors() {
        __RecordDurationStartTime();

        float universeRadius = _gameMgr.GameSettings.UniverseSize.Radius();
        D.AssertNotEqual(Constants.ZeroF, universeRadius);
        __universeRadiusSqrd = universeRadius * universeRadius;

        double universeVol = Constants.FourThirds * Mathf.PI * Mathf.Pow(universeRadius, 3F);
        double volOfACoreSector = Mathf.Pow(TempGameValues.SectorSideLength, 3F);
        double totalCoreSectorVol = 0D;
        double estTotalRimSectorVol = 0D;

        int inspectedCellCount = Constants.Zero;
        int rimCount = Constants.Zero;
        int failedRimCount = Constants.Zero;
        int drossCount = Constants.Zero;
        int outCount = Constants.Zero;

        // all axes have the same range of lowest to highest values so just pick x
        int highestCellVertexGridCoordinateAxisValue = Mathf.RoundToInt(_outermostCellVertexGridCoordinates.x); // float values are effectively ints
        int lowestCellVertexGridCoordinateAxisValue = -highestCellVertexGridCoordinateAxisValue;

        // x, y and z are their respective axis's cell vertex grid coordinate
        for (int x = lowestCellVertexGridCoordinateAxisValue; x < highestCellVertexGridCoordinateAxisValue; x++) {    // '<=' adds vertices on outer edge but no sectors
            for (int y = lowestCellVertexGridCoordinateAxisValue; y < highestCellVertexGridCoordinateAxisValue; y++) {
                for (int z = lowestCellVertexGridCoordinateAxisValue; z < highestCellVertexGridCoordinateAxisValue; z++) {
                    Vector3 cellVertexGridCoordinates = new Vector3(x, y, z);
                    Vector3 cellGridCoordinates = cellVertexGridCoordinates + CellVertexGridCoordinatesToCellGridCoordinatesOffset; // (0.5, 1.5, 6.5)

                    inspectedCellCount++;
                    UniverseCellCategory cellCategory;
                    if (TryDetermineGridCellUseAsASector(cellGridCoordinates, __universeRadiusSqrd, out cellCategory)) {
                        Vector3 cellWorldLocation = _grid.GridToWorld(cellGridCoordinates);
                        if (cellCategory == UniverseCellCategory.Core) {
                            IntVector3 sectorID = CalculateSectorIDFromCellGridCoordindates(cellGridCoordinates);

                            Vector3 cellVertexWorldLocation = _grid.GridToWorld(cellVertexGridCoordinates);
                            _cellVertexWorldLocations.Add(cellVertexWorldLocation);
                            _cellGridCoordinatesToSectorIdLookup.Add(cellGridCoordinates, sectorID);
                            _sectorIdToCellGridCoordinatesLookup.Add(sectorID, cellGridCoordinates);

                            AddCoreSector(sectorID, cellWorldLocation);
                            totalCoreSectorVol += volOfACoreSector;
                        }
                        else {
                            D.AssertEqual(UniverseCellCategory.Rim, cellCategory);
                            Vector3 acceptablePositionPropValue;
                            float acceptableRadiusPropValue;
                            if (RimSector.TryFindAcceptablePosition(cellWorldLocation, __universeRadiusSqrd, out acceptablePositionPropValue, out acceptableRadiusPropValue)) {
                                IntVector3 sectorID = CalculateSectorIDFromCellGridCoordindates(cellGridCoordinates);

                                Vector3 cellVertexWorldLocation = _grid.GridToWorld(cellVertexGridCoordinates);
                                _cellVertexWorldLocations.Add(cellVertexWorldLocation);
                                _cellGridCoordinatesToSectorIdLookup.Add(cellGridCoordinates, sectorID);
                                _sectorIdToCellGridCoordinatesLookup.Add(sectorID, cellGridCoordinates);

                                AddRimSector(cellWorldLocation, acceptablePositionPropValue, acceptableRadiusPropValue, sectorID);
                                rimCount++;
                                // 7.4.18 This estimate is low. Vol of cube with faceDistance = 1 is 1.909 x vol of sphere with radius 1. 
                                // For Small Universe, 70+% of RimSectors get all of that additional volume from the corners, 20% get 2/3rds 
                                // of the additional volume and 10% get 1/3rd of that additional volume. However, mix varies significantly 
                                // depending on _rimCellVertexThreshold and size of universe.
                                estTotalRimSectorVol += Constants.FourThirds * Mathf.PI * Mathf.Pow(acceptableRadiusPropValue, 3F);
                            }
                            else {
                                D.Warn("{0} could not find acceptable position for RimSector centered at {1}.", DebugName, cellWorldLocation);
                                cellCategory = UniverseCellCategory.FailedRim;
                            }
                        }
                    }

                    if (cellCategory == UniverseCellCategory.FailedRim) {
                        failedRimCount++;
                    }
                    if (cellCategory == UniverseCellCategory.Dross) {
                        drossCount++;
                    }
                    if (cellCategory == UniverseCellCategory.Out) {
                        outCount++;
                    }
                }
            }
        }

        int gridCellQty = Mathf.RoundToInt(_gridSize.x * _gridSize.y * _gridSize.z);
        if (inspectedCellCount != gridCellQty) {
            D.Error("{0}: inspected cell count {1} should equal {2} cells in grid.", DebugName, inspectedCellCount, gridCellQty);
        }
        D.Log("{0} inspected {1} grid cells, creating {2} ASectors. CoreSectors = {3}, RimSectors = {4}, FailedRimCells = {5}, DrossCells = {6}, OutCells = {7}.",
            DebugName, inspectedCellCount, _sectorIdToSectorLookup.Count, _coreSectorIDs.Count, rimCount, failedRimCount, drossCount, outCount);
        //__LogRimSectorCornerCount();
        double estTotalSectorVol = totalCoreSectorVol + estTotalRimSectorVol;
        double totalCoreSectorPercentOfUniverseVol = totalCoreSectorVol / universeVol;
        double estRimSectorPercentOfUniverseVol = estTotalRimSectorVol / universeVol;
        double totalASectorPercentOfUniverseVol = totalCoreSectorPercentOfUniverseVol + estRimSectorPercentOfUniverseVol;
        D.LogBold("{0}: Estimated navigable volume {1:E1} is {2:P00} of UniverseVolume {3:E1}.", DebugName, estTotalSectorVol, totalASectorPercentOfUniverseVol, universeVol);
        D.Log("{0}: Total CoreSector Volume {1:E1} and estimated Total Rim Volume {2:E1} is {3:P00} and {4:P00} respectively of Universe volume.",
            DebugName, totalCoreSectorVol, estTotalRimSectorVol, totalCoreSectorPercentOfUniverseVol, estRimSectorPercentOfUniverseVol);
        // 7.4.18 Small Results _rimCellVertexThreshold =2: Core=136, Rim=216(min 264 radius), Dross=56; @3: Core=136, Rim=168(min 480 radius), Dross=104
        // CoreSector Vol % of UniverseVol = @2@3 = 50%; RimSectorVol % = @2 31+%, @3 30+% 

        __LogDuration("{0}.ConstructSectors()".Inject(DebugName));
    }

    private bool TryDetermineGridCellUseAsASector(Vector3 gridCellCenter, float universeRadiusSqrd, out UniverseCellCategory category) {
        // From Testing: if > 4 valid corners, center is always valid. With 4 valid corners, center is sometimes valid. 
        // With 3 valid corners, center is rarely valid

        bool isCenterValid = false;
        Vector3 worldCellCenter = _grid.GridToWorld(gridCellCenter);
        if (GameUtility.IsLocationContainedInUniverse(worldCellCenter, universeRadiusSqrd)) {
            isCenterValid = true;
        }

        int validCornerCount = 0;
        Vector3[] gridCellCorners = GetGridCellCorners(gridCellCenter);
        foreach (var gridCellCorner in gridCellCorners) {
            Vector3 worldCellCorner = _grid.GridToWorld(gridCellCorner);

            if (GameUtility.IsLocationContainedInUniverse(worldCellCorner, universeRadiusSqrd)) {
                validCornerCount++;
            }
        }
        bool isGridCellCoreSector = validCornerCount == 8;
        bool isGridCellRim = !isGridCellCoreSector && validCornerCount >= _rimCellVertexThreshold;

        bool isGridCellDross = !isGridCellCoreSector && !isGridCellRim && validCornerCount > 0;
        bool isGridCellOut = validCornerCount == 0;

        if (validCornerCount < 3) {
            D.Assert(!isCenterValid);
        }

        if (isGridCellCoreSector) {
            D.AssertEqual(8, validCornerCount);
            D.Assert(!isGridCellDross);
            D.Assert(!isGridCellRim);
            D.Assert(!isGridCellOut);
            category = UniverseCellCategory.Core;
        }
        else if (isGridCellRim) {
            D.Assert(!isGridCellOut);
            D.Assert(!isGridCellDross);
            D.Assert(!isGridCellCoreSector);
            __RecordRimCornerCount(validCornerCount);
            category = UniverseCellCategory.Rim;
        }
        else if (isGridCellDross) {
            D.Assert(!isGridCellCoreSector);
            D.Assert(!isGridCellRim);
            D.Assert(!isGridCellOut);
            category = UniverseCellCategory.Dross;
        }
        else {
            D.Assert(isGridCellOut);
            category = UniverseCellCategory.Out;

        }
        return category == UniverseCellCategory.Core || category == UniverseCellCategory.Rim;
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

    private void AddCoreSector(IntVector3 sectorID, Vector3 sectorCenterWorldLoc) {
        Sector sector = MakeSectorInstance(sectorID, sectorCenterWorldLoc);
        _sectorIdToCellWorldLocationLookup.Add(sectorID, sectorCenterWorldLoc);
        _sectorIdToSectorLookup.Add(sectorID, sector);
        _coreSectorIDs.Add(sectorID);
    }

    private void AddRimSector(Vector3 sectorCenterWorldLoc, Vector3 positionPropValue, float radiusPropValue, IntVector3 sectorID) {
        RimSector sector = MakeRimSectorInstance(positionPropValue, radiusPropValue, sectorID);
        _sectorIdToCellWorldLocationLookup.Add(sectorID, sectorCenterWorldLoc);
        _sectorIdToSectorLookup.Add(sectorID, sector);
    }

    #endregion

    /// <summary>
    /// Returns the SectorID that contains the provided world location.
    /// Throws an error if <c>worldLocation</c> is not within the universe or there is no Sector at that location.
    /// <remarks>Provided as a convenience when the client knows the location provided is inside the universe and a ASector.
    /// If this is not certain, use TryGetSectorIDContaining(worldLocation) instead.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <returns></returns>
    public IntVector3 GetSectorIDContaining(Vector3 worldLocation) {
        //GameUtility.__ValidateLocationContainedInUniverse(worldLocation, __universeRadiusSqrd);
        if (!GameUtility.IsLocationContainedInUniverse(worldLocation, __universeRadiusSqrd)) {
            D.Error("{0}: {1} is not contained in Universe.", DebugName, worldLocation);
        }
        IntVector3 sectorID;
        Vector3 nearestCellGridCoordinates = _grid.NearestCell(worldLocation, RectGrid.CoordinateSystem.Grid);
        bool isSectorIDPresent = _cellGridCoordinatesToSectorIdLookup.TryGetValue(nearestCellGridCoordinates, out sectorID);
        if (!isSectorIDPresent) {
            D.Error("@{0} found cell {1} using _grid.NearestCell that has no Sector for WorldLocation {2}. " +
                "Use TryGetSectorIDContaining instead?", DebugName, nearestCellGridCoordinates, worldLocation);
        }
        return sectorID;
    }

    /// <summary>
    /// Gets the world space location of the sector indicated by the provided sectorID.
    /// Throws an error if no sector is present at that sectorID.
    /// <remarks>This may not be the same value as a RimSector's Position property.</remarks>
    /// </summary>
    /// <param name="sectorID">ID of the sector.</param>
    /// <returns></returns>
    public Vector3 GetSectorWorldLocation(IntVector3 sectorID) {
        D.AssertNotDefault(sectorID);
        Vector3 worldCenterLocation;
        bool isSectorFound = _sectorIdToCellWorldLocationLookup.TryGetValue(sectorID, out worldCenterLocation);
        D.Assert(isSectorFound, sectorID.DebugName);
        return worldCenterLocation;
    }


    /// <summary>
    /// Returns <c>true</c> if a sectorID has been assigned containing this worldLocation, <c>false</c> otherwise.
    /// <remarks>Locations inside the universe have assigned SectorIDs, while those outside do not.</remarks>
    /// <remarks>5.22.17 This version allows the use of worldLocations outside the Universe.
    /// Current known users that use locations outside the Universe are SectorExaminer and MainCameraControl.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <param name="sectorID">The resulting sectorID.</param>
    /// <returns></returns>
    public bool TryGetSectorIDContaining(Vector3 worldLocation, out IntVector3 sectorID) {
        Vector3 nearestCellGridCoordinates = _grid.NearestCell(worldLocation, RectGrid.CoordinateSystem.Grid);
        return _cellGridCoordinatesToSectorIdLookup.TryGetValue(nearestCellGridCoordinates, out sectorID);
    }

    /// <summary>
    /// Gets the sector associated with this sectorID. 
    /// </summary>
    /// <param name="sectorID">The sectorID.</param>
    /// <returns></returns>
    public ASector GetSector(IntVector3 sectorID) {
        D.AssertNotDefault(sectorID);
        ASector sector;
        if (!_sectorIdToSectorLookup.TryGetValue(sectorID, out sector)) {
            D.Error("{0}: No Sector at {1}.", DebugName, sectorID.DebugName);
        }
        return sector;
    }

    /// <summary>
    /// Returns <c>true</c> if a sector has been assigned containing this worldLocation, <c>false</c> otherwise.
    /// <remarks>Throws an error if worldLocation is outside the Universe.</remarks>
    /// <remarks>Not all locations inside the universe are in Sectors as some locations are inside RimCells.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <param name="sector">The sector.</param>
    /// <returns></returns>
    public bool TryGetSectorContaining(Vector3 worldLocation, out ASector sector) {
        GameUtility.__ValidateLocationContainedInUniverse(worldLocation, __universeRadiusSqrd);
        IntVector3 sectorID;
        if (TryGetSectorIDContaining(worldLocation, out sectorID)) {
            sector = GetSector(sectorID);
            return true;
        }
        sector = null;
        return false;
    }

    /// <summary>
    /// Gets the sector containing the provided worldLocation.
    /// Throws an error if <c>worldLocation</c> is not within the universe or not within a Sector.
    /// <remarks>Provided as a convenience when the client knows the location provided is inside the universe and a Sector.
    /// If this is not certain, use TryGetSectorContaining(worldLocation) instead.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <returns></returns>
    public ASector GetSectorContaining(Vector3 worldLocation) {
        var sectorID = GetSectorIDContaining(worldLocation);
        return GetSector(sectorID);
    }

    public bool TryGetRandomSectorID(out IntVector3 sectorID, IEnumerable<IntVector3> excludedIDs = null, bool includeRim = true) {
        IEnumerable<IntVector3> idsToChooseFrom = includeRim ? _sectorIdToCellGridCoordinatesLookup.Keys : _coreSectorIDs;
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

    public bool IsCoreSector(IntVector3 sectorID) {
        D.Assert(_sectorIdToSectorLookup.Keys.Contains(sectorID));
        return _coreSectorIDs.Contains(sectorID);
    }

    public bool IsRimSector(IntVector3 sectorID) {
        return !IsCoreSector(sectorID);
    }

    /// <summary>
    /// Gets the IDs of the neighbors to the sector indicated by centerSectorID including RimSectors.
    /// The centerSectorID is not included.
    /// <remarks>Provided as a convenience when you know you are in a Sector.
    /// Use GetNeighboringSectorIDs(worldLocation) if you may be over a Cell without a Sector.</remarks>
    /// </summary>
    /// <param name="centerSectorID">The center SectorID.</param>
    /// <param name="includeRim">if set to <c>true</c> [include rim].</param>
    /// <returns></returns>
    public IList<IntVector3> GetNeighboringSectorIDs(IntVector3 centerSectorID, bool includeRim = true) {
        D.AssertNotDefault(centerSectorID);
        var sectorIDs = includeRim ? _sectorIdToSectorLookup.Keys : _coreSectorIDs;
        IList<IntVector3> neighbors = new List<IntVector3>();
        int[] xValuePair = CalcSectorIDNeighborPair(centerSectorID.x);
        int[] yValuePair = CalcSectorIDNeighborPair(centerSectorID.y);
        int[] zValuePair = CalcSectorIDNeighborPair(centerSectorID.z);
        foreach (var x in xValuePair) {
            foreach (var y in yValuePair) {
                foreach (var z in zValuePair) {
                    IntVector3 sectorID = new IntVector3(x, y, z);
                    if (sectorIDs.Contains(sectorID)) {
                        neighbors.Add(sectorID);
                    }
                }
            }
        }
        return neighbors;
    }

    private int[] CalcSectorIDNeighborPair(int centerIdAxisValue) {
        int[] neighborValuePair = new int[2];
        // no 0 value in my sectorIDs
        neighborValuePair[0] = centerIdAxisValue - 1 == 0 ? centerIdAxisValue - 2 : centerIdAxisValue - 1;
        neighborValuePair[1] = centerIdAxisValue + 1 == 0 ? centerIdAxisValue + 2 : centerIdAxisValue + 1;
        return neighborValuePair;
    }

    /// <summary>
    /// Gets the neighboring sectorIDs within the Universe that surrounds the ASector containing worldLocation.
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <param name="includeRim">if set to <c>true</c> [include rim].</param>
    /// <returns></returns>
    public IEnumerable<IntVector3> GetNeighboringSectorIDs(Vector3 worldLocation, bool includeRim = true) {
        GameUtility.__ValidateLocationContainedInUniverse(worldLocation, __universeRadiusSqrd);
        Vector3 gridCellCoordinates = _grid.WorldToGrid(worldLocation);
        IList<IntVector3> neighbors = new List<IntVector3>();
        float[] xValuePair = CalcGridCellNeighborPair(gridCellCoordinates.x);
        float[] yValuePair = CalcGridCellNeighborPair(gridCellCoordinates.y);
        float[] zValuePair = CalcGridCellNeighborPair(gridCellCoordinates.z);
        foreach (var x in xValuePair) {
            foreach (var y in yValuePair) {
                foreach (var z in zValuePair) {
                    Vector3 neighborGridCoordinates = new Vector3(x, y, z);
                    IntVector3 neighborSectorID;
                    if (_cellGridCoordinatesToSectorIdLookup.TryGetValue(neighborGridCoordinates, out neighborSectorID)) {
                        if (!includeRim && IsRimSector(neighborSectorID)) {
                            continue;
                        }
                        neighbors.Add(neighborSectorID);
                    }
                }
            }
        }
        D.AssertNotEqual(Constants.Zero, neighbors.Count);
        return neighbors;
    }

    private float[] CalcGridCellNeighborPair(float gridCellAxisValue) {
        float[] neighborValuePair = new float[2];
        neighborValuePair[0] = gridCellAxisValue - 1F;
        neighborValuePair[1] = gridCellAxisValue + 1F;
        return neighborValuePair;
    }

    /// <summary>
    /// Gets the neighboring sectors to the sector at centerID including RimSectors.
    /// The sector at centerID is not included.
    /// </summary>
    /// <param name="centerID">The centerID.</param>
    /// <param name="includeRim">if set to <c>true</c> [include rim].</param>
    /// <returns></returns>
    public IEnumerable<ISector_Ltd> GetNeighboringSectors(IntVector3 centerID, bool includeRim = true) {
        IList<ISector_Ltd> neighborSectors = new List<ISector_Ltd>();
        foreach (var sectorID in GetNeighboringSectorIDs(centerID, includeRim)) {
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
    /// Returns a SectorID that can be used as a Home Sector for a player based on playerSeparation
    /// </summary>
    /// <param name="playerSeparation">The player separation.</param>
    /// <param name="existingHomeSectorIDs">The existing SectorIDs being used by other players for their Home.</param>
    /// <param name="excludedSectorIDs">The excluded SectorIDs.</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IntVector3 GetHomeSectorID(PlayerSeparation playerSeparation, IEnumerable<IntVector3> existingHomeSectorIDs,
        IEnumerable<IntVector3> excludedSectorIDs) {
        // the term ..GridCells refers to the Vector3 center of cell in the grid coordinate system, aka (0.5, 1.5, -0.5)
        int uSectorRadius = _gameMgr.GameSettings.UniverseSize.RadiusInSectors();
        float minOriginToGridCellDistance;
        float maxOriginToGridCellDistance;
        switch (playerSeparation) {
            case PlayerSeparation.Close:
                minOriginToGridCellDistance = Constants.ZeroF;
                maxOriginToGridCellDistance = uSectorRadius / Constants.OneThird;
                break;
            case PlayerSeparation.Normal:
                minOriginToGridCellDistance = uSectorRadius / Constants.OneThird;
                maxOriginToGridCellDistance = uSectorRadius / Constants.TwoThirds;
                break;
            case PlayerSeparation.Distant:
                minOriginToGridCellDistance = uSectorRadius * Constants.TwoThirds;
                maxOriginToGridCellDistance = uSectorRadius;
                break;
            case PlayerSeparation.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(playerSeparation));
        }
        float minOriginToGridCellDistanceSqrd = minOriginToGridCellDistance * minOriginToGridCellDistance;
        float maxOriginToGridCellDistanceSqrd = maxOriginToGridCellDistance * maxOriginToGridCellDistance;

        var allCandidateSectorIDs = _coreSectorIDs.Except(existingHomeSectorIDs).Except(excludedSectorIDs);
        var allCandidateGridCells = _sectorIdToCellGridCoordinatesLookup.Where(kvp => allCandidateSectorIDs.Contains(kvp.Key))
            .Select(kvp => kvp.Value);

        IList<Vector3> candidateGridCells = new List<Vector3>();
        foreach (var gridCell in allCandidateGridCells) {
            float originToGridCellDistanceSqrd = Vector3.SqrMagnitude(gridCell - GameConstants.UniverseOrigin);
            if (Utility.IsInRange(originToGridCellDistanceSqrd, minOriginToGridCellDistanceSqrd, maxOriginToGridCellDistanceSqrd)) {
                candidateGridCells.Add(gridCell);
            }
        }
        D.Assert(candidateGridCells.Any());

        var existingHomeGridCells = _sectorIdToCellGridCoordinatesLookup.Where(kvp => existingHomeSectorIDs.Contains(kvp.Key))
            .Select(kvp => kvp.Value);

        Vector3 chosenGridCell;
        if (!existingHomeGridCells.Any()) {
            // First SectorID choice: choose candidateGridCell that is furthest away from origin
            chosenGridCell = candidateGridCells.MaxBy(cCell => Vector3.SqrMagnitude(cCell - GameConstants.UniverseOrigin));
        }
        else {
            chosenGridCell = MyMath.ChooseFurthestFrom(candidateGridCells, existingHomeGridCells);
        }

        var chosenHomeSectorID = _cellGridCoordinatesToSectorIdLookup[chosenGridCell];
        return chosenHomeSectorID;
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
            D.Error("{0}: No CellGridCoordinates recorded for SectorID {1}.", DebugName, sectorID.DebugName);
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

    private Sector MakeSectorInstance(IntVector3 sectorID, Vector3 worldLocation) {
        Sector sector = new Sector(worldLocation);
        SectorData data = new SectorData(sector, sectorID) {
            Name = SectorNameFormat.Inject(sectorID)
            //Density = 1F  the concept of space density is now attached to Topography, not Sectors. Density is relative and affects only drag
        };
        sector.Data = data;
        return sector;
    }

    private RimSector MakeRimSectorInstance(Vector3 positionPropValue, float radiusPropValue, IntVector3 sectorID) {
        return new RimSector(positionPropValue, radiusPropValue, sectorID);
    }

    #region Debug

    private float __universeRadiusSqrd;
    private IList<int> __rimCellValidCornerCount;

    private void __RecordRimCornerCount(int validCornerCount) {
        //D.Log("{0}: Rim valid corners = {1}.", DebugName, validCornerCount);
        Utility.ValidateForRange(validCornerCount, _rimCellVertexThreshold, 7);
        __rimCellValidCornerCount = __rimCellValidCornerCount ?? new List<int>();
        // no need to clear as SectorGrid will always be new with new game
        __rimCellValidCornerCount.Add(validCornerCount);
    }

    private void __LogRimSectorCornerCount() {
        for (int vertexCount = _rimCellVertexThreshold; vertexCount < 8; vertexCount++) {
            int cellCount = __rimCellValidCornerCount.Where(cellValidCornerCount => cellValidCornerCount == vertexCount).Count();
            D.Log("{0}: Number of RimSectors with {1} valid corners = {2}.", DebugName, vertexCount, cellCount);
            // Small: 1:56, 2:48, 3:24, 4:96, 5:0, 6:24, 7:24, Total 272 Rim
        }
    }

    /// <summary>
    /// Returns a random sectorID including IDs for RimSectors.
    /// <remarks>Debug to place fleets in RimSectors.</remarks>
    /// </summary>
    /// <param name="sectorID">The sector identifier.</param>
    /// <param name="excludedIDs">The excluded IDs.</param>
    /// <returns></returns>
    public bool __TryGetRandomSectorID(out IntVector3 sectorID, IEnumerable<IntVector3> excludedIDs = null) {
        IEnumerable<IntVector3> idsToChooseFrom = _sectorIdToSectorLookup.Keys;
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
    /// Validates that no world sector corner is a duplicate
    /// or almost duplicate of another. 
    /// <remarks>Warning! 1300 corners takes 8+ secs.</remarks>
    /// </summary>
    private void __ValidateWorldSectorCorners() {
        __RecordDurationStartTime();
        // verify no duplicate corners
        var worldCorners = _cellVertexWorldLocations;
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
        D.Log("{0} validated {1} sector corners.", DebugName, cornerCount);
        __LogDuration("{0}.__ValidateWorldSectorCorners()".Inject(DebugName));
    }

    #endregion

    #region Obsolete Archive

    /// <summary>
    /// Gets the sectorIDs that surround centerSectorID that reside between minDistance and maxDistance sectors away
    /// from centerSector.
    /// <remarks>7.5.18 The returned SectorIDs do not distinguish between CoreSectors and RimSectors.</remarks>
    /// </summary>
    /// <param name="centerSectorID">The center SectorID.</param>
    /// <param name="minDistance">The minimum distance in sectors from centerSectorID.</param>
    /// <param name="maxDistance">The maximum distance in sectors from centerSectorID.</param>
    /// <returns></returns>
    [Obsolete]
    public IList<IntVector3> GetSurroundingSectorIDsBetween(IntVector3 centerSectorID, int minDistance, int maxDistance) {
        D.AssertNotDefault(centerSectorID);
        D.Assert(_sectorIdToSectorLookup.ContainsKey(centerSectorID));
        Utility.ValidateForRange(minDistance, 0, maxDistance - 1);
        Utility.ValidateForRange(maxDistance, minDistance + 1, _gameMgr.GameSettings.UniverseSize.RadiusInSectors() * 2);

        __RecordDurationStartTime();

        int minDistanceSqrd = minDistance * minDistance;
        int maxDistanceSqrd = maxDistance * maxDistance;

        Vector3 centerGridCellCoordinates = _sectorIdToCellGridCoordinatesLookup[centerSectorID];

        Vector3[] allCandidateGridCellCoordinates = _cellGridCoordinatesToSectorIdLookup.Keys.Except(centerGridCellCoordinates).ToArray();
        int gridCellQty = allCandidateGridCellCoordinates.Length;
        IList<IntVector3> resultingSectorIDs = new List<IntVector3>(gridCellQty);

        for (int i = 0; i < gridCellQty; i++) {
            Vector3 candidateGridCellCoordinates = allCandidateGridCellCoordinates[i];
            Vector3 vectorFromCenterToCandidateGridCell = candidateGridCellCoordinates - centerGridCellCoordinates;
            float vectorSqrMagnitude = vectorFromCenterToCandidateGridCell.sqrMagnitude;
            if (Utility.IsInRange(vectorSqrMagnitude, minDistanceSqrd, maxDistanceSqrd)) {
                IntVector3 candidateSectorID = _cellGridCoordinatesToSectorIdLookup[candidateGridCellCoordinates];
                resultingSectorIDs.Add(candidateSectorID);
            }
        }

        if ((Utility.SystemTime - __durationStartTime).TotalSeconds > 0.1F) {
            __LogDuration("{0}.GetSurroundingSectorIdsBetween()".Inject(DebugName));
        }
        return resultingSectorIDs;
    }

    /// <summary>
    /// Gets the nearest SectorID to the provided <c>worldLocation</c>.
    /// <remarks>Expensive.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <returns></returns>
    [Obsolete]
    private IntVector3 GetNearestSectorIDTo(Vector3 worldLocation) {
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

    [Obsolete]
    private bool TryDetermineCellUseAsSector(Vector3 gridCellCenter, float universeRadiusSqrd, out SectorCategory category) {
        category = SectorCategory.None;
        bool isCellValidSector = false;
        bool isACellCornerOutsideUniverse = false;
        float closestValidWorldCellCornerSqrDistanceFromUniverseOrigin = Mathf.Infinity;
        Vector3 universeOrigin = GameConstants.UniverseOrigin;

        // if all cell corners are inside universe, its Core, if none, its not valid
        int validCornerCount = 0;
        Vector3[] gridCellCorners = GetGridCellCorners(gridCellCenter);
        foreach (var gridCellCorner in gridCellCorners) {
            float worldCellCornerSqrDistanceFromUniverseOrigin = 0F;
            Vector3 worldCellCorner = _grid.GridToWorld(gridCellCorner);

            if ((worldCellCornerSqrDistanceFromUniverseOrigin = Vector3.SqrMagnitude(worldCellCorner - universeOrigin)) <= universeRadiusSqrd) {
                isCellValidSector = true;
                validCornerCount++;
                if (worldCellCornerSqrDistanceFromUniverseOrigin < closestValidWorldCellCornerSqrDistanceFromUniverseOrigin) {
                    closestValidWorldCellCornerSqrDistanceFromUniverseOrigin = worldCellCornerSqrDistanceFromUniverseOrigin;
                }
            }
            else {
                isACellCornerOutsideUniverse = true;
            }
        }

        if (isCellValidSector) {
            D.Assert(validCornerCount > 0);
            if (isACellCornerOutsideUniverse) {
                // if cell center is inside, its Peripheral
                Vector3 worldCellCenter = _grid.GridToWorld(gridCellCenter);
                if (Vector3.SqrMagnitude(worldCellCenter - universeOrigin) <= universeRadiusSqrd) {
                    category = SectorCategory.Peripheral;
                }
                else {
                    // if cell center outside, its Rim
                    category = SectorCategory.Rim;
                    if (validCornerCount == 1) {
                        // except if there is only 1 corner inside and its right on the universe edge
                        float distanceFromUniverseEdge = Mathf.Sqrt(universeRadiusSqrd) - Mathf.Sqrt(closestValidWorldCellCornerSqrDistanceFromUniverseOrigin);
                        if (distanceFromUniverseEdge.ApproxEquals(Constants.ZeroF)) {
                            isCellValidSector = false;
                            category = SectorCategory.None;
                        }
                        else if (Mathfx.Approx(distanceFromUniverseEdge, Constants.ZeroF, 40F)) {
                            // 5.13.17 My testing shows the single corner is a minimum of these values inside:
                            // Gigantic: 40, Enormous: 54, Large: 151, Normal: 100, Small: 310, Tiny: No cells with only 1 corner inside
                            D.Warn("{0}: Cell found with only 1 corner on or inside universeEdge with very little area. Distance inside = {1}.",
                                DebugName, distanceFromUniverseEdge);
                        }
                    }
                }
            }
            else {
                D.AssertEqual(8, validCornerCount);
                category = SectorCategory.Core;
            }
        }
        return isCellValidSector;
    }

    #endregion

    protected override void Cleanup() {
        GameReferences.SectorGrid = null;
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
        _mainCameraCntl.sectorIDChanged -= CameraSectorIDChangedEventHandler;
    }

    public override string ToString() {
        return DebugName;
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

    #region Nested Classes

    public enum UniverseCellCategory {
        None,

        /// <summary>
        /// A cell that is completely inside the universe.
        /// </summary>
        Core,
        /// <summary>
        /// A cell that is mostly inside the universe.
        /// </summary>
        Rim,
        /// <summary>
        /// A cell that was a candidate to become a RimSector but failed to 
        /// find a Position and Radius property that would allow it to operate as one.
        /// </summary>
        FailedRim,
        /// <summary>
        /// A cell mostly outside the universe whose portion inside is not
        /// deemed sufficient to attempt to make it a RimSector.
        /// </summary>
        Dross,
        /// <summary>
        /// A cell completely outside the universe.
        /// </summary>
        Out
    }

    #endregion

    #region ISectorGrid Members

    private IEnumerable<ISector> _iSectorGridSectors;
    IEnumerable<ISector> ISectorGrid.Sectors {
        get {
            _iSectorGridSectors = _iSectorGridSectors ?? _sectorIdToSectorLookup.Values.Cast<ISector>();
            return _iSectorGridSectors;
        }
    }

    private IEnumerable<ISector> _iSectorGridCoreSectors;
    IEnumerable<ISector> ISectorGrid.CoreSectors {
        get {
            _iSectorGridCoreSectors = _iSectorGridCoreSectors ?? CoreSectors.Cast<ISector>();
            return _iSectorGridCoreSectors;
        }
    }

    ISector ISectorGrid.GetSector(IntVector3 sectorID) {
        return GetSector(sectorID);
    }

    ISector ISectorGrid.GetSectorContaining(Vector3 worldLocation) {
        return GetSectorContaining(worldLocation);
    }

    #endregion

}

