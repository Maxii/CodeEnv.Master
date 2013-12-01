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
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Initializes Data for all items in the universe.
/// </summary>
public class __UniverseInitializer : AMonoBase, IDisposable {

    //private static IList<Vector3> _obstacleLocations;
    //public static IList<Vector3> ObstacleLocations { get { return _obstacleLocations; } }

    private GameManager _gameMgr;
    private IList<IDisposable> _subscribers;

    //private FleetManager[] _fleetMgrs;
    //private SystemManager[] _systemMgrs;

    //private SystemItem[] _systems;
    //private SettlementItem[] _settlements;
    //private Item[] _stars;
    //private Item[] _planetoids;
    private Item _universeCenter;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        Subscribe();
        AcquireGameObjectsRequiringDataToInitialize();
    }

    private void AcquireGameObjectsRequiringDataToInitialize() {
        //_fleetMgrs = gameObject.GetSafeMonoBehaviourComponentsInChildren<FleetManager>();
        // Item[] excludedFleetElements = _fleetMgrs.Where(fMgr => fMgr.transform.childCount > 0)
        // .SelectMany(fm => fm.gameObject.GetSafeMonoBehaviourComponentsInChildren<Item>()).ToArray();

        //_systemMgrs = gameObject.GetSafeMonoBehaviourComponentsInChildren<SystemManager>();
        //Item[] excludedSystemElements = _systemMgrs.Where(sMgr => sMgr.transform.childCount > 0)
        // .SelectMany(sm => sm.gameObject.GetSafeMonoBehaviourComponentsInChildren<PlanetoidItem>()).ToArray();

        //_systems = gameObject.GetSafeMonoBehaviourComponentsInChildren<SystemItem>();
        //_settlements = gameObject.GetSafeMonoBehaviourComponentsInChildren<SettlementItem>();

        //Item[] celestialObjects = gameObject.GetSafeMonoBehaviourComponentsInChildren<Item>()
        //.Except(excludedSystemElements).Except(_settlements).Except(excludedFleetElements).ToArray();
        //    Item[] celestialObjects = gameObject.GetSafeMonoBehaviourComponentsInChildren<Item>()
        //.Except(_systems).Except(_settlements).Except(excludedFleetElements).ToArray();

        //_stars = celestialObjects.Where(co => co.gameObject.GetComponent<StarView>() != null).ToArray();
        //_planetoids = celestialObjects.Where(co => co.gameObject.GetComponent<MovingView>() != null).ToArray();
        //_universeCenter = celestialObjects.Single(co => co.gameObject.GetComponent<UniverseCenterView>() != null);
        _universeCenter = gameObject.GetSafeMonoBehaviourComponentInChildren<UniverseCenterView>().gameObject.GetSafeMonoBehaviourComponent<Item>();

        //_obstacleLocations = _systems.Select(s => s.transform.position).ToList();
        //_obstacleLocations.Add(_universeCenter.transform.position);
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
    }

    private void OnGameStateChanged() {
        if (_gameMgr.CurrentState == GameState.GeneratingPathGraphs) {
            InitializeGameObjectData();
        }
        if (_gameMgr.CurrentState == GameState.RunningCountdown_2) {
            InitializePlayerIntelLevel();
        }
    }

    private void InitializeGameObjectData() {
        //InitializeSystems();    // systems before settements and celestial objects as they need the system name
        //InitializeSettlements();
        //InitializeStars();
        //InitializePlanetoids();
        InitializeCenter();

        //InitializeFleets();
    }

    //private void InitializeSystems() {
    //    int sysNumber = 0;
    //    foreach (SystemItem system in _systems) {
    //        Transform systemTransform = system.transform;
    //        string systemName = "System_" + sysNumber;
    //        //SystemData data = new SystemData(systemTransform, systemName) {
    //        SystemData data = new SystemData(systemName) {

    //            // there is no parentName for a System
    //            LastHumanPlayerIntelDate = new GameDate(),
    //            Capacity = 25,
    //            Resources = new OpeYield(3.1F, 2.0F, 4.8F),
    //            SpecialResources = new XYield(XResource.Special_1, 0.3F),
    //        };
    //        system.Data = data;
    //        sysNumber++;
    //    }
    //}

    //private void InitializeSettlements() {
    //    foreach (Item settlement in _settlements) {
    //        SystemItem system = settlement.gameObject.GetSafeMonoBehaviourComponentInParents<SystemItem>();
    //        string systemName = system.Data.Name;
    //        string settlementName = systemName + " Settlement";
    //        //SettlementData data = new SettlementData(settlement.transform, settlementName, 50F, systemName) {
    //        SettlementData data = new SettlementData(settlementName, 50F, systemName) {

    //            SettlementSize = SettlementSize.City,
    //            Population = 100,
    //            CapacityUsed = 10,
    //            ResourcesUsed = new OpeYield(1.3F, 0.5F, 2.4F),
    //            SpecialResourcesUsed = new XYield(new XYield.XResourceValuePair(XResource.Special_1, 0.2F)),
    //            Strength = new CombatStrength(1f, 2f, 3f, 4f, 5f, 6f),
    //            CurrentHitPoints = 38F,
    //            Owner = GameManager.Instance.HumanPlayer
    //        };
    //        settlement.Data = data;
    //        // PlayerIntelLevel is determined by the IntelLevel of the System
    //        system.Data.Settlement = data;  // TODO temporary as the SystemHud shows settlement info this way
    //    }
    //}

    //private void InitializeStars() {
    //    foreach (Item star in _stars) {
    //        SystemItem system = star.gameObject.GetSafeMonoBehaviourComponentInParents<SystemItem>();
    //        string systemName = system.Data.Name;
    //        string starName = systemName + " Star";
    //        //Data data = new Data(star.transform, starName, 1000000F, systemName) {
    //        Data data = new Data(starName, 1000000F, systemName) {

    //            LastHumanPlayerIntelDate = new GameDate()
    //        };
    //        star.Data = data;
    //        // PlayerIntelLevel is determined by the IntelLevel of the System
    //    }
    //}

    //private void InitializePlanetoids() {
    //    int planetoidNumber = 0;
    //    foreach (Item planetoid in _planetoids) {
    //        SystemItem system = planetoid.gameObject.GetSafeMonoBehaviourComponentInParents<SystemItem>();
    //        string systemName = system.Data.Name;
    //        string planetName = "Planet_" + planetoidNumber;
    //        //Data data = new Data(planetoid.transform, planetName, 100000F, systemName) {
    //        Data data = new Data(planetName, 100000F, systemName) {

    //            LastHumanPlayerIntelDate = new GameDate()
    //        };
    //        planetoid.Data = data;
    //        planetoidNumber++;
    //        // PlayerIntelLevel is determined by the IntelLevel of the System
    //    }
    //}

    private void InitializeCenter() {
        if (_universeCenter) {
            Data data = new Data("UniverseCenter", Mathf.Infinity);

            _universeCenter.Data = data;
            _universeCenter.enabled = true;
            _universeCenter.gameObject.GetSafeMonoBehaviourComponent<UniverseCenterView>().enabled = true;
        }
    }

    // **************** New Section now randomly instantiates fleets of ships rather than just find pre-existing ones ********************************

    //private void InitializeFleets() {
    //    if (!_fleetMgrs.IsNullOrEmpty()) {
    //        BuildFleets();
    //    }
    //}

    //private void BuildFleets() {
    //    bool isHumanFleetCreated = false;
    //    foreach (var fleetMgr in _fleetMgrs) {
    //        IPlayer owner;
    //        if (!isHumanFleetCreated) {
    //            owner = GameManager.Instance.HumanPlayer;
    //            isHumanFleetCreated = true;
    //        }
    //        else {
    //            owner = new Player(new Race(Enums<Races>.GetRandom(excludeDefault: true)), IQ.Normal);
    //        }
    //        FleetComposition composition = new FleetComposition();

    //        //determine how many ships of what hull for the fleet, then build shipdata and add to composition
    //        int shipCount = RandomExtended<int>.Range(3, 27);
    //        for (int i = 0; i < shipCount; i++) {

    //            IEnumerable<ShipHull> hullsToExclude = new ShipHull[] { default(ShipHull), 
    //                ShipHull.Fighter, ShipHull.Scout, ShipHull.Science, ShipHull.Support, ShipHull.Troop, ShipHull.Colonizer };
    //            ShipHull hull = RandomExtended<ShipHull>.Choice(Enums<ShipHull>.GetValues().Except(hullsToExclude).ToArray());

    //            string shipName = hull.GetName() + Constants.Underscore + i;
    //            float mass = TempGameValues.__GetMass(hull);
    //            float drag = 0.1F;
    //            ShipData shipData = new ShipData(shipName, 50F, mass, drag) {
    //                // Ship's optionalParentName gets set when it gets attached to a fleet
    //                Hull = hull,
    //                Strength = new CombatStrength(),
    //                LastHumanPlayerIntelDate = new GameDate(),
    //                CurrentHitPoints = UnityEngine.Random.Range(25F, 50F),
    //                MaxTurnRate = UnityEngine.Random.Range(1F, 2F),
    //                Owner = owner,
    //                MaxThrust = mass * drag * UnityEngine.Random.Range(2F, 5F)  // MaxThrust = Mass * Drag * MaxSpeed;
    //            };
    //            composition.AddShip(shipData);
    //        }
    //        fleetMgr.BuildFleet(composition);
    //    }
    //}

    // *****************************************************************************************************************************************************************


    /// <summary>
    /// PlayerIntelLevel changes immediately propogate through COs and Ships so initialize this last in case the change pulls Data.
    /// </summary>
    private void InitializePlayerIntelLevel() {
        //_systemMgrs.ForAll<SystemManager>(sm => sm.gameObject.GetSafeInterfaceInChildren<ISystemViewable>().PlayerIntelLevel
        //    = Enums<IntelLevel>.GetRandom(excludeDefault: true));
        _universeCenter.gameObject.GetSafeInterface<IViewable>().PlayerIntelLevel = Enums<IntelLevel>.GetRandom(excludeDefault: true);
        //_fleetMgrs.ForAll<FleetManager>(fm => fm.gameObject.GetSafeInterfaceInChildren<IFleetViewable>().PlayerIntelLevel
        //    = RandomExtended<IntelLevel>.Choice(Enums<IntelLevel>.GetValues().Except(default(IntelLevel), IntelLevel.Nil).ToArray()));
        //= IntelLevel.Nil);
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

