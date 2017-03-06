// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GamePoolManager.cs
// My pooling manager singleton using Enums to interface with PathologicalGames.PoolManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using PathologicalGames;
using UnityEngine;

/// <summary>
/// My pooling manager singleton using Enums to interface with PathologicalGames.PoolManager.
/// </summary>
public class GamePoolManager : AMonoSingleton<GamePoolManager>, IGamePoolManager {

    private const string OrdnancePoolName = "Ordnance";

    private const string EffectsPoolName = "Effects";

    private const string FormationStationPoolName = "FormationStations";

    private const string HighlightsPoolName = "Highlights";

    #region Editor Fields

    [Tooltip("The prefab for an explosion effect.")]
    [SerializeField]
    private Transform _explosionPrefab = null;

    [Tooltip("The prefab for a spherical highlight.")]
    [SerializeField]
    private Transform _sphericalHighlightPrefab = null;

    [Tooltip("The prefab for a FleetFormationStation.")]
    [SerializeField]
    private Transform _formationStationPrefab = null;

    [Tooltip("The prefab for Ordnance Beam.")]
    [SerializeField]
    private Transform _beamPrefab = null;

    [Tooltip("The prefab for Ordnance Projectile.")]
    [SerializeField]
    private Transform _projectilePrefab = null;

    [Tooltip("The prefab for Ordnance Missile.")]
    [SerializeField]
    private Transform _missilePrefab = null;


    [Tooltip("Show GamePoolManager debug logging.")]
    [SerializeField]
    private bool _showDebugLog = false;
    public bool ShowDebugLog {
        get { return _showDebugLog; }
        //set { _showDebugLog = value; }    // 2.17 17 UNCLEAR whether any value in dynamically changing ShowDebugLog during Runtime
    }

    [Tooltip("Show PoolManager's verbose debug logging.")]
    [SerializeField]
    private bool _showVerboseDebugLog = false;

    #endregion

    private string _debugName;
    public string DebugName {
        get {
            if (_debugName == null) {
                _debugName = typeof(GamePoolManager).Name;
            }
            return _debugName;
        }
    }

    #region Initialization

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// </summary>
    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.GamePoolManager = Instance;
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        __ValidatePrefabs();
    }

    /// <summary>
    /// Initializes this GamePoolManager.
    /// <remarks>Creates the PrefabPools from the prefab Editor fields, sets
    /// the number of preload instances to create and creates them.</remarks>
    /// </summary>
    /// <param name="gameSettings">The game settings.</param>
    public void Initialize(GameSettings gameSettings) {
        var debugCntls = DebugControls.Instance;
        int preloadAmt = debugCntls.FleetsAutoAttackAsDefault ? 3 : 2;
        CreatePrefabPool(EffectsPoolName, _explosionPrefab, preloadAmt);    // preload instances are created before delegate assigned
        PoolManager.Pools[EffectsPoolName].instantiateDelegates += InstantiateNewInstanceEventHandler;

        preloadAmt = 3;
        CreatePrefabPool(HighlightsPoolName, _sphericalHighlightPrefab, preloadAmt);
        PoolManager.Pools[HighlightsPoolName].instantiateDelegates += InstantiateNewInstanceEventHandler;

        // FleetFormationStations
        int fleetQty = GetFleetQty(gameSettings);
        preloadAmt = TempGameValues.MaxShipsPerFleet * fleetQty;
        CreatePrefabPool(FormationStationPoolName, _formationStationPrefab, preloadAmt);
        PoolManager.Pools[FormationStationPoolName].instantiateDelegates += InstantiateNewInstanceEventHandler;

        // Ordnance
        int maxShips = TempGameValues.MaxShipsPerFleet * fleetQty;
        int maxFacilities = TempGameValues.MaxFacilitiesPerBase * GetBaseQty(gameSettings);
        int maxElements = maxShips + maxFacilities;

        float avgBeamsOperatingPerElement = .01F;
        if (gameSettings.__UseDebugCreatorsOnly) {
            avgBeamsOperatingPerElement = 0.1F;
        }
        else if (debugCntls.FleetsAutoAttackAsDefault) {
            avgBeamsOperatingPerElement = 0.03F * debugCntls.MaxAttackingFleetsPerPlayer;
        }
        preloadAmt = Mathf.RoundToInt(avgBeamsOperatingPerElement * maxElements);
        CreatePrefabPool(OrdnancePoolName, _beamPrefab, preloadAmt);

        float avgProjectilesInFlightPerElement = 0.03F;
        if (gameSettings.__UseDebugCreatorsOnly) {
            avgProjectilesInFlightPerElement = 0.1F;
        }
        else if (debugCntls.FleetsAutoAttackAsDefault) {
            avgProjectilesInFlightPerElement = 0.06F * debugCntls.MaxAttackingFleetsPerPlayer;
        }
        preloadAmt = Mathf.RoundToInt(avgProjectilesInFlightPerElement * maxElements);
        CreatePrefabPool(OrdnancePoolName, _projectilePrefab, preloadAmt);

        float avgMissilesInFlightPerElement = 0.03F;
        if (gameSettings.__UseDebugCreatorsOnly) {
            avgMissilesInFlightPerElement = 0.1F;
        }
        else if (debugCntls.FleetsAutoAttackAsDefault) {
            avgMissilesInFlightPerElement = 0.10F * debugCntls.MaxAttackingFleetsPerPlayer;
        }
        preloadAmt = Mathf.RoundToInt(avgMissilesInFlightPerElement * maxElements);
        CreatePrefabPool(OrdnancePoolName, _missilePrefab, preloadAmt);
        PoolManager.Pools[OrdnancePoolName].instantiateDelegates += InstantiateNewInstanceEventHandler;
    }

    #endregion

    #region Ordnance

    public Transform OrdnanceSpawnPool { get { return PoolManager.Pools[OrdnancePoolName].transform; } }

    public Transform Spawn(WDVCategory ordnanceID, Vector3 location) {
        return Spawn(ordnanceID, location, Quaternion.identity);
    }

    public Transform Spawn(WDVCategory ordnanceID, Vector3 location, Quaternion rotation) {
        return Spawn(ordnanceID, location, rotation, OrdnanceSpawnPool);
    }

    public Transform Spawn(WDVCategory ordnanceID, Vector3 location, Quaternion rotation, Transform parent) {
        return PoolManager.Pools[OrdnancePoolName].Spawn(ordnanceID.GetValueName(), location, rotation, parent);
    }

    public void DespawnOrdnance(Transform ordnanceTransform) {
        DespawnOrdnance(ordnanceTransform, OrdnanceSpawnPool);
    }

    public void DespawnOrdnance(Transform ordnanceTransform, Transform parent) {
        PoolManager.Pools[OrdnancePoolName].Despawn(ordnanceTransform, parent);
    }

    #endregion

    #region Effects

    public Transform EffectsSpawnPool { get { return PoolManager.Pools[EffectsPoolName].transform; } }

    public IEffect Spawn(EffectID effectID, Vector3 location) {
        return Spawn(effectID, location, Quaternion.identity);
    }

    public IEffect Spawn(EffectID effectID, Vector3 location, Quaternion rotation) {
        return Spawn(effectID, location, rotation, EffectsSpawnPool);
    }

    public IEffect Spawn(EffectID effectID, Vector3 location, Quaternion rotation, Transform parent) {
        Transform effectTransform = PoolManager.Pools[EffectsPoolName].Spawn(effectID.GetValueName(), location, rotation, parent);
        return effectTransform.GetComponent<IEffect>();
    }

    public void DespawnEffect(Transform effectTransform) {
        DespawnEffect(effectTransform, EffectsSpawnPool);
    }

    public void DespawnEffect(Transform effectTransform, Transform parent) {
        PoolManager.Pools[EffectsPoolName].Despawn(effectTransform, parent);
    }

    #endregion

    #region Formation Stations

    public Transform FormationStationSpawnPool { get { return PoolManager.Pools[FormationStationPoolName].transform; } }

    public FleetFormationStation SpawnFormationStation(Vector3 location) {
        return SpawnFormationStation(location, Quaternion.identity);
    }

    public FleetFormationStation SpawnFormationStation(Vector3 location, Quaternion rotation) {
        return SpawnFormationStation(location, rotation, FormationStationSpawnPool);
    }

    public FleetFormationStation SpawnFormationStation(Vector3 location, Quaternion rotation, Transform parent) {
        Transform stationTransform = PoolManager.Pools[FormationStationPoolName].Spawn(_formationStationPrefab.name, location, rotation, parent);
        return stationTransform.GetComponent<FleetFormationStation>();
    }

    public void DespawnFormationStation(Transform stationTransform) {
        DespawnFormationStation(stationTransform, FormationStationSpawnPool);
    }

    public void DespawnFormationStation(Transform stationTransform, Transform parent) {
        PoolManager.Pools[FormationStationPoolName].Despawn(stationTransform, parent);
    }

    #endregion

    #region Highlights

    public Transform HighlightsSpawnPool { get { return PoolManager.Pools[HighlightsPoolName].transform; } }

    public ISphericalHighlight SpawnHighlight(Vector3 location) {
        return SpawnHighlight(location, Quaternion.identity);
    }

    public ISphericalHighlight SpawnHighlight(Vector3 location, Quaternion rotation) {
        return SpawnHighlight(location, rotation, HighlightsSpawnPool);
    }

    public ISphericalHighlight SpawnHighlight(Vector3 location, Quaternion rotation, Transform parent) {
        Transform highlightTransform = PoolManager.Pools[HighlightsPoolName].Spawn(_sphericalHighlightPrefab.name, location, rotation, parent);
        return highlightTransform.GetComponent<PooledSphericalHighlight>();
    }

    public void DespawnHighlight(Transform highlightTransform) {
        DespawnHighlight(highlightTransform, HighlightsSpawnPool);
    }

    public void DespawnHighlight(Transform highlightTransform, Transform parent) {
        PoolManager.Pools[HighlightsPoolName].Despawn(highlightTransform, parent);
    }

    #endregion

    private void CreatePrefabPool(string spawnPoolName, Transform prefab, int preloadAmt) {
        PrefabPool prefabPool = new PrefabPool(prefab);
        prefabPool.preloadAmount = preloadAmt;
        prefabPool.preloadTime = false;
        prefabPool.limitInstances = false;
        prefabPool.cullDespawned = false;

        SpawnPool spawnPool = PoolManager.Pools[spawnPoolName];
        spawnPool.logMessages = _showVerboseDebugLog;
        spawnPool.CreatePrefabPool(prefabPool);
        D.Log(ShowDebugLog, "{0} has created {1} instances of {2}.", DebugName, prefabPool.totalCount, prefab.name);
    }

    private int GetFleetQty(GameSettings gameSettings) {
        if (gameSettings.__UseDebugCreatorsOnly) {
            return 3;   // HACK
        }
        int userFleetQty = gameSettings.UserStartLevel.FleetStartQty();
        int aiFleetQty = 0;
        var aiStartLevels = gameSettings.AIPlayersStartLevels;
        foreach (var aiStartLevel in aiStartLevels) {
            aiFleetQty += aiStartLevel.FleetStartQty();
        }
        if (gameSettings.__DeployAdditionalAICreators) {
            int aiPlayerQty = aiStartLevels.Length;
            aiFleetQty += aiPlayerQty * gameSettings.__AdditionalFleetCreatorQty;
        }
        return userFleetQty + aiFleetQty;
    }

    private int GetBaseQty(GameSettings gameSettings) {
        if (gameSettings.__UseDebugCreatorsOnly) {
            return 4;   // HACK
        }
        int userBaseQty = gameSettings.UserStartLevel.StarbaseStartQty() + gameSettings.UserStartLevel.SettlementStartQty();
        int aiBaseQty = 0;
        var aiStartLevels = gameSettings.AIPlayersStartLevels;
        foreach (var aiStartLevel in aiStartLevels) {
            aiBaseQty += aiStartLevel.StarbaseStartQty() + aiStartLevel.SettlementStartQty();
        }
        if (gameSettings.__DeployAdditionalAICreators) {
            int aiPlayerQty = aiStartLevels.Length;
            aiBaseQty += aiPlayerQty * gameSettings.__AdditionalStarbaseCreatorQty + aiPlayerQty * gameSettings.__AdditionalSettlementCreatorQty;
        }
        return userBaseQty + aiBaseQty;
    }

    #region Event and Property Change Handlers

    /// <summary>
    /// Handles the creation of new prefab instances during runtime. Done this way to allow
    /// counting of any new instances required beyond initial preload amount.
    /// </summary>
    /// <param name="prefab">The prefab.</param>
    /// <param name="pos">The position.</param>
    /// <param name="rot">The rot.</param>
    /// <returns></returns>
    private GameObject InstantiateNewInstanceEventHandler(GameObject prefab, Vector3 pos, Quaternion rot) {
        __IncrementAdditionalInstancesCreatedCount(prefab);
        return GameObject.Instantiate(prefab, pos, rot);
    }

    void OnValidate() {
        CheckValuesForChange();
    }

    #endregion

    #region Value Change Checking

    private void CheckValuesForChange() {
        CheckShowDebugLog();
    }

    private bool _showDebugLogPrev;
    private void CheckShowDebugLog() {
        if (_showDebugLog != _showDebugLogPrev) {
            _showDebugLogPrev = _showDebugLog;
            HandleShowDebugLogChanged();
        }
    }

    #endregion

    private void HandleShowDebugLogChanged() {
        // UNDONE not clear whether any value in dynamically changing ShowDebugLog during Runtime
    }

    #region Cleanup

    protected override void __CleanupOnApplicationQuit() {
        base.__CleanupOnApplicationQuit();
        __ReportAdditionalInstancesCreated();
    }

    protected override void Cleanup() {
        Unsubscribe();
        References.GamePoolManager = null;
    }

    private void Unsubscribe() {
        PoolManager.Pools[EffectsPoolName].instantiateDelegates -= InstantiateNewInstanceEventHandler;
        PoolManager.Pools[HighlightsPoolName].instantiateDelegates -= InstantiateNewInstanceEventHandler;
        PoolManager.Pools[FormationStationPoolName].instantiateDelegates -= InstantiateNewInstanceEventHandler;
        PoolManager.Pools[OrdnancePoolName].instantiateDelegates -= InstantiateNewInstanceEventHandler;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug

    private IDictionary<string, int> __additionalInstancesCreatedLookup;
    private IDictionary<string, int> __preloadedInstanceQtyLookup;

    private void __IncrementAdditionalInstancesCreatedCount(GameObject prefab) {
        __additionalInstancesCreatedLookup = __additionalInstancesCreatedLookup ?? new Dictionary<string, int>(6);
        __preloadedInstanceQtyLookup = __preloadedInstanceQtyLookup ?? __InitializePreloadedInstanceQtyLookup();
        string prefabName = prefab.name;
        if (!__additionalInstancesCreatedLookup.ContainsKey(prefabName)) {
            __additionalInstancesCreatedLookup.Add(prefabName, 0);
        }
        __additionalInstancesCreatedLookup[prefabName]++;
    }

    private void __ReportAdditionalInstancesCreated() {
        if (__additionalInstancesCreatedLookup != null) {
            foreach (var prefabNameKey in __additionalInstancesCreatedLookup.Keys) {
                int newInstanceCount = __additionalInstancesCreatedLookup[prefabNameKey];
                if (newInstanceCount > 3) {
                    D.Warn("{0} had to create {1} new instances of {2} over the preload amount {3} during runtime.",
                        DebugName, newInstanceCount, prefabNameKey, __preloadedInstanceQtyLookup[prefabNameKey]);
                }
            }
        }
        else {
            D.Log(ShowDebugLog, "{0} didn't have to create any additional pooled instances.", DebugName);
        }
    }

    private IDictionary<string, int> __InitializePreloadedInstanceQtyLookup() {
        return new Dictionary<string, int>(6) {
            { _explosionPrefab.name, __GetPreloadedInstanceQty(EffectsPoolName, _explosionPrefab) },
            { _sphericalHighlightPrefab.name, __GetPreloadedInstanceQty(HighlightsPoolName, _sphericalHighlightPrefab) },
            { _formationStationPrefab.name, __GetPreloadedInstanceQty(FormationStationPoolName, _formationStationPrefab) },
            { _beamPrefab.name, __GetPreloadedInstanceQty(OrdnancePoolName, _beamPrefab) },
            { _missilePrefab.name, __GetPreloadedInstanceQty(OrdnancePoolName, _missilePrefab) },
            { _projectilePrefab.name, __GetPreloadedInstanceQty(OrdnancePoolName, _projectilePrefab) }
        };
    }

    private int __GetPreloadedInstanceQty(string spawnPoolName, Transform prefab) {
        SpawnPool spawnPool = PoolManager.Pools[spawnPoolName];
        return spawnPool.GetPrefabPool(prefab).preloadAmount;
    }

    private void __ValidatePrefabs() {
        D.AssertNotNull(_explosionPrefab);
        D.AssertNotNull(_sphericalHighlightPrefab);
        D.AssertNotNull(_explosionPrefab);
        D.AssertNotNull(_beamPrefab);
        D.AssertNotNull(_projectilePrefab);
        D.AssertNotNull(_missilePrefab);
    }

    #endregion

}

