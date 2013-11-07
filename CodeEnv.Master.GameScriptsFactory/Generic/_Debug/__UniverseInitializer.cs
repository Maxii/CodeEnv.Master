// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: __UniverseInitializer.cs
// Initializes Data for all items in the universe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Initializes Data for all items in the universe.
/// </summary>
public class __UniverseInitializer : AMonoBehaviourBase, IDisposable {

    private static IList<Vector3> _obstacleLocations;
    public static IList<Vector3> ObstacleLocations { get { return _obstacleLocations; } }

    private GameManager _gameMgr;
    private IList<IDisposable> _subscribers;
    private FleetItem[] _fleets;
    private ShipItem[] _ships;
    private Item[] _planetoids;
    private Item[] _stars;
    private SystemItem[] _systems;
    private SettlementItem[] _settlements;
    private Item _universeCenter;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        Subscribe();
        AcquireGameObjectsRequiringDataToInitialize();
    }

    private void AcquireGameObjectsRequiringDataToInitialize() {
        _fleets = gameObject.GetSafeMonoBehaviourComponentsInChildren<FleetItem>();
        _ships = gameObject.GetSafeMonoBehaviourComponentsInChildren<ShipItem>();
        // TODO I'll need to pick the ships under each fleet and then add those ships to each fleet when initializing
        _systems = gameObject.GetSafeMonoBehaviourComponentsInChildren<SystemItem>();
        _settlements = gameObject.GetSafeMonoBehaviourComponentsInChildren<SettlementItem>();
        Item[] celestialObjects = gameObject.GetSafeMonoBehaviourComponentsInChildren<Item>()
            .Except(_fleets).Except(_ships).Except(_systems).Except(_settlements).ToArray();
        _stars = celestialObjects.Where(co => co.gameObject.GetComponent<StarView>() != null).ToArray();
        _planetoids = celestialObjects.Where(co => co.gameObject.GetComponent<MovingView>() != null).ToArray();
        _universeCenter = celestialObjects.Single(co => co.gameObject.GetComponent<UniverseCenterView>() != null);

        _obstacleLocations = _systems.Select(s => s.transform.position).ToList();
        _obstacleLocations.Add(_universeCenter.transform.position);
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.GameState, OnGameStateChanged));
    }

    private void OnGameStateChanged() {
        if (_gameMgr.GameState == GameState.Waiting) {
            InitializeGameObjectData();
        }
    }

    private void InitializeGameObjectData() {
        InitializeSystems();    // systems before settements and celestial objects as they need the system name
        InitializeSettlements();
        InitializeStars();
        InitializePlanetoids();
        InitializeShips();  // ships before fleet as fleet data needs ships
        InitializeFleet();
        InitializeCenter();
        InitializePlayerIntelLevel();
    }

    private void InitializeSystems() {
        int sysNumber = 0;
        foreach (SystemItem system in _systems) {
            Transform systemTransform = system.transform;
            string systemName = "System_" + sysNumber;
            SystemData data = new SystemData(systemTransform, systemName) {
                // there is no parentName for a System
                LastHumanPlayerIntelDate = new GameDate(),
                Capacity = 25,
                Resources = new OpeYield(3.1F, 2.0F, 4.8F),
                SpecialResources = new XYield(XResource.Special_1, 0.3F),
            };
            system.Data = data;
            sysNumber++;
        }
    }

    private void InitializeSettlements() {
        foreach (Item settlement in _settlements) {
            SystemItem system = settlement.gameObject.GetSafeMonoBehaviourComponentInParents<SystemItem>();
            string systemName = system.Data.Name;
            string settlementName = systemName + " Settlement";
            SettlementData data = new SettlementData(settlement.transform, settlementName, 50F, systemName) {
                SettlementSize = SettlementSize.City,
                Population = 100,
                CapacityUsed = 10,
                ResourcesUsed = new OpeYield(1.3F, 0.5F, 2.4F),
                SpecialResourcesUsed = new XYield(new XYield.XResourceValuePair(XResource.Special_1, 0.2F)),
                Strength = new CombatStrength(1f, 2f, 3f, 4f, 5f, 6f),
                CurrentHitPoints = 38F,
                Owner = GameManager.Instance.HumanPlayer
            };
            settlement.Data = data;
            // PlayerIntelLevel is determined by the IntelLevel of the System
            system.Data.Settlement = data;  // TODO temporary as the SystemHud shows settlement info this way
        }
    }

    private void InitializeStars() {
        foreach (Item star in _stars) {
            SystemItem system = star.gameObject.GetSafeMonoBehaviourComponentInParents<SystemItem>();
            string systemName = system.Data.Name;
            string starName = systemName + " Star";
            Data data = new Data(star.transform, starName, 1000000F, systemName) {
                LastHumanPlayerIntelDate = new GameDate()
            };
            star.Data = data;
            // PlayerIntelLevel is determined by the IntelLevel of the System
        }
    }

    private void InitializePlanetoids() {
        int planetoidNumber = 0;
        foreach (Item planetoid in _planetoids) {
            SystemItem system = planetoid.gameObject.GetSafeMonoBehaviourComponentInParents<SystemItem>();
            string systemName = system.Data.Name;
            string planetName = "Planet_" + planetoidNumber;
            Data data = new Data(planetoid.transform, planetName, 100000F, systemName) {
                LastHumanPlayerIntelDate = new GameDate()
            };
            planetoid.Data = data;
            planetoidNumber++;
            // PlayerIntelLevel is determined by the IntelLevel of the System
        }
    }

    private void InitializeShips() {
        int shipNumber = 0;
        foreach (ShipItem ship in _ships) {
            string shipName = "Ship_" + shipNumber;
            ShipData data = new ShipData(ship.transform, shipName, 50F) {
                // Ship's optionalParentName gets set when it gets attached to a fleet
                Hull = ShipHull.Destroyer,
                Strength = new CombatStrength(1f, 2f, 3f, 4f, 5f, 6f),
                LastHumanPlayerIntelDate = new GameDate(),
                CurrentHitPoints = 38F,
                Owner = GameManager.Instance.HumanPlayer,
                MaxTurnRate = 2.0F,
                RequestedHeading = ship.transform.forward
            };
            data.MaxThrust = data.Mass * data.Drag * 2F;    // MaxThrust = MaxSpeed * Mass * Drag
            ship.Data = data;
            shipNumber++;
            // PlayerIntelLevel is determined by the IntelLevel of the Fleet
        }
    }

    private void InitializeFleet() {
        if (!_fleets.IsNullOrEmpty()) {
            FleetItem fleetMgr = _fleets[0];
            FleetData data = new FleetData(fleetMgr.transform, "Borg Fleet") {
                // there is no parentName for a fleet
                LastHumanPlayerIntelDate = new GameDate()
            };

            foreach (var ship in _ships) {
                data.AddShip(ship.Data);
            }
            fleetMgr.Data = data;
        }
    }

    private void InitializeCenter() {
        if (_universeCenter) {
            Data data = new Data(_universeCenter.transform, "UniverseCenter", Mathf.Infinity);
            _universeCenter.Data = data;
        }
    }

    /// <summary>
    /// PlayerIntelLevel changes immediately propogate through COs and Ships so initialize this last in case the change pulls Data.
    /// </summary>
    private void InitializePlayerIntelLevel() {
        _systems.ForAll<SystemItem>(sys => sys.gameObject.GetSafeInterface<ISystemViewable>().PlayerIntelLevel
            = Enums<IntelLevel>.GetRandom(excludeDefault: true));
        _fleets.ForAll<FleetItem>(f => f.gameObject.GetSafeInterface<IFleetViewable>().PlayerIntelLevel
            = RandomExtended<IntelLevel>.Choice(Enums<IntelLevel>.GetValues().Except(default(IntelLevel), IntelLevel.Nil).ToArray()));
        _universeCenter.gameObject.GetSafeInterface<IViewable>().PlayerIntelLevel = Enums<IntelLevel>.GetRandom(excludeDefault: true);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable
    [DoNotSerialize]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

}

