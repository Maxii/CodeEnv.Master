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

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Initializes Data for all items in the universe.
/// </summary>
public class __UniverseInitializer : AMonoBehaviourBase {

    private FleetAdmiral[] _fleetsToInitialize;
    private ShipCaptain[] _shipsToInitialize;
    private AFollowableItem[] _planetsAndMoonsToInitialize;
    private Star[] _starsToInitialize;
    private OrbitalPlane[] _systemsToInitialize;

    void Awake() {
        _fleetsToInitialize = gameObject.GetSafeMonoBehaviourComponentsInChildren<FleetAdmiral>();
        _shipsToInitialize = gameObject.GetSafeMonoBehaviourComponentsInChildren<ShipCaptain>();
        // TODO I'll need to pick the ships under each fleet and then add those ships to each fleet when initializing
        FollowableItem[] allFollowableItems = gameObject.GetSafeMonoBehaviourComponentsInChildren<FollowableItem>();
        _planetsAndMoonsToInitialize = allFollowableItems.Except<FollowableItem>(_shipsToInitialize)
            .Except<FollowableItem>(_fleetsToInitialize).ToArray<FollowableItem>();
        _starsToInitialize = gameObject.GetSafeMonoBehaviourComponentsInChildren<Star>();
        _systemsToInitialize = gameObject.GetSafeMonoBehaviourComponentsInChildren<OrbitalPlane>();

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
        foreach (var orbitalPlane in _systemsToInitialize) {
            Transform systemTransform = orbitalPlane.transform.parent;
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
            orbitalPlane.Data = data;
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


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

