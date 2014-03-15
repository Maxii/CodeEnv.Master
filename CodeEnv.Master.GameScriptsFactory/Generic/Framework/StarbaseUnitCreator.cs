// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseUnitCreator.cs
// Initialization class that deploys a Starbase at the location of this StarbaseCreator.
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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Initialization class that deploys a Starbase at the location of this StarbaseCreator. 
/// </summary>
public class StarbaseUnitCreator : AUnitCreator<FacilityModel, FacilityCategory, FacilityData, FacilityStats, StarbaseCmdModel> {

    private UnitFactory _factory;

    protected override void Awake() {
        base.Awake();
        _factory = UnitFactory.Instance;
    }

    protected override GameState GetCreationGameState() {
        return GameState.DeployingSystems;
    }

    protected override void CreateElementStat(FacilityCategory category, string elementName) {
        FacilityStats stat = new FacilityStats() {
            Category = category,
            Name = elementName,
            Mass = 10000F,
            MaxHitPoints = 50F,
            Strength = new CombatStrength(),
            Weapons = new List<Weapon>() { 
                new Weapon(WeaponCategory.BeamOffense, model: 1) {
                Range = UnityEngine.Random.Range(3F, 5F),
                ReloadPeriod = UnityEngine.Random.Range(0.6F, 0.9F),
                Damage = UnityEngine.Random.Range(3F, 8F)
                }
            },
            CurrentHitPoints = UnityEngine.Random.Range(25F, 50F)
        };
        _elementStats.Add(stat);
    }

    protected override IList<FacilityStats> GetStats(FacilityCategory elementCategory) {
        return _elementStats.Where(s => s.Category == elementCategory).ToList();
    }

    protected override FacilityModel MakeElement(FacilityStats stat) {
        return _factory.MakeInstance(stat);
    }

    protected override void MakeElement(FacilityStats stat, ref FacilityModel element) {
        _factory.MakeInstance(stat, ref element);
    }

    protected override FacilityCategory GetCategory(FacilityStats stat) {
        return stat.Category;
    }

    protected override FacilityCategory[] GetValidElementCategories() {
        return new FacilityCategory[] { FacilityCategory.Construction, FacilityCategory.Defense, FacilityCategory.Economic, FacilityCategory.Science };
    }

    protected override FacilityCategory[] GetValidHQElementCategories() {
        return new FacilityCategory[] { FacilityCategory.CentralHub };
    }

    protected override StarbaseCmdModel GetCommand(IPlayer owner) {
        StarbaseCmdModel cmd;
        if (_isPreset) {
            cmd = gameObject.GetSafeMonoBehaviourComponentInChildren<StarbaseCmdModel>();
            _factory.PopulateCommand(UnitName, owner, ref cmd);
        }
        else {
            cmd = _factory.MakeStarbaseCmdInstance(UnitName, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        return cmd;
    }

    protected override void EnableViews() {
        _elements.ForAll(e => e.gameObject.GetSafeMonoBehaviourComponent<FacilityView>().enabled = true);
        _command.gameObject.GetSafeMonoBehaviourComponent<StarbaseCmdView>().enabled = true;
    }

    protected override void __InitializeCommandIntel() {
        _command.gameObject.GetSafeInterface<ICommandViewable>().PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

