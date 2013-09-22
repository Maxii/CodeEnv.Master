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

    private GameManager _gameMgr;
    private IList<IDisposable> _subscribers;

    private FleetManager[] _fleets;
    private ShipCaptain[] _ships;
    private FollowableItem[] _planetsAndMoons;
    private Star[] _stars;
    private SystemManager[] _systems;
    private StationaryItem _universeCenter;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        Subscribe();
        AcquireGameObjectsRequiringDataToInitialize();
    }

    private void AcquireGameObjectsRequiringDataToInitialize() {
        _fleets = gameObject.GetSafeMonoBehaviourComponentsInChildren<FleetManager>();
        _ships = gameObject.GetSafeMonoBehaviourComponentsInChildren<ShipCaptain>();
        // TODO I'll need to pick the ships under each fleet and then add those ships to each fleet when initializing
        _systems = gameObject.GetSafeMonoBehaviourComponentsInChildren<SystemManager>();
        _stars = gameObject.GetSafeMonoBehaviourComponentsInChildren<Star>();
        _planetsAndMoons = new FollowableItem[0];
        foreach (var sys in _systems) {
            _planetsAndMoons = _planetsAndMoons.Concat<FollowableItem>(sys.gameObject.GetSafeMonoBehaviourComponentsInChildren<FollowableItem>()).ToArray();
        }
        _universeCenter = gameObject.GetSafeMonoBehaviourComponentInChildren<UniverseCenter>();
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
        InitializeSystems();
        InitializeStars();
        InitializePlanetsAndMoons();
        InitializeShips();
        InitializeFleet();
        InitializeCenter();
    }

    private void InitializeSystems() {
        int sysNumber = 0;
        foreach (SystemManager sysMgr in _systems) {
            Transform systemTransform = sysMgr.transform;
            SystemData data = new SystemData(systemTransform, "System_" + sysNumber) {
                // there is no parentName for a System
                LastHumanPlayerIntelDate = new GameDate(),
                Capacity = 25,
                Resources = new OpeYield(3.1F, 2.0F, 4.8F),
                SpecialResources = new XYield(XResource.Special_1, 0.3F),
                Settlement = new SettlementData() {
                    SettlementSize = SettlementSize.City,
                    Population = 100,
                    CapacityUsed = 10,
                    ResourcesUsed = new OpeYield(1.3F, 0.5F, 2.4F),
                    SpecialResourcesUsed = new XYield(new XYield.XResourceValuePair(XResource.Special_1, 0.2F)),
                    Strength = new CombatStrength(1f, 2f, 3f, 4f, 5f, 6f),
                    Health = 38F,
                    MaxHitPoints = 50F,
                    Owner = GameManager.Instance.HumanPlayer
                }
            };
            sysMgr.Data = data;
            sysMgr.PlayerIntelLevel = Enums<IntelLevel>.GetRandom(excludeDefault: true);
            D.Log("Random PlayerIntelLevel = {0}.", sysMgr.PlayerIntelLevel.GetName());
            sysNumber++;
        }
    }

    private void InitializeStars() {
        foreach (Star star in _stars) {
            SystemManager sysMgr = star.gameObject.GetSafeMonoBehaviourComponentInParents<SystemManager>();
            string parentName = sysMgr.Data.Name;
            string name = parentName + " Star";
            Data data = new Data(star.transform, name, parentName) {
                LastHumanPlayerIntelDate = new GameDate()
            };
            star.Data = data;
            // Celestial object PlayerIntelLevel is determined by the IntelLevel of the System
        }
    }

    private void InitializePlanetsAndMoons() {
        int planetNumber = 0;
        foreach (FollowableItem item in _planetsAndMoons) {
            SystemManager sysMgr = item.gameObject.GetSafeMonoBehaviourComponentInParents<SystemManager>();
            string parentName = sysMgr.Data.Name;
            string name = "Planet_" + planetNumber;
            Data data = new Data(item.transform, name, parentName) {
                LastHumanPlayerIntelDate = new GameDate()
            };
            item.Data = data;
            planetNumber++;
            // Celestial object PlayerIntelLevel is determined by the IntelLevel of the System
        }
    }

    private void InitializeShips() {
        int shipNumber = 0;
        foreach (ShipCaptain ship in _ships) {
            ShipData data = new ShipData(ship.transform, "Ship_" + shipNumber) {
                // Ship's optionalParentName gets set when it gets attached to a fleet
                Hull = ShipHull.Destroyer,
                Strength = new CombatStrength(1f, 2f, 3f, 4f, 5f, 6f),
                LastHumanPlayerIntelDate = new GameDate(),
                Health = 38F,
                MaxHitPoints = 50F,
                Owner = GameManager.Instance.HumanPlayer,
                MaxTurnRate = 1.0F,
                RequestedHeading = ship.transform.forward
            };
            data.MaxThrust = data.Mass * data.Drag * 2F;    // MaxThrust = MaxSpeed * Mass * Drag
            ship.Data = data;
            shipNumber++;
            // A ship's PlayerIntelLevel is determined by the IntelLevel of the Fleet
        }
    }

    private void InitializeFleet() {
        FleetManager fleet = _fleets[0];
        Transform admiralTransform = fleet.gameObject.GetSafeMonoBehaviourComponentInChildren<FleetCommand>().transform;
        FleetData data = new FleetData(admiralTransform, "Borg Fleet") {
            // there is no parentName for a fleet
            LastHumanPlayerIntelDate = new GameDate()
        };

        foreach (var ship in _ships) {
            data.AddShip(ship.Data);
        }
        fleet.Data = data;
        fleet.PlayerIntelLevel = IntelLevel.Complete;
    }

    private void InitializeCenter() {
        if (_universeCenter) {
            Data data = new Data(_universeCenter.transform, "UniverseCenter");
            _universeCenter.Data = data;
            _universeCenter.PlayerIntelLevel = IntelLevel.Unknown;
        }
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
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
            Unsubscribe();
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

