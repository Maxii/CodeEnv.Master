// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseBuilder.cs
// Principal builder of the Universe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Principal builder of the Universe.
/// </summary>
public class UniverseBuilder : AMonoSingleton<UniverseBuilder> {

    // 8.20.16 SystemDensity editor control moved to DebugControls

    protected override bool IsRootGameObject { get { return true; } }

    public UniverseCenterItem UniverseCenter { get; private set; }

    private GameManager _gameMgr;
    private SectorGrid _sectorGrid;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _gameMgr = GameManager.Instance;
        _sectorGrid = SectorGrid.Instance;
        Subscribe();
    }

    private void Subscribe() {
        _gameMgr.gameStateChanged += GameStateChangedEventHandler;
    }

    #region Event and Property Change Handlers

    private void GameStateChangedEventHandler(object sender, EventArgs e) {
        LogEvent();
        GameState gameState = _gameMgr.CurrentState;
        if (gameState == GameState.Building) {
            HandleGameStateBegun_Building();
        }
        if (gameState == GameState.DeployingSystemCreators) {
            HandleGameStateBegun_DeploySystemCreators();
        }
        if (gameState == GameState.BuildingSystems) {
            HandleGameStateBegun_BuildSystems();
        }
        if (gameState == GameState.PreparingToRun) {
            HandleGameStateBegun_PrepareForRunning();
        }
        if (gameState == GameState.Running) {
            CommenceUCenterOperations();
        }
    }

    #endregion

    private void HandleGameStateBegun_Building() {
        _gameMgr.RecordGameStateProgressionReadiness(Instance, GameState.Building, isReady: false);
        _sectorGrid.BuildSectors();
        _gameMgr.RecordGameStateProgressionReadiness(Instance, GameState.Building, isReady: true);
    }

    private void HandleGameStateBegun_DeploySystemCreators() {
        _gameMgr.RecordGameStateProgressionReadiness(this, GameState.DeployingSystemCreators, isReady: false);

        DeploySystemCreators();

        _gameMgr.RecordGameStateProgressionReadiness(this, GameState.DeployingSystemCreators, isReady: true);
        // SystemCreators build, initialize and deploy their system during this state, then they allow the state to progress
    }

    private void HandleGameStateBegun_BuildSystems() {
        _gameMgr.RecordGameStateProgressionReadiness(this, GameState.BuildingSystems, isReady: false);
        InitializeUniverseCenter();
        _gameMgr.RecordGameStateProgressionReadiness(this, GameState.BuildingSystems, isReady: true);
        // SystemCreators build, initialize and deploy their system during this state, then they allow the state to progress
    }

    private void HandleGameStateBegun_PrepareForRunning() {
        _gameMgr.RecordGameStateProgressionReadiness(this, GameState.PreparingToRun, isReady: false);
        SectorGrid.Instance.CommenceSectorOperations();
        _gameMgr.RecordGameStateProgressionReadiness(this, GameState.PreparingToRun, isReady: true);
    }

    #region System Creator Deployment

    private void DeploySystemCreators() {
        __RecordDurationStartTime();

        var deployableSectorIndices = _sectorGrid.AllSectorIndexes.Where(index => !_sectorGrid.GetSector(index).IsOnPeriphery);
        int allowedSystemQty = CalcUniverseSystemsQty(deployableSectorIndices.Count());
        var sectorIndicesAlreadyOccupiedBySystemCreators = HandleManuallyDeployedSystemCreators(allowedSystemQty);
        var sectorIndicesOccupiedByStarbaseCreators = gameObject.GetComponentsInChildren<DebugStarbaseCreator>().Select(creator => creator.SectorIndex);
        var unoccupiedDeployableSectorIndices = deployableSectorIndices.Except(sectorIndicesAlreadyOccupiedBySystemCreators).Except(sectorIndicesOccupiedByStarbaseCreators);
        int additionalSystemQtyToDeploy = allowedSystemQty - sectorIndicesAlreadyOccupiedBySystemCreators.Count();
        int additionalQtyDeployed = DeployAdditionalSystemCreators(additionalSystemQtyToDeploy, unoccupiedDeployableSectorIndices);

        __LogDuration("{0}.DeploySystemCreators".Inject(GetType().Name));
        D.Log("{0} built and deployed {1} additional {2}s.", GetType().Name, additionalQtyDeployed, typeof(SystemCreator).Name);
    }

    private int CalcUniverseSystemsQty(int nonPeripheralSectorQty) {
        SystemDensity systemDensity = DebugControls.Instance.SystemDensity;
        if (systemDensity == SystemDensity.Existing_Debug) {
            return gameObject.GetComponentsInChildren<SystemCreator>().Count();
        }
        return Mathf.FloorToInt(nonPeripheralSectorQty * systemDensity.SystemsPerSector());
    }

    /// <summary>
    /// Handles any manually deployed system creators already present including accounting for their 
    /// already assigned name, if any. Returns the sector indices where creators are already deployed.
    /// </summary>
    /// <returns></returns>
    private IEnumerable<IntVector3> HandleManuallyDeployedSystemCreators(int allowedQty) {
        var systemNameFactory = SystemNameFactory.Instance;
        // need to have already manually deployed all named creators
        var orderedCreators = gameObject.GetSafeComponentsInChildren<SystemCreator>().OrderBy(sysCreator => sysCreator.IsSystemNamed);

        var allowedCreators = orderedCreators.Take(allowedQty);
        var namedAllowedCreators = allowedCreators.Where(c => c.IsSystemNamed);
        namedAllowedCreators.ForAll(c => systemNameFactory.MarkNameAsUsed(c.SystemName));

        var unallowedCreators = orderedCreators.Except(allowedCreators);
        unallowedCreators.ForAll(c => GameUtility.Destroy(c.gameObject));

        return allowedCreators.Select(c => c.SectorIndex);
    }

    private int DeployAdditionalSystemCreators(int qty, IEnumerable<IntVector3> unoccupiedDeployableSectorIndices) {
        Utility.ValidateNotNegative(qty);
        if (qty == Constants.Zero) {
            return Constants.Zero;
        }

        SystemFactory factory = SystemFactory.Instance;
        var unoccupiedSectorIndices = unoccupiedDeployableSectorIndices.Shuffle();
        var sectorIndicesToDeployTo = unoccupiedSectorIndices.Take(qty);
        var sectorPositionsToDeployTo = sectorIndicesToDeployTo.Select(index => _sectorGrid.GetSectorPosition(index));
        sectorPositionsToDeployTo.ForAll(position => factory.MakeCreatorInstance(position));
        return sectorPositionsToDeployTo.Count();
    }

    #endregion

    private void InitializeUniverseCenter() {
        UniverseCenter = GetComponentInChildren<UniverseCenterItem>();
        if (UniverseCenter != null) {
            float radius = TempGameValues.UniverseCenterRadius;
            float lowOrbitRadius = radius + 5F;
            FocusableItemCameraStat cameraStat = __MakeCameraStat(radius, lowOrbitRadius);
            UniverseCenter.Name = "UniverseCenter";
            UniverseCenterData data = new UniverseCenterData(UniverseCenter, radius, lowOrbitRadius);
            UniverseCenter.CameraStat = cameraStat;
            UniverseCenter.Data = data;
            // UC will be enabled when CommenceOperations() called
        }
    }

    private void CommenceUCenterOperations() {
        if (UniverseCenter != null) {
            UniverseCenter.FinalInitialize();
            UniverseCenter.CommenceOperations();
        }
    }

    private FocusableItemCameraStat __MakeCameraStat(float radius, float lowOrbitRadius) {
        float minViewDistance = radius + 1F;
        float highOrbitRadius = lowOrbitRadius + TempGameValues.ShipCloseOrbitSlotDepth;
        float optViewDistance = highOrbitRadius + 1F;
        return new FocusableItemCameraStat(minViewDistance, optViewDistance, fov: 80F);
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _gameMgr.gameStateChanged -= GameStateChangedEventHandler;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

