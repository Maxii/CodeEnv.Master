﻿// --------------------------------------------------------------------------------------------------------------------
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
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Initializes Data for all items in the universe.
/// </summary>
public class __UniverseInitializer : AMonoBehaviourBase, IDisposable {

    private GameManager _gameMgr;
    private IList<IDisposable> _subscribers;

    private FleetAdmiral[] _fleetsToInitialize;
    private ShipCaptain[] _shipsToInitialize;
    private FollowableItem[] _planetsAndMoonsToInitialize;
    private Star[] _starsToInitialize;
    private SystemManager[] _systemsToInitialize;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        Subscribe();
        AcquireGameObjectsRequiringDataToInitialize();
    }

    private void AcquireGameObjectsRequiringDataToInitialize() {
        _fleetsToInitialize = gameObject.GetSafeMonoBehaviourComponentsInChildren<FleetAdmiral>();
        _shipsToInitialize = gameObject.GetSafeMonoBehaviourComponentsInChildren<ShipCaptain>();
        // TODO I'll need to pick the ships under each fleet and then add those ships to each fleet when initializing
        FollowableItem[] allFollowableItems = gameObject.GetSafeMonoBehaviourComponentsInChildren<FollowableItem>();
        _planetsAndMoonsToInitialize = allFollowableItems.Except<FollowableItem>(_shipsToInitialize)
            .Except<FollowableItem>(_fleetsToInitialize).ToArray<FollowableItem>();
        _starsToInitialize = gameObject.GetSafeMonoBehaviourComponentsInChildren<Star>();
        _systemsToInitialize = gameObject.GetSafeMonoBehaviourComponentsInChildren<SystemManager>();
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
        InitializePlanetsAndMoons();
        InitializeStars();
        InitializeSystems();
        InitializeShips();
        InitializeFleet();
    }

    private void InitializePlanetsAndMoons() {
        foreach (var item in _planetsAndMoonsToInitialize) {
            Data data = new Data(item.transform) {
                ItemName = item.gameObject.name,
                LastHumanPlayerIntelDate = new GameDate()
            };
            item.Data = data;
        }
    }

    private void InitializeStars() {
        foreach (var star in _starsToInitialize) {
            Data data = new Data(star.transform) {
                ItemName = star.gameObject.name,
                PieceName = star.gameObject.GetSafeMonoBehaviourComponentInParents<SystemGraphics>().gameObject.name,
                LastHumanPlayerIntelDate = new GameDate()
            };
            star.Data = data;
        }
    }

    private void InitializeSystems() {
        foreach (var sysMgr in _systemsToInitialize) {
            Transform systemTransform = sysMgr.transform;
            SystemData data = new SystemData(systemTransform) {
                ItemName = systemTransform.gameObject.GetSafeMonoBehaviourComponentInChildren<Star>().gameObject.name,
                PieceName = systemTransform.name,
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
        }
    }

    private void InitializeShips() {
        foreach (var ship in _shipsToInitialize) {
            ShipData data = new ShipData(ship.transform) {
                ItemName = ship.gameObject.name,
                // Ship's PieceName gets set when it gets attached to a fleet
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
        }
    }

    private void InitializeFleet() {
        var fleet = _fleetsToInitialize[0];
        FleetData data = new FleetData(fleet.transform) {
            // there is no ItemName for a fleet
            PieceName = fleet.gameObject.name,
            LastHumanPlayerIntelDate = new GameDate()
        };

        foreach (var ship in _shipsToInitialize) {
            data.AddShip(ship.Data);
        }
        fleet.Data = data;
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
    }

    void OnDestroy() {
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

