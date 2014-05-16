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
public class SettlementUnitCreator : AUnitCreator<FacilityModel, FacilityCategory, FacilityData, FacilityStat, SettlementCmdModel> {

    private UnitFactory _factory;   // not accesible from AUnitCreator

    protected override void Awake() {
        base.Awake();
        _factory = UnitFactory.Instance;
    }

    // all starting units are now built and initialized during GameState.PrepareUnitsForOperations

    protected override FacilityStat CreateElementStat(FacilityCategory category, string elementName) {
        return new FacilityStat(elementName, 10000F, 50F, category);
    }

    protected override FacilityModel MakeElement(FacilityStat stat, IEnumerable<WeaponStat> weaponStats, IPlayer owner) {
        return _factory.MakeInstance(stat, weaponStats, owner);
    }

    protected override bool MakeElement(FacilityStat stat, IEnumerable<WeaponStat> weaponStats, IPlayer owner, ref FacilityModel element) {
        _factory.MakeInstance(stat, weaponStats, owner, ref element);
        return true;    // IMPROVE dummy return for facilities to match signature of abstract MakeElement - facilties currently don't have a HumanView 
    }

    protected override FacilityCategory GetCategory(FacilityStat stat) {
        return stat.Category;
    }

    protected override FacilityCategory[] GetValidElementCategories() {
        return new FacilityCategory[] { FacilityCategory.Construction, FacilityCategory.Defense, FacilityCategory.Economic, FacilityCategory.Science };
    }

    protected override FacilityCategory[] GetValidHQElementCategories() {
        return new FacilityCategory[] { FacilityCategory.CentralHub };
    }

    protected override SettlementCmdModel MakeCommand(IPlayer owner) {
        LogEvent();
        SettlementCmdStat cmdStat = new SettlementCmdStat(UnitName, 10F, 100, Formation.Circle, new CombatStrength(0F, 5F, 0F, 5F, 0F, 5F), 100);

        SettlementCmdModel cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSafeMonoBehaviourComponentInChildren<SettlementCmdModel>();
            var existingCmdReference = cmd;
            bool isCmdCompatibleWithOwner = _factory.MakeSettlementCmdInstance(cmdStat, owner, ref cmd);
            if (!isCmdCompatibleWithOwner) {
                Destroy(existingCmdReference.gameObject);
            }
        }
        else {
            cmd = _factory.MakeSettlementCmdInstance(cmdStat, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        return cmd;
    }

    protected override bool DeployUnit() {
        LogEvent();
        var allSystems = SystemCreator.AllSystems;
        var availableSystems = allSystems.Where(sys => sys.Data.Owner == TempGameValues.NoPlayer);
        if (availableSystems.IsNullOrEmpty()) {
            D.Warn("No Systems available to deploy {0}..", UnitName);
            return false;
        }
        availableSystems.First().AssignSettlement(_command);
        return true;
    }

    protected override void BeginElementsOperations() {
        LogEvent();
        _elements.ForAll(e => (e as FacilityModel).CurrentState = FacilityState.Idling);
    }

    protected override void BeginCommandOperations() {
        LogEvent();
        _command.CurrentState = SettlementState.Idling;
    }

    protected override void AssignHQElement() {
        LogEvent();
        var candidateHQElements = _command.Elements.Where(e => GetValidHQElementCategories().Contains((e as FacilityModel).Data.Category));
        D.Assert(!candidateHQElements.IsNullOrEmpty()); // bases must have a CentralHub, even if preset
        _command.HQElement = RandomExtended<IElementModel>.Choice(candidateHQElements) as FacilityModel;
    }

    protected override void __InitializeCommandIntel() {
        LogEvent();
        // For now settlements assume the intel coverage of their system when assigned
    }

    protected override void EnableOtherWhenRunning() {
        D.Assert(GameStatus.Instance.IsRunning);
        // the entire settlementUnit gameobject has already been detached from this creator at this point
        GameObject settlementUnitGo = _command.transform.parent.gameObject;
        settlementUnitGo.GetSafeMonoBehaviourComponentsInChildren<CameraLOSChangedRelay>().ForAll(relay => relay.enabled = true);
        settlementUnitGo.GetSafeMonoBehaviourComponentsInChildren<WeaponRangeTracker>().ForAll(wrt => wrt.enabled = true);
        settlementUnitGo.GetSafeMonoBehaviourComponentsInChildren<Revolve>().ForAll(rev => rev.enabled = true);
        settlementUnitGo.GetSafeMonoBehaviourComponentInParents<Orbit>().enabled = true;    // the overall settlement unit orbit around the system's star
        settlementUnitGo.GetSafeMonoBehaviourComponentInChildren<UISprite>().enabled = true;
        // no other orbits present,  // other possibles: Billboard, ScaleRelativeToCamera
        // TODO SensorRangeTracker
    }

    protected override void IssueFirstUnitCommand() {
        LogEvent();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

