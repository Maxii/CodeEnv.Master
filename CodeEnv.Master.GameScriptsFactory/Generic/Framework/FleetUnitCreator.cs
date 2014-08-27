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

    private static IList<FleetCmdModel> _allFleets = new List<FleetCmdModel>();
    public static IList<FleetCmdModel> AllFleets { get { return _allFleets; } }

    private static UnitFactory _factory;    // IMPROVE move back to AUnitCreator using References.IUnitFactory?

    public bool move;
    public bool attack;

    protected override void Awake() {
        base.Awake();
        if (_factory == null) {
            _factory = UnitFactory.Instance;
        }
    }

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

    protected override FleetCmdModel MakeCommand(IPlayer owner) {
        LogEvent();
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
        LogEvent();
        var candidateHQElements = _command.Elements.Where(e => GetValidHQElementCategories().Contains((e as ShipModel).Data.Category));
        if (candidateHQElements.IsNullOrEmpty()) {
            // _command might not hold a valid HQ Element if preset
            D.Warn("No valid HQElements for {0} found.", UnitName);
            candidateHQElements = _command.Elements;
        }
        _command.HQElement = RandomExtended<AUnitElementModel>.Choice(candidateHQElements) as ShipModel;
    }

    protected override bool DeployUnit() {
        LogEvent();
        // Fleets don't need to be deployed. They are already on location.
        return true;
    }

    protected override void BeginElementsOperations() {
        LogEvent();
        _elements.ForAll(e => e.CommenceOperations());
    }

    protected override void BeginCommandOperations() {
        LogEvent();
        _command.CommenceOperations();
    }

    protected override void __InitializeCommandIntel() {
        LogEvent();
        _command.gameObject.GetSafeMonoBehaviourComponent<FleetCmdView>().PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive;
    }

    protected override void EnableOtherWhenRunning() {
        D.Assert(GameStatus.Instance.IsRunning);
        gameObject.GetSafeMonoBehaviourComponentsInChildren<CameraLOSChangedRelay>().ForAll(relay => relay.enabled = true);
        gameObject.GetSafeMonoBehaviourComponentsInChildren<WeaponRangeMonitor>().ForAll(monitor => monitor.enabled = true);
        //gameObject.GetSafeMonoBehaviourComponentInChildren<UISprite>().enabled = true;    // doesn't appear to be needed

        // formation stations control enabled themselves when the assigned ship changes
        // no orbits or revolves present  // other possibles: Billboard, ScaleRelativeToCamera
        // TODO SensorRangeTracker
    }

    protected override void IssueFirstUnitCommand() {
        LogEvent();
        if (move) {
            if (attack) {
                __GetFleetAttackUnderway();
            }
            else {
                __GetFleetUnderway();
            }
        }
        //__GetFleetAttackUnderway();
        //__GetFleetUnderway();
    }

    private void __GetFleetUnderway() {
        LogEvent();
        IPlayer fleetOwner = _owner;
        IEnumerable<IDestinationTarget> moveTgts = FindObjectsOfType<StarbaseCmdModel>().Where(sb => sb.IsAlive && fleetOwner.IsRelationship(sb.Owner, DiplomaticRelations.Ally)).Cast<IDestinationTarget>();
        if (!moveTgts.Any()) {
            // in case no starbases qualify
            moveTgts = FindObjectsOfType<SettlementCmdModel>().Where(s => s.IsAlive && fleetOwner.IsRelationship(s.Owner, DiplomaticRelations.Ally)).Cast<IDestinationTarget>();
            if (!moveTgts.Any()) {
                // in case no Settlements qualify
                moveTgts = FindObjectsOfType<PlanetModel>().Where(p => p.IsAlive && p.Owner == TempGameValues.NoPlayer).Cast<IDestinationTarget>();
                if (!moveTgts.Any()) {
                    // in case no Planets qualify
                    moveTgts = FindObjectsOfType<SystemModel>().Where(sys => sys.Owner == TempGameValues.NoPlayer).Cast<IDestinationTarget>();
                    if (!moveTgts.Any()) {
                        // in case no Systems qualify
                        moveTgts = FindObjectsOfType<FleetCmdModel>().Where(f => f.IsAlive && fleetOwner.IsRelationship(f.Owner, DiplomaticRelations.Ally)).Cast<IDestinationTarget>();
                        if (!moveTgts.Any()) {
                            // in case no fleets qualify
                            moveTgts = FindObjectsOfType<SectorModel>().Where(s => s.Owner == TempGameValues.NoPlayer).Cast<IDestinationTarget>();
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
        //IDestinationTarget destination = moveTgts.MaxBy(mt => Vector3.SqrMagnitude(mt.Position - _transform.position));
        IDestinationTarget destination = moveTgts.MinBy(mt => Vector3.SqrMagnitude(mt.Position - _transform.position));

        _command.CurrentOrder = new FleetOrder(FleetDirective.MoveTo, destination, Speed.FleetStandard);
    }

    private void __GetFleetAttackUnderway() {
        LogEvent();
        IPlayer fleetOwner = _owner;
        IEnumerable<IMortalTarget> attackTgts = FindObjectsOfType<StarbaseCmdModel>().Where(sb => sb.IsAlive && fleetOwner.IsEnemyOf(sb.Owner)).Cast<IMortalTarget>();
        if (attackTgts.IsNullOrEmpty()) {
            // in case no Starbases qualify
            attackTgts = FindObjectsOfType<SettlementCmdModel>().Where(s => s.IsAlive && fleetOwner.IsEnemyOf(s.Owner)).Cast<IMortalTarget>();
            if (attackTgts.IsNullOrEmpty()) {
                // in case no Settlements qualify
                attackTgts = FindObjectsOfType<FleetCmdModel>().Where(f => f.IsAlive && fleetOwner.IsEnemyOf(f.Owner)).Cast<IMortalTarget>();
                if (attackTgts.IsNullOrEmpty()) {
                    // in case no Fleets qualify
                    attackTgts = FindObjectsOfType<PlanetModel>().Where(p => p.IsAlive && fleetOwner.IsEnemyOf(p.Owner)).Cast<IMortalTarget>();
                    if (attackTgts.IsNullOrEmpty()) {
                        // in case no enemy Planets qualify
                        attackTgts = FindObjectsOfType<PlanetModel>().Where(p => p.IsAlive && p.Owner == TempGameValues.NoPlayer).Cast<IMortalTarget>();
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
        IMortalTarget attackTgt = attackTgts.MinBy(t => Vector3.SqrMagnitude(t.Position - _transform.position));
        _command.CurrentOrder = new FleetOrder(FleetDirective.Attack, attackTgt);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

