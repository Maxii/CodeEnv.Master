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
public class FleetUnitCreator : AUnitCreator<ShipModel, ShipCategory, ShipData, ShipStat, FleetCmdModel> {

    private UnitFactory _factory;   // not accesible from AUnitCreator

    protected override void Awake() {
        base.Awake();
        _factory = UnitFactory.Instance;
    }

    // all starting units are now built and initialized during GameState.PrepareUnitsForOperations

    protected override ShipStat CreateElementStat(ShipCategory category, string elementName) {
        float mass = TempGameValues.__GetMass(category);
        float drag = 0.1F;
        var combatStance = Enums<ShipCombatStance>.GetRandom(excludeDefault: true);
        float maxTurnRate = UnityEngine.Random.Range(300F, 315F);
        float fullThrust = mass * drag * UnityEngine.Random.Range(2F, 5F); // MaxThrust = Mass * Drag * MaxSpeed;

        return new ShipStat(elementName, mass, 50F, category, combatStance, maxTurnRate, drag, fullThrust);
    }

    protected override FleetCmdModel MakeCommand(IPlayer owner) {
        FleetCmdStat cmdStat = new FleetCmdStat(UnitName, 10F, 100, Formation.Globe, new CombatStrength(0F, 5F, 0F, 5F, 0F, 5F));
        FleetCmdModel cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSafeMonoBehaviourComponentInChildren<FleetCmdModel>();
            var existingCmdReference = cmd;
            bool isCmdCompatibleWithOwner = _factory.MakeFleetCmdInstance(cmdStat, owner, ref cmd);
            if (!isCmdCompatibleWithOwner) {
                Destroy(existingCmdReference.gameObject);
            }
        }
        else {
            cmd = _factory.MakeFleetCmdInstance(cmdStat, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        return cmd;
    }

    protected override ShipModel MakeElement(ShipStat shipStat, IEnumerable<WeaponStat> weaponStats, IPlayer owner) {
        return _factory.MakeInstance(shipStat, weaponStats, owner);
    }

    /// <summary>
    /// Makes an element based off of the provided element. Returns true if the provided element is compatible
    /// with the provided owner, false if it is not and had to be replaced. If an element is replaced, then clients
    /// are responsible for destroying the original provided element.
    /// </summary>
    /// <param name="stat">The stat.</param>
    /// <param name="weaponStats">The weapon stats.</param>
    /// <param name="owner">The owner.</param>
    /// <param name="element">The element.</param>
    /// <returns></returns>
    protected override bool MakeElement(ShipStat stat, IEnumerable<WeaponStat> weaponStats, IPlayer owner, ref ShipModel element) { // OPTIMIZE
        return _factory.MakeInstance(stat, weaponStats, owner, ref element);
    }

    protected override ShipCategory GetCategory(ShipStat stat) {
        return stat.Category;
    }

    protected override ShipCategory[] GetValidElementCategories() {
        return new ShipCategory[] { ShipCategory.Frigate, ShipCategory.Destroyer, ShipCategory.Cruiser, ShipCategory.Carrier, ShipCategory.Dreadnaught };
    }

    protected override ShipCategory[] GetValidHQElementCategories() {
        return new ShipCategory[] { ShipCategory.Cruiser, ShipCategory.Carrier, ShipCategory.Dreadnaught };
    }

    protected override void AssignHQElement() {
        var candidateHQElements = _command.Elements.Where(e => GetValidHQElementCategories().Contains((e as ShipModel).Data.Category));
        if (candidateHQElements.IsNullOrEmpty()) {
            // _command might not hold a valid HQ Element if preset
            candidateHQElements = _command.Elements;
        }
        _command.HQElement = RandomExtended<IElementModel>.Choice(candidateHQElements) as ShipModel;
    }

    protected override void DeployUnit() {
        // Fleets don't need to be deployed. They are already on location.
    }

    protected override void BeginElementsOperations() {
        _elements.ForAll(e => (e as ShipModel).CurrentState = ShipState.Idling);
    }

    protected override void BeginCommandOperations() {
        _command.CurrentState = FleetState.Idling;
    }

    protected override void __InitializeCommandIntel() {
        _command.gameObject.GetSafeInterface<ICommandViewable>().PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive;
    }

    protected override void IssueFirstUnitCommand() {
        __GetFleetAttackUnderway();
        //__GetFleetUnderway();
    }

    private void __GetFleetUnderway() {
        IDestinationTarget destination = null; // = FindObjectOfType<SettlementCmdModel>();
        if (destination == null) {
            // in case Settlements are disabled
            destination = new StationaryLocation(_transform.position + UnityEngine.Random.onUnitSphere * 20F);
        }
        _command.CurrentOrder = new FleetOrder(FleetOrders.MoveTo, destination, Speed.FleetStandard);
    }

    private void __GetFleetAttackUnderway() {
        IPlayer fleetOwner = _owner;
        IEnumerable<IMortalTarget> attackTgts = FindObjectsOfType<StarbaseCmdModel>().Where(sb => sb.IsOperational && fleetOwner.IsEnemyOf(sb.Owner)).Cast<IMortalTarget>();
        if (attackTgts.IsNullOrEmpty()) {
            // in case no Starbases qualify
            attackTgts = FindObjectsOfType<SettlementCmdModel>().Where(s => s.IsOperational && fleetOwner.IsEnemyOf(s.Owner)).Cast<IMortalTarget>();
            if (attackTgts.IsNullOrEmpty()) {
                // in case no Settlements qualify
                attackTgts = FindObjectsOfType<FleetCmdModel>().Where(f => f.IsOperational && fleetOwner.IsEnemyOf(f.Owner)).Cast<IMortalTarget>();
                if (attackTgts.IsNullOrEmpty()) {
                    // in case no Fleets qualify
                    attackTgts = FindObjectsOfType<PlanetoidModel>().Where(p => p.IsOperational && fleetOwner.IsEnemyOf(p.Owner)).Cast<IMortalTarget>();
                    if (attackTgts.IsNullOrEmpty()) {
                        // in case no enemy Planetoids qualify
                        attackTgts = FindObjectsOfType<PlanetoidModel>().Where(p => p.IsOperational && p.Owner == TempGameValues.NoPlayer).Cast<IMortalTarget>();
                        if (attackTgts.Any()) {
                            D.Log("{0} can find no AttackTargets that meet the enemy selection criteria. Picking an unowned Planet.", UnitName);
                        }
                        else {
                            D.Warn("{0} can find no AttackTargets of any sort. Defaulting to __GetFleetUnderway().", UnitName);
                            __GetFleetUnderway();
                            return;
                        }
                    }
                }
            }
        }
        IMortalTarget attackTgt = attackTgts.MinBy(t => Vector3.Distance(t.Position, _transform.position));
        _command.CurrentOrder = new FleetOrder(FleetOrders.Attack, attackTgt);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

