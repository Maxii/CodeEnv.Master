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

    private const string SectorNameFormat = "{0}[{1}]";

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
    private int _rimCellVertexThreshold = 1;    // 7.10.18 Testing shows 1 works. ASector UniverseCoverage >>> 99%

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

    private IEnumerable<CoreSector> _coreSectors;
    public IEnumerable<CoreSector> CoreSectors {
        get {
            _coreSectors = _coreSectors ?? _sectorIdToSectorLookup.Values.Where(sector => sector is CoreSector).Cast<CoreSector>();
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
        HandleViewModeChanged();
    }

    private void CameraSectorIDChangedEventHandler(object sender, EventArgs e) {
        HandleCameraSectorIDChanged();
    }

    #endregion

    private void HandleViewModeChanged() {
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

    private void HandleCameraSectorIDChanged() {
        if (_playerViews.ViewMode == PlayerViewMode.SectorView) {
            D.Assert(IsGridWireframeShowing);

            IntVector3 cameraSectorID;
            bool isCameraSectorIdValid = _mainCameraCntl.TryGetValidSectorID(out cameraSectorID);
            bool isCameraOutsideUniverse = !GameUtility.IsLocationContainedInUniverse(_mainCameraCntl.Position);
            //D.Log("{0}: CameraSectorID has changed to {1}. Generating new grid points.", DebugName, cameraSectorID);
            if (isCameraOutsideUniverse || !isCameraSectorIdValid || _sectorVisibilityDepth == Constants.Zero) {
                // Camera is either 1) outside the universe, 2) inside but over an invalid cell or 3) we are supposed to show all 
                _gridWireframe.Points = GenerateAllWireframeGridPoints();
            }
            else {
                // Camera is inside the universe AND its over a valid cell AND we aren't supposed to show all
                _gridWireframe.Points = GenerateWireframeGridPoints(cameraSectorID);
            }
        }
    }

    #region Show SectorView Grid

    private void ShowSectorGrid(bool toShow) {
        if (IsGridWireframeShowing != toShow) {
            if (toShow) {
                D.Assert(!IsGridWireframeShowing);
                IntVector3 cameraSectorID;
                bool isCameraSectorIdValid = _mainCameraCntl.TryGetValidSectorID(out cameraSectorID);
                bool isCameraOutsideUniverse = !GameUtility.IsLocationContainedInUniverse(_mainCameraCntl.Position);

                List<Vector3> gridPoints;
                if (isCameraOutsideUniverse || !isCameraSectorIdValid || _sectorVisibilityDepth == Constants.Zero) {
                    // Camera is either 1) outside the universe, 2) inside but over an invalid cell or 3) we are supposed to show all 
                    gridPoints = GenerateAllWireframeGridPoints();
                }
                else {
                    // Camera is inside the universe AND its over a valid cell AND we aren't supposed to show all
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
        D.Assert(Mathfx.Approx(transform.position, GameConstants.UniverseOrigin, .01F), transform.position.ToString());
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

        UniverseSize universeSize = _gameMgr.GameSettings.UniverseSize;
        float universeRadius = universeSize.Radius();
        //D.Log("{0}: UniverseRadius is {1:0.#} for a SectorGridSize of {2}.", DebugName, universeRadius, _gridSize);
        __universeRadiusSqrd = universeRadius * universeRadius;
        float universeNavigableRadius = universeSize.NavigableRadius();

        double universeVol = Constants.FourThirds * Mathf.PI * Mathf.Pow(universeRadius, 3F);
        double volOfACoreSector = Mathf.Pow(TempGameValues.SectorSideLength, 3F);
        double totalCoreSectorVol = 0D;
        double estAllSectorInsideUniverseVol = universeVol;

        int inspectedCellCount = Constants.Zero;
        int rimCount = Constants.Zero;
        int failedRimCount = Constants.Zero;
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
                    IEnumerable<Vector3> validCorners;
                    if (TryDetermineGridCellUseAsASector(cellGridCoordinates, __universeRadiusSqrd, out cellCategory, out validCorners)) {
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
                            D.AssertEqual(UniverseCellCategory.RimCandidate, cellCategory);
                            Vector3 acceptablePositionPropValue;
                            float acceptableRadiusPropValue;
                            if (TryFindAcceptableRimCell(cellWorldLocation, universeSize, universeNavigableRadius, out acceptablePositionPropValue,
                                out acceptableRadiusPropValue)) {
                                cellCategory = UniverseCellCategory.Rim;
                                rimCount++;
                                IntVector3 sectorID = CalculateSectorIDFromCellGridCoordindates(cellGridCoordinates);

                                Vector3 cellVertexWorldLocation = _grid.GridToWorld(cellVertexGridCoordinates);
                                _cellVertexWorldLocations.Add(cellVertexWorldLocation);
                                _cellGridCoordinatesToSectorIdLookup.Add(cellGridCoordinates, sectorID);
                                _sectorIdToCellGridCoordinatesLookup.Add(sectorID, cellGridCoordinates);

                                AddRimSector(cellWorldLocation, acceptablePositionPropValue, acceptableRadiusPropValue, sectorID);
                                if (_logDebugRimCellStepValues) {
                                    __RecordRimCornerCount(validCorners.Count());
                                }
                            }
                            else {
                                //D.Log(@"{0} could not find acceptable position for RimSector candidate. ValidCornerCount = {1}, 
                                //    RimCellVertexThreshold = {2}.", DebugName, validCorners.Count(), _rimCellVertexThreshold);
                                cellCategory = UniverseCellCategory.FailedRim;
                                failedRimCount++;
                                float estFailedRimCellVolInsideUniverse = __EstimateFailedRimCellUniverseVolume(cellWorldLocation, validCorners, universeRadius);
                                estAllSectorInsideUniverseVol -= estFailedRimCellVolInsideUniverse;
                            }
                        }
                    }
                    else {
                        D.AssertEqual(UniverseCellCategory.Out, cellCategory);
                        outCount++;
                    }
                }
            }
        }

        int gridCellQty = Mathf.RoundToInt(_gridSize.x * _gridSize.y * _gridSize.z);
        if (inspectedCellCount != gridCellQty) {
            D.Error("{0}: inspected cell count {1} should equal {2} cells in grid.", DebugName, inspectedCellCount, gridCellQty);
        }
        D.Log("{0} inspected {1} grid cells, creating {2} ASectors. CoreSectors: {3}, RimSectors: {4}, FailedRimCells: {5}, OutCells: {6}.",
            DebugName, inspectedCellCount, _sectorIdToSectorLookup.Count, _coreSectorIDs.Count, rimCount, failedRimCount, outCount);
        if (_logDebugRimCellStepValues) {
            __LogRimSectorCornerCount();
            __LogRimCellStepValues(universeSize);

            double estTotalRimSectorVol = estAllSectorInsideUniverseVol - totalCoreSectorVol;
            float estAllSectorPercentOfUniverseVol = (float)(estAllSectorInsideUniverseVol / universeVol);
            float coreSectorPercentOfUniverseVol = (float)(totalCoreSectorVol / universeVol);
            float estRimSectorPercentOfUniverseVol = (float)(estTotalRimSectorVol / universeVol);

            D.Log("{0}: Estimated navigable volume {1:E6} is {2:P6} of UniverseVolume {3:E6}.", DebugName, estAllSectorInsideUniverseVol,
                estAllSectorPercentOfUniverseVol, universeVol);
            D.Log("{0}: CoreSector Volume {1:E6} and estimated Rim Volume {2:E6} is {3:P6} and {4:P6} respectively of Universe volume.",
                DebugName, totalCoreSectorVol, estTotalRimSectorVol, coreSectorPercentOfUniverseVol, estRimSectorPercentOfUniverseVol);
        }

        __LogDuration("{0}.ConstructSectors()".Inject(DebugName));
    }

    private bool TryDetermineGridCellUseAsASector(Vector3 gridCellCenter, float universeRadiusSqrd, out UniverseCellCategory category,
        out IEnumerable<Vector3> validCorners) {
        IList<Vector3> validCorners_Internal = new List<Vector3>();

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
                validCorners_Internal.Add(worldCellCorner);
                validCornerCount++;
            }
        }
        bool isCellACoreSector = validCornerCount == 8;
        bool isCellARimSectorCandidate = !isCellACoreSector && validCornerCount >= _rimCellVertexThreshold;

        bool isGridCellOut = validCornerCount == 0;

        if (validCornerCount < 3) {
            // From Testing: if > 4 valid corners, center is always valid. With 4 valid corners, center is sometimes valid. 
            // With 3 valid corners, center is rarely valid and with < 3 valid corners, never valid
            D.Assert(!isCenterValid);
        }

        if (isCellACoreSector) {
            D.AssertEqual(8, validCornerCount);
            D.Assert(!isCellARimSectorCandidate);
            D.Assert(!isGridCellOut);
            category = UniverseCellCategory.Core;
        }
        else if (isCellARimSectorCandidate) {
            D.Assert(!isCellACoreSector);
            D.Assert(!isGridCellOut);
            category = UniverseCellCategory.RimCandidate;
        }
        else {
            D.Assert(isGridCellOut);
            category = UniverseCellCategory.Out;
        }
        validCorners = validCorners_Internal;
        return category == UniverseCellCategory.Core || category == UniverseCellCategory.RimCandidate;
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
        CoreSector sector = MakeSectorInstance(sectorID, sectorCenterWorldLoc);
        _sectorIdToCellWorldLocationLookup.Add(sectorID, sectorCenterWorldLoc);
        _sectorIdToSectorLookup.Add(sectorID, sector);
        _coreSectorIDs.Add(sectorID);
    }

    private void AddRimSector(Vector3 sectorCenterWorldLoc, Vector3 positionPropValue, float radiusPropValue, IntVector3 sectorID) {
        RimSector sector = MakeRimSectorInstance(sectorCenterWorldLoc, positionPropValue, radiusPropValue, sectorID);
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
        GameUtility.__ValidateLocationContainedInUniverse(worldLocation, __universeRadiusSqrd);
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
    /// Gets the world space location of the center of the sector indicated by the provided sectorID.
    /// Throws an error if no sector is present at that sectorID.
    /// <remarks>This may not be the same value as a RimSector's Position property.</remarks>
    /// </summary>
    /// <param name="sectorID">ID of the sector.</param>
    /// <returns></returns>
    public Vector3 GetSectorCenterLocation(IntVector3 sectorID) {
        D.AssertNotDefault(sectorID);
        Vector3 centerLocation;
        bool isSectorFound = _sectorIdToCellWorldLocationLookup.TryGetValue(sectorID, out centerLocation);
        D.Assert(isSectorFound, sectorID.DebugName);
        return centerLocation;
    }

    /// <summary>
    /// Returns <c>true</c> if a CoreSector has been assigned containing this worldLocation, <c>false</c> otherwise.
    /// <remarks>Locations outside the universe and a very small percentage inside the universe (locations in FailedRimCells) 
    /// do not have assigned SectorIDs at all. RimSectors have assigned SectorIDs but aren't Core Sectors.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <param name="sectorID">The resulting core sectorID.</param>
    /// <returns></returns>
    public bool TryGetCoreSectorIDContaining(Vector3 worldLocation, out IntVector3 coreSectorID) {
        Vector3 nearestCellGridCoordinates = _grid.NearestCell(worldLocation, RectGrid.CoordinateSystem.Grid);
        IntVector3 sectorID;
        if (_cellGridCoordinatesToSectorIdLookup.TryGetValue(nearestCellGridCoordinates, out sectorID)) {
            if (IsCoreSector(sectorID)) {
                coreSectorID = sectorID;
                return true;
            }
        }
        coreSectorID = default(IntVector3);
        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if a sectorID has been assigned containing this worldLocation, <c>false</c> otherwise.
    /// <remarks>Locations outside the universe and a very small percentage inside the universe (locations in FailedRimCells) 
    /// do not have assigned SectorIDs.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <param name="sectorID">The resulting sectorID.</param>
    /// <returns></returns>
    public bool TryGetSectorIDContaining(Vector3 worldLocation, out IntVector3 sectorID) {
        Vector3 nearestCellGridCoordinates = _grid.NearestCell(worldLocation, RectGrid.CoordinateSystem.Grid);
        return _cellGridCoordinatesToSectorIdLookup.TryGetValue(nearestCellGridCoordinates, out sectorID);
    }

    /// <summary>
    /// Gets the nearest Core SectorID to the provided <c>worldLocation</c>.
    /// <remarks>Warns if worldLocation is already contained in a CoreSector.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <returns></returns>
    public IntVector3 GetClosestCoreSectorIDTo(Vector3 worldLocation) {
        IntVector3 coreSectorID;
        if (TryGetCoreSectorIDContaining(worldLocation, out coreSectorID)) {
            D.Warn("{0}: {1} is already within Sector {2}. This method is intended for use after checking TryGetCoreSectorIDContaining().",
                DebugName, worldLocation, coreSectorID.DebugName);
        }

        if (coreSectorID == default(IntVector3)) {
            // Either worldLocation is in a RimSector or not located in a sector at all
            if (GameUtility.IsLocationContainedInUniverse(worldLocation, __universeRadiusSqrd)) {
                // worldLocation is in a RimSector or FailedRimCell without a SectorID
                var neighboringCoreSectorIDs = GetLocalSectorIDs(worldLocation, includeRim: false);
                if (neighboringCoreSectorIDs.Any()) {
                    // the coreSectorID whose position is closest to worldLocation
                    coreSectorID = neighboringCoreSectorIDs.MinBy(cSectorID => Vector3.SqrMagnitude(worldLocation - GetSector(cSectorID).Position));
                }
            }
        }

        if (coreSectorID == default(IntVector3)) {
            // worldLocation is in a cell without any CoreSector neighbors. This is expensive
            Vector3 nearestCellToWorldLocation = _grid.NearestCell(worldLocation, RectGrid.CoordinateSystem.Grid);
            Vector3 nearestCellWithCoreSectorID = default(Vector3);    // the nearest cell grid coordinates that has an associated CoreSectorID
            float smallestSqrDistanceToCellWithCoreSectorID = float.MaxValue;

            // all the cell grid coordinates that have an associated Core Sector
            var allCellsWithCoreSectorID = _sectorIdToCellGridCoordinatesLookup.Where(kvp => IsCoreSector(kvp.Key)).Select(kvp => kvp.Value);
            float sqrDistance;
            foreach (var cellWithCoreSectorID in allCellsWithCoreSectorID) {
                if ((sqrDistance = Vector3.SqrMagnitude(nearestCellToWorldLocation - cellWithCoreSectorID)) < smallestSqrDistanceToCellWithCoreSectorID) {
                    nearestCellWithCoreSectorID = cellWithCoreSectorID;
                    smallestSqrDistanceToCellWithCoreSectorID = sqrDistance;
                }
            }
            D.Assert(nearestCellWithCoreSectorID != default(Vector3));
            coreSectorID = _cellGridCoordinatesToSectorIdLookup[nearestCellWithCoreSectorID];
            D.Assert(IsCoreSector(coreSectorID));
        }
        D.Assert(coreSectorID != default(IntVector3));

        return coreSectorID;
    }

    /// <summary>
    /// Gets the nearest SectorID to the provided <c>worldLocation</c>.
    /// <remarks>Warns if worldLocation is already contained in ASector.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <returns></returns>
    public IntVector3 GetClosestSectorIDTo(Vector3 worldLocation) {
        IntVector3 sectorID;
        if (TryGetSectorIDContaining(worldLocation, out sectorID)) {
            D.Warn("{0}: {1} is already within Sector {2}. This method is intended for use after checking TryGetSectorIDContaining().",
                DebugName, worldLocation, sectorID.DebugName);
        }

        if (sectorID == default(IntVector3)) {
            if (GameUtility.IsLocationContainedInUniverse(worldLocation, __universeRadiusSqrd)) {
                // worldLocation is in a FailedRimCell without a SectorID
                var neighboringSectorIDs = GetLocalSectorIDs(worldLocation);
                // the sectorID whose 'PositionProperty' is closest to worldLocation
                sectorID = neighboringSectorIDs.MinBy(aSectorID => Vector3.SqrMagnitude(worldLocation - GetSector(aSectorID).Position));
            }
        }

        if (sectorID == default(IntVector3)) {
            // worldLocation is outside the Universe. This is expensive
            Vector3 nearestCellToWorldLocation = _grid.NearestCell(worldLocation, RectGrid.CoordinateSystem.Grid);
            Vector3 nearestCellWithSectorID = default(Vector3);    // the nearest cell grid coordinates that has an associated SectorID
            float smallestSqrDistanceToCellWithSectorID = float.MaxValue;

            // all the cell grid coordinates that have an associated SectorID
            var allCellsWithSectorID = _cellGridCoordinatesToSectorIdLookup.Keys;
            float sqrDistance;
            foreach (var cellWithSectorID in allCellsWithSectorID) {
                if ((sqrDistance = Vector3.SqrMagnitude(nearestCellToWorldLocation - cellWithSectorID)) < smallestSqrDistanceToCellWithSectorID) {
                    nearestCellWithSectorID = cellWithSectorID;
                    smallestSqrDistanceToCellWithSectorID = sqrDistance;
                }
            }
            D.Assert(nearestCellWithSectorID != default(Vector3));
            sectorID = _cellGridCoordinatesToSectorIdLookup[nearestCellWithSectorID];
        }
        D.Assert(sectorID != default(IntVector3));

        return sectorID;
    }

    /// <summary>
    /// Gets the sector associated with this sectorID. 
    /// <remarks>Throws an error if no ASector is associated with this sectorID,
    /// which means default(IntVector3).</remarks>
    /// </summary>
    /// <param name="sectorID">The sectorID.</param>
    /// <returns></returns>
    public ASector GetSector(IntVector3 sectorID) {
        ASector sector;
        if (!_sectorIdToSectorLookup.TryGetValue(sectorID, out sector)) {
            D.Error("{0}: No Sector at {1}.", DebugName, sectorID.DebugName);
        }
        return sector;
    }

    /// <summary>
    /// Returns <c>true</c> if an ASector has been assigned containing this worldLocation, <c>false</c> otherwise.
    /// <remarks>Locations outside the universe and a very small percentage inside the universe (locations in FailedRimCells) 
    /// do not have an assigned ASector.</remarks>
    /// <remarks>7.13.18 No current known user.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <param name="sector">The sector.</param>
    /// <returns></returns>
    [Obsolete("Not currently used")]
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
    /// Gets the IDs of the neighbors to the sector indicated by centerSectorID.
    /// The centerSectorID is not included.
    /// <remarks>Can be empty if includeRim is false and centerSectorID has only RimSectors as neighbors.</remarks>
// <remarks>Provided as a convenience when you know you are in a Sector.
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
    /// Gets the local sectorIDs within the Universe that surrounds the GridCell containing worldLocation.
    /// If worldLocation is in a cell that has a valid SectorID, it is included.
    /// <remarks>Can be empty if includeRim is false and worldLocation has only RimSectors as neighbors.</remarks>
    /// <remarks>Throws an error if worldLocation is not within the universe.</remarks>
    /// <remarks>This method can be used to find the local SectorIDs surrounding worldLocation when worldLocation is
    /// in a FailedRimCell where there is no valid SectorID.</remarks>
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <param name="includeRim">if set to <c>true</c> [include rim].</param>
    /// <returns></returns>
    public IEnumerable<IntVector3> GetLocalSectorIDs(Vector3 worldLocation, bool includeRim = true) {
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
    /// Returns a SectorID that can be used as a Home Sector for a player based on playerSeparation.
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
                maxOriginToGridCellDistance = uSectorRadius * Constants.OneThird;
                break;
            case PlayerSeparation.Normal:
                minOriginToGridCellDistance = uSectorRadius * Constants.OneThird;
                maxOriginToGridCellDistance = uSectorRadius * Constants.TwoThirds;
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
    [Obsolete("Not currently used")]
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

    private CoreSector MakeSectorInstance(IntVector3 sectorID, Vector3 sectorCenterWorldLocation) {
        CoreSector sector = new CoreSector(sectorCenterWorldLocation);
        string sectorName = SectorNameFormat.Inject(typeof(CoreSector).Name, sectorID);
        SectorData data = new SectorData(sector, sectorID) {
            Name = sectorName
            //Density = 1F  the concept of space density is now attached to Topography, not Sectors. Density is relative and affects only drag
        };
        sector.Data = data;
        return sector;
    }

    private RimSector MakeRimSectorInstance(Vector3 sectorCenter, Vector3 positionPropValue, float radiusPropValue, IntVector3 sectorID) {
        RimSector sector = new RimSector(sectorCenter, positionPropValue, radiusPropValue);
        string sectorName = SectorNameFormat.Inject(typeof(RimSector).Name, sectorID);
        SectorData data = new SectorData(sector, sectorID) {
            Name = sectorName
            //Density = 1F  the concept of space density is now attached to Topography, not Sectors. Density is relative and affects only drag
        };
        sector.Data = data;
        return sector;
    }

    #region Try Find Rim Sector Position and Radius

    /// <summary>
    /// Min rim cell radius threshold used to determine whether radius is acceptable, aka large enough.
    /// <remarks>7.16.18 Current value is 16.5, aka 5.5 MaxFleetFormationRadius x buffer of 3.</remarks>
    /// </summary>
    private static float __RimCellRadiusThreshold = TempGameValues.MaxFleetFormationRadius * 3F;

    /// <summary>
    /// Lookup for Radius steps in percent of NormalCellRadius(600) for use in finding a maximum value for the RimCell Radius property.
    /// </summary>
    private IDictionary<UniverseSize, float[]> _rimCellRadiusStepsLookup = new Dictionary<UniverseSize, float[]>() {
        { UniverseSize.Tiny, new float[] { 0.92F, 0.89F, 0.57F, 0.31F, 0.11F } },
        { UniverseSize.Small, new float[] { 0.99F, 0.93F, 0.91F, 0.72F, 0.66F, 0.44F, 0.38F, 0.24F, 0.07F } },
        { UniverseSize.Normal, new float[] { 0.99F, 0.95F, 0.79F, 0.77F, 0.65F, 0.61F, 0.58F, 0.45F, 0.38F, 0.30F, 0.26F, 0.14F, 0.04F } },
        { UniverseSize.Large, new float[] { 0.99F, 0.96F, 0.95F, 0.82F, 0.77F, 0.75F, 0.72F, 0.70F, 0.64F, 0.61F, 0.58F,
                                            0.50F, 0.46F, 0.45F, 0.40F, 0.29F, 0.26F, 0.24F, 0.18F, 0.13F, 0.09F } },
        { UniverseSize.Enormous, new float[] { 0.99F, 0.97F, 0.96F, 0.89F, 0.88F, 0.87F, 0.83F, 0.81F, 0.80F, 0.79F, 0.78F, 0.74F, 0.72F,
                                               0.70F, 0.69F, 0.66F, 0.59F, 0.56F, 0.53F, 0.48F, 0.45F, 0.44F, 0.43F, 0.34F, 0.32F, 0.29F,
                                               0.26F, 0.19F, 0.18F, 0.14F, 0.12F, 0.09F, 0.07F, 0.05F } },
        { UniverseSize.Gigantic, new float[] { 0.99F, 0.97F, 0.91F, 0.90F, 0.84F, 0.83F, 0.82F, 0.79F, 0.78F, 0.77F, 0.76F, 0.74F, 0.72F,
                                               0.71F, 0.70F, 0.69F, 0.66F, 0.64F, 0.62F, 0.59F, 0.57F, 0.56F, 0.54F, 0.53F, 0.52F, 0.50F,
                                               0.46F, 0.45F, 0.42F, 0.39F, 0.37F, 0.36F, 0.34F, 0.31F, 0.28F, 0.27F, 0.25F, 0.22F, 0.19F,
                                               0.17F, 0.16F, 0.13F, 0.12F, 0.10F, 0.08F, 0.07F, 0.04F, 0.03F } },
    };

    private static float[] DebugRimCellRadiusSteps = {
        Constants.OneHundredPercent,
        0.99F,  0.98F, 0.97F, 0.96F, 0.95F, 0.94F, 0.93F, 0.92F, 0.91F, 0.90F,
        0.89F, 0.88F, 0.87F, 0.86F, 0.85F, 0.84F, 0.83F, 0.82F, 0.81F, 0.80F,
        0.79F, 0.78F, 0.77F, 0.76F, 0.75F, 0.74F, 0.73F, 0.72F, 0.71F, 0.70F,
        0.69F, 0.68F, 0.67F, 0.66F, 0.65F, 0.64F, 0.63F, 0.62F, 0.61F, 0.60F,
        0.59F, 0.58F, 0.57F, 0.56F, 0.55F, 0.54F, 0.53F, 0.52F, 0.51F, 0.50F,
        0.49F, 0.48F, 0.47F, 0.46F, 0.45F, 0.44F, 0.43F, 0.42F, 0.41F, 0.40F,
        0.39F, 0.38F, 0.37F, 0.36F, 0.35F, 0.34F, 0.33F, 0.32F, 0.31F, 0.30F,
        0.29F, 0.28F, 0.27F, 0.26F, 0.25F, 0.24F, 0.23F, 0.22F, 0.21F, 0.20F,
        0.19F, 0.18F, 0.17F, 0.16F, 0.15F, 0.14F, 0.13F, 0.12F, 0.11F, 0.10F,
        0.09F, 0.08F, 0.07F, 0.06F, 0.05F, 0.04F, 0.03F, 0.02F, 0.01F
    };

    /// <summary>
    /// Lookup for PositionSteps in absolute Units for use in finding a RimCell Position property value that maximizes 
    /// a cell's Radius property value.
    /// </summary>
    private IDictionary<UniverseSize, float[]> _rimCellPositionStepsLookup = new Dictionary<UniverseSize, float[]>() {
        { UniverseSize.Tiny, new float[] { 60, 80, 300, 530, 760 } },
        { UniverseSize.Small, new float[] { 0, 40, 50, 220, 250, 390, 550, 630, 714 } },
        { UniverseSize.Normal, new float[] { 0, 30, 140, 150, 250, 270, 360, 430, 460, 510, 622, 676, 740 } },
        { UniverseSize.Large, new float[] { 0, 20, 30, 100, 180, 200, 210, 230, 260, 280, 300, 340, 370, 450, 480,
                                            500, 530, 580, 660, 700, 750 } },
        { UniverseSize.Enormous, new float[] { 0, 20, 70, 80, 90, 130, 140, 150, 160, 190, 200, 210, 220, 240, 280,
                                               310, 330, 360, 380, 390, 420, 480, 490, 520, 530, 580, 590,
                                               612, 614, 642, 674, 710, 740, 770 } },
        { UniverseSize.Gigantic, new float[] { 0, 10, 50, 60, 90, 100, 140, 150, 170, 180, 190, 200, 210, 250, 260, 270, 290,
                                               300, 310, 320, 340, 350, 360, 380, 400, 410, 430, 440, 460, 470, 480, 490,
                                               500, 510, 540, 550, 560, 570, 610, 649, 660, 673, 680, 721, 730, 750, 775 } },
    };

    private static float[] DebugRimCellPositionSteps = {
        0F, 10F, 20F, 30F, 40F, 50F, 60F, 70F, 80F, 90F,
        100F, 110F, 120F, 130F, 140F, 150F, 160F, 170F, 180F, 190F,
        200F, 210F, 220F, 230F, 240F, 250F, 260F, 270F, 280F, 290F,
        300F, 310F, 320F, 330F, 340F, 350F, 360F, 370F, 380F, 390F,
        400F, 410F, 420F, 430F, 440F, 450F, 460F, 470F, 480F, 490F,
        500F, 510F, 520F, 530F, 540F, 550F, 560F, 570F, 580F, 590F,
        600F, 610F, 611F, 612F, 613F, 614F, 615F, 616F, 617F, 618F, 619F, 620F, 621F, 622F, 623F, 624F, 625F, 626F, 627F, 628F, 629F,
        630F, 635F, 640F, 641F, 642F, 643F, 644F, 645F, 646F, 647F, 648F, 649F, 650F, 651F, 660F,
        670F, 671F, 672F, 673F, 674F, 675F, 676F, 677F, 678F, 679F, 680F, 681F, 690F,
        700F, 710F, 711F, 712F, 713F, 714F, 715F, 716F, 717F, 718F, 719F, 720F, 721F, 730F, 740F, 750F, 760F,
        770F, 771F, 772F, 773F, 774F, 775F, 776F, 777F, 778F, 779F, 780F, 781F, 790F, 800F, 900F, 1000F
    };

    /// <summary>
    /// Returns <c>true</c> if finds acceptable Position and Radius Property values for the RimCell centered at
    /// <c>cellCenterWorldLocation</c>, false otherwise.
    /// <remarks>7.16.18 Uses the navigable radius of the universe rather than the full radius of the universe so that fleets
    /// stay a short distance away from the edge of the universe. This keeps the fleet's ships from accidentally moving outside the universe
    /// when they add their formation offset value to derive their actual worldLocation target.</remarks>
    /// </summary>
    /// <param name="cellCenterWorldLocation">The cell center world location.</param>
    /// <param name="universeSize">Size of the universe.</param>
    /// <param name="universeNavigableRadius">The radius of the portion of the universe that can be navigated by Fleets.</param>
    /// <param name="position">The resulting acceptable RimCell position property.</param>
    /// <param name="radius">The resulting acceptable RimCell radius property.</param>
    /// <returns></returns>
    private bool TryFindAcceptableRimCell(Vector3 cellCenterWorldLocation, UniverseSize universeSize, float universeNavigableRadius,
        out Vector3 position, out float radius) {
        float maxRadius = Constants.ZeroF;
        Vector3 maxRadiusPosition = Vector3.zero;
#pragma warning disable 0219
        int maxRadiusIndex = Constants.Zero;
        int maxRadiusPositionIndex = Constants.Zero;
#pragma warning restore 0219

        var normalCellRadius = ASector.NormalCellRadius;
        float universeNavRadiusSqrd = universeNavigableRadius * universeNavigableRadius;

        Vector3 directionToOrigin = (GameConstants.UniverseOrigin - cellCenterWorldLocation).normalized;

        float[] rimCellPositionSteps = _rimCellPositionStepsLookup[universeSize];
        float[] rimCellRadiusSteps = _rimCellRadiusStepsLookup[universeSize];
        if (_logDebugRimCellStepValues) {
            rimCellPositionSteps = DebugRimCellPositionSteps;
            rimCellRadiusSteps = DebugRimCellRadiusSteps;
        }

        for (int posStepIndex = 0; posStepIndex < rimCellPositionSteps.Length; posStepIndex++) {
            float positionDistanceStep = rimCellPositionSteps[posStepIndex];
            Vector3 candidatePosition = cellCenterWorldLocation + (directionToOrigin * positionDistanceStep);
            if (GameUtility.IsLocationContainedInNavigableUniverse(candidatePosition, universeNavRadiusSqrd)) {
                for (int radiusStepIndex = 0; radiusStepIndex < rimCellRadiusSteps.Length; radiusStepIndex++) {
                    float radiusPercentStep = rimCellRadiusSteps[radiusStepIndex];
                    float candidateRadius = normalCellRadius * radiusPercentStep;

                    if (GameUtility.IsSphereCompletelyContainedInNavigableUniverse(candidatePosition, candidateRadius, universeNavigableRadius)) {
                        if (MyMath.IsSphereCompletelyContainedWithinCube(cellCenterWorldLocation, normalCellRadius, candidatePosition, candidateRadius)) {
                            if (candidateRadius > maxRadius) {
                                maxRadius = candidateRadius;
                                maxRadiusIndex = radiusStepIndex;
                                maxRadiusPosition = candidatePosition;
                                maxRadiusPositionIndex = posStepIndex;
                            }
                        }
                    }
                }
            }
        }
        position = maxRadiusPosition;
        radius = maxRadius;
        bool isPositionFound = radius > Constants.ZeroF;
        bool isAcceptablePositionFound = isPositionFound;
        if (isPositionFound) {
            if (maxRadius < __RimCellRadiusThreshold) {
                D.Warn("Found RimSector position {0:0.} units from center with too small Radius {1:0.}, {2:P00} of normal. CellCenter = {3}.",
                    rimCellPositionSteps[maxRadiusPositionIndex], radius, rimCellRadiusSteps[maxRadiusIndex], cellCenterWorldLocation);
                isAcceptablePositionFound = false;    // 7.10.18 Handled this way to get reports on position
            }
            else {
                //D.Log("Found RimSector position {0:0.} units from center with acceptable Radius {1:0.}, {2:P00} of normal. CellCenter = {3}.",
                //    rimCellPositionSteps[maxRadiusPositionIndex], radius, rimCellRadiusSteps[maxRadiusIndex], cellCenterWorldLocation);
            }
        }

        if (_logDebugRimCellStepValues && isAcceptablePositionFound) {
            float debugPositionStep = DebugRimCellPositionSteps[maxRadiusPositionIndex];
            float debugRadiusStep = DebugRimCellRadiusSteps[maxRadiusIndex];
            __RecordRimCellStepValuesUtilized(debugPositionStep, debugRadiusStep);
        }
        return isAcceptablePositionFound;
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

    #region Debug

    private float __universeRadiusSqrd;

    #region Rim Cell Debug

    private float[] __FailedRimCellRadiusPercentSteps = new float[] { 0.05F, 0.04F, 0.03F, 0.025F, 0.02F, 0.015F, 0.01F, 0.005F };

    private float[] __FailedRimCellPositionSteps = new float[] { 1F, 2F, 3F, 4F, 5F, 6F, 7F, 8F, 9F, 10F, 11F, 12F, 13F, 14F, 15F,
        16F, 17F, 18F, 19F, 20F, 21F, 22F, 23F, 24F, 25F, 26F, 27F, 28F, 29F, 30F };

    /// <summary>
    /// Estimates the volume within the universe occupied by this FailedRimCell.
    /// <remarks>7.10.18 UniverseSize.Enormous has a few with 2 valid cell corners.</remarks>
    /// </summary>
    /// <param name="cellCenterWorldLocation">The cell center world location.</param>
    /// <param name="validCellCornerWorldLocations">The valid cell corner world locations.</param>
    /// <param name="universeRadius">The universe radius.</param>
    /// <returns></returns>
    private float __EstimateFailedRimCellUniverseVolume(Vector3 cellCenterWorldLocation, IEnumerable<Vector3> validCellCornerWorldLocations,
        float universeRadius) {
        float estNonNavigableUniverseVol = Constants.ZeroF;
        foreach (var cornerWorldLoc in validCellCornerWorldLocations) {
            estNonNavigableUniverseVol += __EstimateFailedRimCellUniverseVolume(cellCenterWorldLocation, cornerWorldLoc, universeRadius);
        }
        return estNonNavigableUniverseVol;
    }

    private float __EstimateFailedRimCellUniverseVolume(Vector3 cellCenterWorldLocation, Vector3 validCellCornerWorldLocation, float universeRadius) {
        GameUtility.__ValidateLocationContainedInUniverse(validCellCornerWorldLocation);

        float maxRadius = Constants.ZeroF;
#pragma warning disable 0219
        int maxRadiusIndex = Constants.Zero;
        int maxRadiusPositionIndex = Constants.Zero;
#pragma warning restore 0219

        Vector3 directionFromCellCornerToCenter = (cellCenterWorldLocation - validCellCornerWorldLocation).normalized;

        for (int posStepIndex = 0; posStepIndex < __FailedRimCellPositionSteps.Length; posStepIndex++) {
            float positionDistanceStep = __FailedRimCellPositionSteps[posStepIndex];
            Vector3 candidatePosition = validCellCornerWorldLocation + directionFromCellCornerToCenter * (positionDistanceStep);
            if (GameUtility.IsLocationContainedInUniverse(candidatePosition, __universeRadiusSqrd)) {
                for (int radiusStepIndex = 0; radiusStepIndex < __FailedRimCellRadiusPercentSteps.Length; radiusStepIndex++) {
                    float radiusPercentStep = __FailedRimCellRadiusPercentSteps[radiusStepIndex];
                    float candidateRadius = ASector.NormalCellRadius * radiusPercentStep;

                    if (GameUtility.IsSphereCompletelyContainedInUniverse(candidatePosition, candidateRadius, universeRadius)) {
                        if (MyMath.IsSphereCompletelyContainedWithinCube(cellCenterWorldLocation, ASector.NormalCellRadius, candidatePosition, candidateRadius)) {
                            if (candidateRadius > maxRadius) {
                                maxRadius = candidateRadius;
                                maxRadiusIndex = radiusStepIndex;
                                maxRadiusPositionIndex = posStepIndex;
                            }
                        }
                    }
                }
            }
        }
        float estClearRadiusInUniverse = 3F;    // estimated very small volume inside universe
        bool canVolEstimateBeCalculated = maxRadius > Constants.ZeroF;
        if (canVolEstimateBeCalculated) {
            if (__FailedRimCellRadiusPercentSteps[maxRadiusIndex] > 0.03F) {
                D.Warn("Found FailedRimCell position {0:0.} units from single valid corner {1} with Radius {2:0.}, {3:P00} of normal.",
                    __FailedRimCellPositionSteps[maxRadiusPositionIndex], validCellCornerWorldLocation, maxRadius,
                    __FailedRimCellRadiusPercentSteps[maxRadiusIndex], cellCenterWorldLocation);
            }
            else {
                //D.Log("Found FailedRimCell position {0:0.} units from single valid corner {1} with Radius {2:0.}, {3:P00} of normal.",
                //__FailedRimCellPositionSteps[maxRadiusPositionIndex], validCellCornerWorldLocation, maxRadius,
                //__FailedRimCellRadiusPercentSteps[maxRadiusIndex], cellCenterWorldLocation);
            }
            estClearRadiusInUniverse = maxRadius;
        }
        float estSphereToCubeVolMultiplier = 1.4504F;  // 1.909F normally so includes est of how much of additional cube vol inside Universe
        float estDrossCellVolumeInUniverse = Constants.FourThirds * Mathf.PI * Mathf.Pow(estClearRadiusInUniverse, 3F) * estSphereToCubeVolMultiplier;
        return estDrossCellVolumeInUniverse;
    }

    [Tooltip("Check to log and use Debug RimCell Step values to find position and radius values")]
    [SerializeField]
    private bool _logDebugRimCellStepValues = false;

    private HashSet<float> __rimCellPositionStepsUsed;
    private HashSet<float> __rimCellRadiusStepsUsed;

    private void __RecordRimCellStepValuesUtilized(float positionStep, float radiusStep) {
        D.Assert(_logDebugRimCellStepValues);
        __rimCellPositionStepsUsed = __rimCellPositionStepsUsed ?? new HashSet<float>();
        __rimCellRadiusStepsUsed = __rimCellRadiusStepsUsed ?? new HashSet<float>();
        __rimCellPositionStepsUsed.Add(positionStep);
        __rimCellRadiusStepsUsed.Add(radiusStep);
    }

    private void __LogRimCellStepValues(UniverseSize universeSize) {
        D.Assert(_logDebugRimCellStepValues);
        D.LogBold("{0}: RimCell step values follow for UniverseSize {1}.", DebugName, _gameMgr.GameSettings.UniverseSize.GetValueName());
        D.Log("RimCellPositionSteps utilized: {0}.", __rimCellPositionStepsUsed.OrderBy(ps => ps).Concatenate());
        D.Log("RimCellRadiusSteps utilized: {0}.", __rimCellRadiusStepsUsed.OrderByDescending(rs => rs).Concatenate());

        float[] positionSteps = _rimCellPositionStepsLookup[universeSize];
        foreach (var positionStep in __rimCellPositionStepsUsed) {
            if (!positionSteps.Contains(positionStep)) {
                D.Warn("{0}: {1}.{2}'s PositionSteps does not contain {3}.", DebugName, typeof(UniverseSize).Name,
                    universeSize.GetValueName(), positionStep);
            }
        }

        float[] radiusSteps = _rimCellRadiusStepsLookup[universeSize];
        foreach (var radiusStep in __rimCellRadiusStepsUsed) {
            if (!radiusSteps.Contains(radiusStep)) {
                D.Warn("{0}: {1}.{2}'s RadiusSteps does not contain {3}.", DebugName, typeof(UniverseSize).Name,
                    universeSize.GetValueName(), radiusStep);
            }
        }
    }

    private IList<int> __rimCellValidCornerCount;

    private void __RecordRimCornerCount(int validCornerCount) {
        D.Assert(_logDebugRimCellStepValues);
        //D.Log("{0}: Rim valid corners = {1}.", DebugName, validCornerCount);
        Utility.ValidateForRange(validCornerCount, _rimCellVertexThreshold, 7);
        __rimCellValidCornerCount = __rimCellValidCornerCount ?? new List<int>();
        // no need to clear as SectorGrid will always be new with new game
        __rimCellValidCornerCount.Add(validCornerCount);
    }

    private void __LogRimSectorCornerCount() {
        D.Assert(_logDebugRimCellStepValues);
        for (int vertexCount = _rimCellVertexThreshold; vertexCount < 8; vertexCount++) {
            int cellCount = __rimCellValidCornerCount.Where(cellValidCornerCount => cellValidCornerCount == vertexCount).Count();
            D.Log("{0}: Number of RimSectors with {1} valid corners = {2}.", DebugName, vertexCount, cellCount);
            // Small: 1:56, 2:48, 3:24, 4:96, 5:0, 6:24, 7:24, Total 272 Rim
        }
    }

    #endregion

    /// <summary>
    /// Debug version that returns the SectorID that contains worldLocation. If worldLocation
    /// is not within a cell with a SectorID, the invalid value default(IntVector3) is returned.
    /// </summary>
    /// <param name="worldLocation">The world location.</param>
    /// <returns></returns>
    public IntVector3 __GetSectorIDContaining(Vector3 worldLocation) {
        Vector3 closestGridCellToWorldLocation = _grid.NearestCell(worldLocation, RectGrid.CoordinateSystem.Grid);
        IntVector3 sectorID;
        if (_cellGridCoordinatesToSectorIdLookup.TryGetValue(closestGridCellToWorldLocation, out sectorID)) {
            return sectorID;
        }
        return default(IntVector3);
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

    #endregion

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
        /// A cell that can either be a Rim or a FailedRim.
        /// </summary>
        RimCandidate,

        /// <summary>
        /// A cell that is mostly inside the universe.
        /// </summary>
        Rim,
        /// <summary>
        /// A cell that was a candidate to become a RimSector but failed to 
        /// find a Position and Radius property that would allow it to operate as one.
        /// Accordingly, the vast majority of its volume is located outside the universe.
        /// </summary>
        FailedRim,
        /// <summary>
        /// A cell mostly outside the universe whose portion inside is not
        /// deemed sufficient to attempt to make it a RimSector.
        /// </summary>
        [Obsolete]
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

