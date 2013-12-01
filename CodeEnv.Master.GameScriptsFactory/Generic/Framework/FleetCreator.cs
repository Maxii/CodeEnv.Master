// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCreator.cs
// Initialization class that deploys a fleet at the location of this FleetCreator. 
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
/// Initialization class that deploys a fleet at the location of this FleetCreator. The fleet
/// deployed will simply be initialized if already present in the scene. If it is not present, then
/// it will be built and then initialized.
/// </summary>
public class FleetCreator : AMonoBase, IDisposable {

    private static bool __isHumanFleetCreated;

    public int maxShips = 8;

    private string _fleetName;

    private FleetComposition _composition;
    private IList<ShipItem> _ships;
    private FleetItem _fleetCmd;
    private IList<IDisposable> _subscribers;
    private bool _isExistingFleet;

    protected override void Awake() {
        base.Awake();
        _fleetName = gameObject.name;   // the name of the fleet is carried by the name of the FleetMgr gameobject
        _isExistingFleet = _transform.childCount > 0;
        CreateFleetComposition();
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.CurrentState, OnGameStateChanged));
    }

    private void OnGameStateChanged() {
        if (GameManager.Instance.CurrentState == GameState.RunningCountdown_2) {
            DeployFleet();
            EnableFleet();  // must make View operational before starting state changes within it
        }
        if (GameManager.Instance.CurrentState == GameState.RunningCountdown_1) {
            __SetIntelLevel();
        }
    }

    private void CreateFleetComposition() {
        if (_isExistingFleet) {
            CreateCompositionFromChildren();
        }
        else {
            CreateRandomComposition();
        }
    }

    private void CreateCompositionFromChildren() {
        _ships = gameObject.GetSafeMonoBehaviourComponentsInChildren<ShipItem>();
        IPlayer owner = GameManager.Instance.HumanPlayer;
        __isHumanFleetCreated = true;
        _composition = new FleetComposition();
        foreach (var ship in _ships) {
            ShipHull hull = GetShipHull(ship);
            string shipName = ship.gameObject.name;
            ShipData data = CreateShipData(hull, shipName, owner);
            _composition.AddShip(data);
        }
    }

    private void CreateRandomComposition() {
        IPlayer owner;
        if (!__isHumanFleetCreated) {
            owner = GameManager.Instance.HumanPlayer;
            __isHumanFleetCreated = true;
        }
        else {
            owner = new Player(new Race(Enums<Races>.GetRandom(excludeDefault: true)), IQ.Normal);
        }
        _composition = new FleetComposition();

        ShipHull[] __hullsToConsider = new ShipHull[] { ShipHull.Carrier, ShipHull.Cruiser, ShipHull.Destroyer, ShipHull.Dreadnaught, ShipHull.Frigate };

        //determine how many ships of what hull for the fleet, then build shipdata and add to composition
        int shipCount = RandomExtended<int>.Range(1, maxShips);
        for (int i = 0; i < shipCount; i++) {
            ShipHull hull = RandomExtended<ShipHull>.Choice(__hullsToConsider);
            int shipHullIndex = GetShipHullIndex(hull);
            string shipName = hull.GetName() + Constants.Underscore + shipHullIndex;
            ShipData shipData = CreateShipData(hull, shipName, owner);
            _composition.AddShip(shipData);
        }
    }

    private ShipData CreateShipData(ShipHull hull, string shipName, IPlayer owner) {
        float mass = TempGameValues.__GetMass(hull);
        float drag = 0.1F;
        ShipData shipData = new ShipData(shipName, 50F, mass, drag) {
            // Ship's optionalParentName gets set when it gets attached to a fleet
            Hull = hull,
            Strength = new CombatStrength(),
            LastHumanPlayerIntelDate = new GameDate(),
            CurrentHitPoints = UnityEngine.Random.Range(25F, 50F),
            MaxTurnRate = UnityEngine.Random.Range(45F, 315F),
            Owner = owner,
            MaxThrust = mass * drag * UnityEngine.Random.Range(2F, 5F)  // MaxThrust = Mass * Drag * MaxSpeed;
        };
        return shipData;
    }

    private void DeployFleet() {
        if (_isExistingFleet) {
            DeployExistingFleet();
        }
        else {
            DeployRandomFleet();
        }
    }

    private void DeployExistingFleet() {
        InitializeShips();
        _fleetCmd = gameObject.GetSafeMonoBehaviourComponentInChildren<FleetItem>();
        InitializeFleet();
        SelectFlagship();
        AssignFormationPositions();
    }

    private void DeployRandomFleet() {
        BuildShips();
        InitializeShips();
        BuildFleetCommand();
        InitializeFleet();
        SelectFlagship();
        float fleetGlobeRadius = 1F * (float)Math.Pow(_ships.Count * 0.2F, 0.33F);  // cube root of number of groups of 5 ships
        //if (!RandomlyPositionFleetElements(fleetGlobeRadius)) {
        //    // try again with a larger radius
        //    D.Assert(RandomlyPositionFleetElements(fleetGlobeRadius * 1.5F), "Fleet Positioning Error");
        //}
        PositionFleetElementsInCircle(fleetGlobeRadius);
        AssignFormationPositions();
    }

    private void BuildShips() {
        _ships = new List<ShipItem>();
        foreach (var hull in _composition.Hulls) {
            GameObject hullPrefab = RequiredPrefabs.Instance.ships.First(p => p.gameObject.name == hull.GetName()).gameObject;
            string hullName = hullPrefab.name;

            _composition.GetShipData(hull).ForAll(sd => {
                GameObject shipGo = UnityUtility.AddChild(gameObject, hullPrefab);
                shipGo.name = hullName; // get rid of (Clone) in name
                ShipItem ship = shipGo.GetSafeMonoBehaviourComponent<ShipItem>();
                _ships.Add(ship);

            });
        }
    }

    private void InitializeShips() {
        IDictionary<ShipHull, Stack<ShipData>> typeLookup = new Dictionary<ShipHull, Stack<ShipData>>();
        foreach (var ship in _ships) {
            ShipHull hull = GetShipHull(ship);
            Stack<ShipData> dataStack;
            if (!typeLookup.TryGetValue(hull, out dataStack)) {
                dataStack = new Stack<ShipData>(_composition.GetShipData(hull));
                typeLookup.Add(hull, dataStack);
            }
            ship.Data = dataStack.Pop();  // automatically adds the ship's transform to Data when set
        }
    }

    private void BuildFleetCommand() {
        GameObject fleetCmdGo = UnityUtility.AddChild(gameObject, RequiredPrefabs.Instance.fleetCmd.gameObject);
        _fleetCmd = fleetCmdGo.GetSafeMonoBehaviourComponent<FleetItem>();
    }

    private void InitializeFleet() {
        string fleetCmdName = _fleetName + Constants.Space + CommonTerms.Command;
        _fleetCmd.Data = new FleetData(fleetCmdName);    // automatically adds the fleetCmd transform to Data when set

        // add each ship to the fleet
        _ships.ForAll(ship => _fleetCmd.AddShip(ship));
        // include FleetCmd as a target in each ship's CameraLOSChangedRelay
        _ships.ForAll(ship => ship.gameObject.GetSafeMonoBehaviourComponentInChildren<CameraLOSChangedRelay>().AddTarget(_fleetCmd.transform));
    }

    /// <summary>
    /// Selects and marks the flagship so formation creation knows which ship it is.
    /// Once enabled, the flagship will assign itself to its FleetCmd once it has initialized
    /// its Navigator to receive the immediate callback from FleetCmd.
    /// </summary>
    private void SelectFlagship() {
        RandomExtended<ShipItem>.Choice(_ships).IsFlagship = true;
    }

    /// <summary>
    /// Randomly positions the ships of the fleet in a spherical globe around this location.
    /// </summary>
    /// <param name="radius">The radius of the globe within which to deploy the fleet.</param>
    /// <returns></returns>
    private bool RandomlyPositionFleetElements(float radius) {  // FIXME need to set FormationPosition
        GameObject[] shipGos = _ships.Select(s => s.gameObject).ToArray();
        Vector3 fleetCenter = _transform.position;
        D.Log("Radius of Sphere occupied by Fleet of {0} is {1}.", shipGos.Length, radius);
        return UnityUtility.PositionRandomWithinSphere(fleetCenter, radius, shipGos);
        // fleetCmd will relocate itsef once it selects its flagship
    }

    private void PositionFleetElementsInCircle(float radius) {
        Vector3 fleetCenter = _transform.position;
        Stack<Vector3> localFormationPositions = new Stack<Vector3>(Mathfx.UniformPointsOnCircle(radius, _ships.Count - 1));
        foreach (var ship in _ships) {
            if (ship.IsFlagship) {
                ship.transform.position = fleetCenter;
            }
            else {
                Vector3 localFormationPosition = localFormationPositions.Pop();
                ship.transform.position = fleetCenter + localFormationPosition;
            }
        }
    }

    private void AssignFormationPositions() {
        ShipItem flagship = _ships.Single(s => s.IsFlagship);
        Vector3 flagshipPosition = flagship.transform.position;
        foreach (var ship in _ships) {
            if (ship.IsFlagship) {
                ship.Data.FormationPosition = Vector3.zero;
                D.Log("Flagship is {0}.", ship.Data.Name);
                continue;
            }
            ship.Data.FormationPosition = ship.transform.position - flagshipPosition;
            D.Log("{0}.FormationPosition = {1}.", ship.Data.Name, ship.Data.FormationPosition);
        }
    }

    private void __SetIntelLevel() {
        _fleetCmd.gameObject.GetSafeInterface<IFleetViewable>().PlayerIntelLevel = IntelLevel.Complete;
        //RandomExtended<IntelLevel>.Choice(Enums<IntelLevel>.GetValues().Except(default(IntelLevel), IntelLevel.Nil).ToArray());
    }

    private void EnableFleet() {
        // ships need to run their Start first to initialize their navigators and assign the flagship to fleetCmd before fleetCmd is enabled and runs its Start
        _ships.ForAll(ship => ship.enabled = true);
        _fleetCmd.enabled = true;
        _ships.ForAll(ship => ship.gameObject.GetSafeMonoBehaviourComponent<ShipView>().enabled = true);
        _fleetCmd.gameObject.GetSafeMonoBehaviourComponent<FleetView>().enabled = true;
    }

    private ShipHull GetShipHull(ShipItem ship) {
        return Enums<ShipHull>.Parse(ship.gameObject.name);
    }

    private int GetShipHullIndex(ShipHull hull) {
        if (!_composition.Hulls.Contains(hull)) {
            return 1;
        }
        return _composition.GetShipData(hull).Count + 1;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
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

