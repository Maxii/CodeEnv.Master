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
            MaxTurnRate = UnityEngine.Random.Range(45F, 315F),
            Strength = new CombatStrength(),
            Weapons = new List<Weapon>() { 
                new Weapon(WeaponCategory.BeamOffense, model: 1) {
                Range = UnityEngine.Random.Range(3F, 6F),
                ReloadPeriod = UnityEngine.Random.Range(0.4F, 0.6F),
                Damage = UnityEngine.Random.Range(4F, 6F)
                },
                new Weapon(WeaponCategory.MissileOffense, model: 2) {
                Range = UnityEngine.Random.Range(5F, 8F),
                ReloadPeriod = UnityEngine.Random.Range(1F, 1.5F),
                Damage = UnityEngine.Random.Range(8F, 12F)
                }
            },
            CurrentHitPoints = UnityEngine.Random.Range(25F, 50F)
        };
        _elementStats.Add(stat);
    }

    protected override FleetCmdModel GetCommand(IPlayer owner) {
        FleetCmdModel cmd;
        if (_isPreset) {
            cmd = gameObject.GetSafeMonoBehaviourComponentInChildren<FleetCmdModel>();
            _factory.PopulateCommand(UnitName, owner, ref cmd);
        }
        else {
            cmd = _factory.MakeFleetCmdInstance(UnitName, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        return cmd;
    }

    protected override IList<ShipStats> GetStats(ShipCategory elementCategory) {
        return _elementStats.Where(s => s.Category == elementCategory).ToList();
    }

    protected override ShipModel MakeElement(ShipStats stat) {
        return _factory.MakeInstance(stat);
    }

    protected override void MakeElement(ShipStats stat, ref ShipModel element) { // OPTIMIZE
        _factory.MakeInstance(stat, ref element);
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

    protected override void EnableViews() {
        _elements.ForAll(e => e.gameObject.GetSafeMonoBehaviourComponent<ShipView>().enabled = true);
        _command.gameObject.GetSafeMonoBehaviourComponent<FleetCmdView>().enabled = true;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

