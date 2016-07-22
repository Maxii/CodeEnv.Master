// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyPoolManager.cs
// My pooling manager singleton using Enums to interface with PathologicalGames.PoolManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using PathologicalGames;
using UnityEngine;

/// <summary>
/// My pooling manager singleton using Enums to interface with PathologicalGames.PoolManager.
/// </summary>
public class MyPoolManager : AMonoSingleton<MyPoolManager>, IMyPoolManager {

    private const string OrdnancePoolName = "Ordnance";

    private const string EffectsPoolName = "Effects";

    private const string FormationStationPoolName = "FormationStations";

    #region Initialization

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// </summary>
    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.MyPoolManager = Instance;
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        // TODO
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

    public FleetFormationStation Spawn(Vector3 location) {
        return Spawn(location, Quaternion.identity);
    }

    public FleetFormationStation Spawn(Vector3 location, Quaternion rotation) {
        return Spawn(location, rotation, FormationStationSpawnPool);
    }

    public FleetFormationStation Spawn(Vector3 location, Quaternion rotation, Transform parent) {
        Transform stationTransform = PoolManager.Pools[FormationStationPoolName].Spawn("FormationStation", location, rotation, parent);
        return stationTransform.GetComponent<FleetFormationStation>();
    }

    public void DespawnFormationStation(Transform stationTransform) {
        DespawnFormationStation(stationTransform, FormationStationSpawnPool);
    }

    public void DespawnFormationStation(Transform stationTransform, Transform parent) {
        PoolManager.Pools[FormationStationPoolName].Despawn(stationTransform, parent);
    }

    #endregion

    protected override void ExecutePriorToDestroy() {
        base.ExecutePriorToDestroy();
        // TODO tasks to execute before this extra copy of this persistent singleton is destroyed. Default does nothing.
    }

    #region Cleanup

    protected override void Cleanup() {
        References.MyPoolManager = null;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

