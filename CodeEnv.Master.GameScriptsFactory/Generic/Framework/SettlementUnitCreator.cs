// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementUnitCreator.cs
//  Initialization class that deploys a Settlement that is available for assignment to a System.
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
///  Initialization class that deploys a Settlement that is available for assignment to a System.
///  When assigned, the Settlement relocates to the orbital slot for Settlements held open by the System.
/// </summary>
public class SettlementUnitCreator : AUnitCreator<FacilityModel, FacilityCategory, FacilityData, FacilityStats, SettlementCmdModel> {

    public event Action<SettlementUnitCreator> onCompleted;

    protected override GameState GetCreationGameState() {
        return GameState.DeployingSystems;  // Building can take place anytime? Placing in Systems takes place in DeployingSettlements
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

    protected override SettlementCmdModel GetCommand(IPlayer owner) {
        SettlementCmdModel cmd;
        if (_isPreset) {
            cmd = gameObject.GetSafeMonoBehaviourComponentInChildren<SettlementCmdModel>();
            _factory.PopulateCommand(UnitName, owner, ref cmd);
        }
        else {
            cmd = _factory.MakeSettlementCmdInstance(UnitName, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        return cmd;
    }

    protected override void EnableViews() {
        _elements.ForAll(e => e.gameObject.GetSafeMonoBehaviourComponent<FacilityView>().enabled = true);
        _command.gameObject.GetSafeMonoBehaviourComponent<SettlementCmdView>().enabled = true;
    }

    protected override void __InitializeCommandIntel() {
        // For now settlements assume the intel coverage of their system when assigned
    }

    protected override void OnCompleted() {
        base.OnCompleted();
        var temp = onCompleted;
        if (temp != null) {
            temp(this);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

