// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GeneralFactory.cs
// Singleton factory that makes miscellaneous instances that aren't made by either UnitFactory or SystemFactory.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton factory that makes miscellaneous instances that aren't made by either UnitFactory or SystemFactory.
/// </summary>
public class GeneralFactory : AGenericSingleton<GeneralFactory>, IGeneralFactory {
    // Note: no reason to dispose of _instance during scene transition as all its references persist across scenes

    private Orbiter _orbiterPrefab;
    private MovingOrbiter _movingOrbiterPrefab;
    private OrbiterForShips _orbiterForShipsPrefab;
    private MovingOrbiterForShips _movingOrbiterForShipsPrefab;

    private GeneralFactory() {
        Initialize();
    }

    protected override void Initialize() {
        References.GeneralFactory = this;
        _orbiterPrefab = RequiredPrefabs.Instance.orbiter;
        _movingOrbiterPrefab = RequiredPrefabs.Instance.movingOrbiter;
        _orbiterForShipsPrefab = RequiredPrefabs.Instance.orbiterForShips;
        _movingOrbiterForShipsPrefab = RequiredPrefabs.Instance.movingOrbiterForShips;
    }

    /// <summary>
    /// Makes the appropriate instance of IOrbiter parented to <c>parent</c> and not yet enabled.
    /// </summary>
    /// <param name="parent">The GameObject the IOrbiter should be parented too.</param>
    /// <param name="isParentMobile">if set to <c>true</c> [is parent mobile].</param>
    /// <param name="isForShips">if set to <c>true</c> [is for ships].</param>
    /// <param name="orbitPeriod">The orbit period.</param>
    /// <param name="orbiterName">Name of the orbiter.</param>
    /// <returns></returns>
    public IOrbiter MakeOrbiterInstance(GameObject parent, bool isParentMobile, bool isForShips, GameTimeDuration orbitPeriod, string orbiterName = "") {
        GameObject orbiterPrefab = null;
        if (isParentMobile) {
            orbiterPrefab = isForShips ? _movingOrbiterForShipsPrefab.gameObject : _movingOrbiterPrefab.gameObject;
        }
        else {
            orbiterPrefab = isForShips ? _orbiterForShipsPrefab.gameObject : _orbiterPrefab.gameObject;
        }
        string name = orbiterName.IsNullOrEmpty() ? orbiterPrefab.name : orbiterName;
        GameObject orbiterCloneGo = UnityUtility.AddChild(parent, orbiterPrefab);
        orbiterCloneGo.name = name;
        var orbiter = orbiterCloneGo.GetSafeInterface<IOrbiter>();
        orbiter.OrbitPeriod = orbitPeriod;
        return orbiter;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

