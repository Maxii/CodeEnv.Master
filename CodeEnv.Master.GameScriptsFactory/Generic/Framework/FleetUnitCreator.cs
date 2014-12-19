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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Initialization class that deploys a fleet at the location of this FleetCreator. The fleet
/// deployed will simply be initialized if already present in the scene. If it is not present, then
/// it will be built and then initialized.
/// </summary>
[SerializeAll]
public class FleetUnitCreator : AUnitCreator<ShipItem, ShipCategory, ShipData, ShipStat, FleetCommandItem> {

    public bool move;
    public bool attack;

    // all starting units are now built and initialized during GameState.PrepareUnitsForOperations

    protected override ShipStat CreateElementStat(ShipCategory category, string elementName) {
        float mass = TempGameValues.__GetMass(category);
        float drag = 0.1F;
        var combatStance = Enums<ShipCombatStance>.GetRandom(excludeDefault: true);
        float maxTurnRate = UnityEngine.Random.Range(90F, 270F);

        float fullStlSpeed = UnityEngine.Random.Range(1.5F, 3.0F);  // planetoids ~ 0.1 units/hour, so Slow min = 0.15 units/hour
        float fullStlThrust = mass * drag * fullStlSpeed;
        float fullFtlThrust = fullStlThrust * TempGameValues.__FtlMultiplier;   // FullFtlSpeed ~ 15 - 30 units/hour

        return new ShipStat(elementName, mass, 50F, category, combatStance, maxTurnRate, drag, fullStlThrust, fullFtlThrust);
    }

    protected override FleetCommandItem MakeCommand(IPlayer owner) {
        LogEvent();
        var countermeasures = _availableCountermeasureStats.Shuffle().Take(countermeasuresPerCmd);
        FleetCmdStat cmdStat = new FleetCmdStat(UnitName, 10F, 100, Formation.Globe);
        FleetCommandItem cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSafeMonoBehaviourComponentInChildren<FleetCommandItem>();
            _factory.MakeInstance(cmdStat, countermeasures, owner, ref cmd);
        }
        else {
            cmd = _factory.MakeInstance(cmdStat, countermeasures, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        return cmd;
    }

    protected override ShipItem MakeElement(ShipStat shipStat, IEnumerable<WeaponStat> wStats, IEnumerable<CountermeasureStat> cmStats, IEnumerable<SensorStat> sensorStats) {
        return _factory.MakeInstance(shipStat, wStats, cmStats, sensorStats, _owner);
    }

    protected override void PopulateElement(ShipStat stat, IEnumerable<WeaponStat> wStats, IEnumerable<CountermeasureStat> cmStats, IEnumerable<SensorStat> sensorStats, ref ShipItem element) { // OPTIMIZE
        _factory.PopulateInstance(stat, wStats, cmStats, sensorStats, _owner, ref element);
    }

    protected override ShipCategory GetCategory(ShipStat stat) {
        return stat.Category;
    }

    protected override ShipCategory GetCategory(ShipItem element) {
        return element.category;
    }

    protected override ShipCategory[] ElementCategories {
        get { return new ShipCategory[] { ShipCategory.Frigate, ShipCategory.Destroyer, ShipCategory.Cruiser, ShipCategory.Carrier, ShipCategory.Dreadnaught }; }
    }

    protected override ShipCategory[] HQElementCategories {
        get { return new ShipCategory[] { ShipCategory.Cruiser, ShipCategory.Carrier, ShipCategory.Dreadnaught }; }
    }

    protected override void AssignHQElement() {
        LogEvent();
        var candidateHQElements = _command.Elements.Where(e => HQElementCategories.Contains((e as ShipItem).Data.Category));
        if (candidateHQElements.IsNullOrEmpty()) {
            // _command might not hold a valid HQ Element if preset
            D.Warn("No valid HQElements for {0} found.", UnitName);
            candidateHQElements = _command.Elements;
        }
        _command.HQElement = RandomExtended<AUnitElementItem>.Choice(candidateHQElements) as ShipItem;
    }


    protected override bool DeployUnit() {
        LogEvent();
        // Fleets don't need to be deployed. They are already on location.
        return true;
    }

    protected override void __IssueFirstUnitCommand() {
        LogEvent();
        UnityUtility.WaitOneToExecute(onWaitFinished: delegate {    // makes sure all targets are present in scene if they are suppossed to be
            if (move) {                                             // avoids script execution order issue when this creator receives IsRunning before other creators
                if (attack) {
                    __GetFleetAttackUnderway();
                }
                else {
                    __GetFleetUnderway();
                }
            }
        });
    }

    private void __GetFleetUnderway() {
        LogEvent();
        IPlayer fleetOwner = _owner;
        IEnumerable<INavigableTarget> moveTgts = StarbaseUnitCreator.AllUnitCommands.Where(sb => sb.IsAliveAndOperating && fleetOwner.IsRelationship(sb.Owner, DiplomaticRelationship.Ally)).Cast<INavigableTarget>();
        if (!moveTgts.Any()) {
            // in case no starbases qualify
            moveTgts = SettlementUnitCreator.AllUnitCommands.Where(s => s.IsAliveAndOperating && fleetOwner.IsRelationship(s.Owner, DiplomaticRelationship.Ally)).Cast<INavigableTarget>();
            if (!moveTgts.Any()) {
                // in case no Settlements qualify
                moveTgts = SystemCreator.AllPlanetoids.Where(p => p is PlanetItem && p.IsAliveAndOperating && p.Owner == TempGameValues.NoPlayer).Cast<INavigableTarget>();
                if (!moveTgts.Any()) {
                    // in case no Planets qualify
                    moveTgts = SystemCreator.AllSystems.Where(sys => sys.Owner == TempGameValues.NoPlayer).Cast<INavigableTarget>();
                    if (!moveTgts.Any()) {
                        // in case no Systems qualify
                        moveTgts = FleetUnitCreator.AllUnitCommands.Where(f => f.IsAliveAndOperating && fleetOwner.IsRelationship(f.Owner, DiplomaticRelationship.Ally)).Cast<INavigableTarget>();
                        if (!moveTgts.Any()) {
                            // in case no fleets qualify
                            moveTgts = SectorGrid.AllSectors.Where(s => s.Owner == TempGameValues.NoPlayer).Cast<INavigableTarget>();
                            if (!moveTgts.Any()) {
                                D.Warn("{0} can find no MoveTargets of any sort. MoveOrder has been cancelled.", UnitName);
                                return;
                            }
                            D.Log("{0} can find no MoveTargets that meet the selection criteria. Picking an unowned Sector.", UnitName);
                        }
                    }
                }
            }
        }
        INavigableTarget destination = moveTgts.MaxBy(mt => Vector3.SqrMagnitude(mt.Position - _transform.position));
        //INavigableTarget destination = moveTgts.MinBy(mt => Vector3.SqrMagnitude(mt.Position - _transform.position));
        D.Log("{0} destination is {1}.", UnitName, destination.FullName);
        _command.CurrentOrder = new FleetOrder(FleetDirective.Move, destination, Speed.FleetStandard);
    }

    private void __GetFleetAttackUnderway() {
        LogEvent();
        IPlayer fleetOwner = _owner;
        IEnumerable<IUnitAttackableTarget> attackTgts = StarbaseUnitCreator.AllUnitCommands.Where(sb => sb.IsAliveAndOperating && fleetOwner.IsEnemyOf(sb.Owner)).Cast<IUnitAttackableTarget>();
        if (attackTgts.IsNullOrEmpty()) {
            // in case no Starbases qualify
            attackTgts = SettlementUnitCreator.AllUnitCommands.Where(s => s.IsAliveAndOperating && fleetOwner.IsEnemyOf(s.Owner)).Cast<IUnitAttackableTarget>();
            if (attackTgts.IsNullOrEmpty()) {
                // in case no Settlements qualify
                attackTgts = FleetUnitCreator.AllUnitCommands.Where(f => f.IsAliveAndOperating && fleetOwner.IsEnemyOf(f.Owner)).Cast<IUnitAttackableTarget>();
                if (attackTgts.IsNullOrEmpty()) {
                    // in case no Fleets qualify
                    attackTgts = SystemCreator.AllPlanetoids.Where(p => p is PlanetItem && p.IsAliveAndOperating && fleetOwner.IsEnemyOf(p.Owner)).Cast<IUnitAttackableTarget>();
                    if (attackTgts.IsNullOrEmpty()) {
                        // in case no enemy Planets qualify
                        D.Log("{0} can find no AttackTargets of any sort. Defaulting to __GetFleetUnderway().", UnitName);
                        __GetFleetUnderway();
                        return;
                    }
                }
            }
        }
        IUnitAttackableTarget attackTgt = attackTgts.MinBy(t => Vector3.SqrMagnitude(t.Position - _transform.position));
        //IUnitAttackableTarget attackTgt = attackTgts.MaxBy(t => Vector3.SqrMagnitude(t.Position - _transform.position));
        D.Log("{0} attack target is {1}.", UnitName, attackTgt.FullName);
        _command.CurrentOrder = new FleetOrder(FleetDirective.Attack, attackTgt);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

