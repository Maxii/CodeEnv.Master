// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetUnitCreator.cs
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
public class FleetUnitCreator : AUnitCreator<ShipModel, ShipCategory, ShipData, ShipStats, FleetCmdModel> {

    private UnitFactory _factory;

    protected override void Awake() {
        base.Awake();
        _factory = UnitFactory.Instance;
    }

    protected override GameState GetCreationGameState() {
        return GameState.DeployingSettlements;  // Can be anytime? Should be after GeneratePathGraph so no interference
    }

    protected override void CreateElementStat(ShipCategory category, string elementName) {
        float mass = TempGameValues.__GetMass(category);
        float drag = 0.1F;

        ShipStats stat = new ShipStats() {
            Category = category,
            Name = elementName,
            Mass = mass,
            Drag = drag,
            FullThrust = mass * drag * UnityEngine.Random.Range(2F, 5F), // MaxThrust = Mass * Drag * MaxSpeed;
            MaxHitPoints = 50F,
            MaxTurnRate = UnityEngine.Random.Range(300F, 315F),
            Strength = new CombatStrength(),
            CombatStance = Enums<ShipCombatStance>.GetRandom(excludeDefault: true),
            Weapons = new List<Weapon>() { 
                new Weapon(WeaponCategory.BeamOffense, model: 1) {
                Range = UnityEngine.Random.Range(2F, 4F),
                ReloadPeriod = UnityEngine.Random.Range(1.4F, 1.6F),
                Damage = UnityEngine.Random.Range(4F, 6F)
                },
                new Weapon(WeaponCategory.MissileOffense, model: 2) {
                Range = UnityEngine.Random.Range(2F, 4F),
                ReloadPeriod = UnityEngine.Random.Range(2F, 2.5F),
                Damage = UnityEngine.Random.Range(8F, 12F)
                }
            },
        };
        _elementStats.Add(stat);
    }

    protected override FleetCmdModel GetCommand(IPlayer owner) {
        FleetCmdStats cmdStats = new FleetCmdStats() {
            Name = UnitName,
            MaxHitPoints = 10F,
            MaxCmdEffectiveness = 100,
            Strength = new CombatStrength(),
            UnitFormation = Formation.Globe
        };
        FleetCmdModel cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSafeMonoBehaviourComponentInChildren<FleetCmdModel>();
            var existingCmdReference = cmd;
            bool isCmdCompatibleWithOwner = _factory.MakeFleetCmdInstance(cmdStats, owner, ref cmd);
            if (!isCmdCompatibleWithOwner) {
                Destroy(existingCmdReference.gameObject);
            }
        }
        else {
            cmd = _factory.MakeFleetCmdInstance(cmdStats, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        return cmd;
    }

    protected override int GetStatsCount(ShipCategory elementCategory) {
        return _elementStats.Where(s => s.Category == elementCategory).Count();
    }

    protected override ShipModel MakeElement(ShipStats stat, IPlayer owner) {
        return _factory.MakeInstance(stat, owner);
    }

    /// <summary>
    /// Makes an element based off of the provided element. Returns true if the provided element is compatible
    /// with the provided owner, false if it is not and had to be replaced. If an element is replaced, then clients
    /// are responsible for destroying the original provided element.
    /// </summary>
    /// <param name="stat">The stat.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="element">The element.</param>
    /// <returns></returns>
    protected override bool MakeElement(ShipStats stat, IPlayer owner, ref ShipModel element) { // OPTIMIZE
        return _factory.MakeInstance(stat, owner, ref element);
    }

    protected override ShipCategory GetCategory(ShipStats stat) {
        return stat.Category;
    }

    protected override ShipCategory[] GetValidElementCategories() {
        return new ShipCategory[] { ShipCategory.Frigate, ShipCategory.Destroyer, ShipCategory.Cruiser, ShipCategory.Carrier, ShipCategory.Dreadnaught };
    }

    protected override ShipCategory[] GetValidHQElementCategories() {
        return new ShipCategory[] { ShipCategory.Cruiser, ShipCategory.Carrier, ShipCategory.Dreadnaught };
    }

    protected override void __InitializeCommandIntel() {
        _command.gameObject.GetSafeInterface<ICommandViewable>().PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

