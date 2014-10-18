// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RequiredPrefabs.cs
// Singleton container that holds prefabs that are gauranteed to be used in the GameScene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Singleton container that holds prefabs that are gauranteed to be used in the GameScene.
/// </summary>
/// <remarks>
/// I think these are a real reference to the prefab in the Project view, not a separate instance
/// clone of the Prefab in the startScene. As such, they must be Instantiated before use.
/// </remarks>
public class RequiredPrefabs : AMonoBaseSingleton<RequiredPrefabs> {

    /// <summary>
    /// A generic prefab for labels that track the world object they are parented too.
    /// They need to have specific scripts added after instantiation to function.
    /// </summary>
    public GameObject worldTrackingLabel;
    /// <summary>
    /// A generic prefab for sprites that track the world object they are parented too.
    /// They need to have specific scripts added after instantiation to function.
    /// </summary>
    public GameObject worldTrackingSprite;

    /// <summary>
    /// A specific prefab for labels that track world objects from the UI layer.
    /// Includes all scripts.
    /// </summary>
    public UITrackingLabel uiTrackingLabel;
    /// <summary>
    /// A specific prefab for sprites that track world objects from the UI layer.
    /// Includes all scripts.
    /// </summary>
    public UITrackingSprite uiTrackingSprite;

    public SphereCollider universeEdge;
    public Transform cameraDummyTarget;
    public SectorModel sector;

    public Orbiter orbiter;
    public MovingOrbiter movingOrbiter;
    public OrbiterForShips orbiterForShips;
    public MovingOrbiterForShips movingOrbiterForShips;

    public FleetCmdView_AI aiFleetCmd;
    public ShipView[] aiShips;

    public FleetCmdView_Player humanFleetCmd;
    public ShipView_Player[] humanShips;

    public SettlementCmdView_AI aiSettlementCmd;
    public SettlementCmdView_Player humanSettlementCmd;

    public StarbaseCmdView_AI aiStarbaseCmd;
    public StarbaseCmdView_Player humanStarbaseCmd;

    public FacilityModel[] facilities;

    public SystemModel system;   // without the star and settlement
    public StarModel[] stars;
    //public StarModelView[] testStars;
    public PlanetModel[] planets;   // no bundled moons or orbiters
    public MoonModel[] moons;       // no orbiters

    public WeaponRangeMonitor weaponRangeMonitor;
    public FormationStation formationStation;

    protected override void Awake() {
        base.Awake();
        if (TryDestroyExtraCopies()) {
            return;
        }
    }

    /// <summary>
    /// Ensures that no matter how many scenes this Object is
    /// in (having one dedicated to each sscene may be useful for testing) there's only ever one copy
    /// in memory if you make a scene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    private bool TryDestroyExtraCopies() {
        if (_instance && _instance != this) {
            D.Log("{0}_{1} found as extra. Initiating destruction sequence.".Inject(this.name, InstanceID));
            Destroy(gameObject);
            return true;
        }
        else {
            DontDestroyOnLoad(gameObject);
            _instance = this;
            return false;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


