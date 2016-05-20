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
using MoreLinq;

/// <summary>
/// Initialization class that deploys a fleet at the location of this FleetCreator. The fleet
/// deployed will simply be initialized if already present in the scene. If it is not present, then
/// it will be built and then initialized.
/// </summary>  // Has Editor
public class FleetUnitCreator : AUnitCreator<ShipItem, ShipHullCategory, ShipData, ShipHullStat, FleetCmdItem> {

    /// <summary>
    /// Indicates whether this Fleet should move to a destination.
    /// </summary>
    public bool move;

    /// <summary>
    /// If fleet is to move to a destination, should it pick the farthest or the closest?
    /// </summary>
    public bool findFarthest;

    /// <summary>
    /// The fleet is to move to a destination, should it attack it?
    /// </summary>
    public bool attack;

    /// <summary>
    /// Indicates whether the FTL drive of all the ships in the fleet should start damaged, aka not operational.
    /// They can still repair themselves.
    /// </summary>
    public bool ftlStartsDamaged;

    /// <summary>
    /// The exclusions when randomly picking ShipCombatStances.
    /// </summary>
    public ShipCombatStanceExclusions stanceExclusions;

    // all starting units are now built and initialized during GameState.PrepareUnitsForOperations

    protected override ShipHullStat CreateElementHullStat(ShipHullCategory hullCat, string elementName) {
        float hullMass = __GetHullMass(hullCat);
        float drag = __GetHullDrag(hullCat);    //0.1F;
        float science = hullCat == ShipHullCategory.Science ? 10F : Constants.ZeroF;
        float culture = hullCat == ShipHullCategory.Support || hullCat == ShipHullCategory.Colonizer ? 2F : Constants.ZeroF;
        float income = __GetIncome(hullCat);
        float expense = __GetExpense(hullCat);
        Vector3 hullDimensions = __GetHullDimensions(hullCat);
        return new ShipHullStat(hullCat, elementName, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F,
            hullMass, drag, 0F, expense, 50F, new DamageStrength(2F, 2F, 2F), hullDimensions, science, culture, income);
    }

    protected override void MakeAndRecordDesign(string designName, ShipHullStat hullStat, IEnumerable<AWeaponStat> weaponStats, IEnumerable<PassiveCountermeasureStat> passiveCmStats, IEnumerable<ActiveCountermeasureStat> activeCmStats, IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats) {
        ShipHullCategory hullCategory = hullStat.HullCategory;
        var engineStat = __MakeEnginesStat(hullCategory);
        var combatStance = SelectCombatStance();
        var weaponDesigns = _factory.__MakeWeaponDesigns(hullCategory, weaponStats);
        var design = new ShipDesign(_owner, designName, hullStat, engineStat, combatStance, weaponDesigns, passiveCmStats, activeCmStats, sensorStats, shieldGenStats);
        GameManager.Instance.PlayersDesigns.Add(design);
    }

    protected override FleetCmdItem MakeCommand(Player owner) {
        LogEvent();
        var countermeasures = _availablePassiveCountermeasureStats.Shuffle().Take(countermeasuresPerCmd);
        UnitCmdStat cmdStat = __MakeCmdStat();
        CameraFleetCmdStat cameraStat = __MakeCmdCameraStat(TempGameValues.ShipMaxRadius);
        FleetCmdItem cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSingleComponentInChildren<FleetCmdItem>();
            _factory.PopulateInstance(cmdStat, cameraStat, countermeasures, owner, ref cmd);
        }
        else {
            cmd = _factory.MakeInstance(cmdStat, cameraStat, countermeasures, owner, gameObject);
        }
        cmd.IsTrackingLabelEnabled = enableTrackingLabel;
        return cmd;
    }

    protected override ShipItem MakeElement(string designName) {
        ShipDesign design = GameManager.Instance.PlayersDesigns.GetShipDesign(_owner, designName);
        CameraFollowableStat cameraStat = __MakeElementCameraStat(design.HullStat);
        var ship = _factory.MakeInstance(_owner, cameraStat, design);
        ship.Data.IsFtlDamaged = ftlStartsDamaged;
        return ship;
    }

    protected override void PopulateElement(string designName, ref ShipItem element) {
        ShipDesign design = GameManager.Instance.PlayersDesigns.GetShipDesign(_owner, designName);
        CameraFollowableStat cameraStat = __MakeElementCameraStat(design.HullStat);
        _factory.PopulateInstance(_owner, cameraStat, design, ref element);
        element.Data.IsFtlDamaged = ftlStartsDamaged;
    }

    protected override ShipHullCategory GetCategory(ShipHullStat hullStat) { return hullStat.HullCategory; }

    protected override ShipHullCategory GetCategory(AHull hull) { return (hull as ShipHull).HullCategory; }

    protected override ShipHullCategory[] ElementCategories {
        get {
            return new ShipHullCategory[] { ShipHullCategory.Frigate, ShipHullCategory.Destroyer, ShipHullCategory.Cruiser, ShipHullCategory.Carrier, ShipHullCategory.Dreadnought,
        ShipHullCategory.Colonizer, ShipHullCategory.Science, ShipHullCategory.Troop, ShipHullCategory.Support};
        }
    }

    protected override ShipHullCategory[] HQElementCategories {
        get { return new ShipHullCategory[] { ShipHullCategory.Cruiser, ShipHullCategory.Carrier, ShipHullCategory.Dreadnought }; }
    }

    protected override void AssignHQElement() {
        LogEvent();
        var candidateHQElements = _command.Elements.Where(e => HQElementCategories.Contains((e as ShipItem).Data.HullCategory));
        if (candidateHQElements.IsNullOrEmpty()) {
            // _command might not hold a valid HQ Element if preset
            D.Warn("No valid HQElements for {0} found.", UnitName);
            candidateHQElements = _command.Elements;
        }
        var hqElement = RandomExtended.Choice(candidateHQElements) as ShipItem;
        _command.HQElement = hqElement;
    }

    protected override bool DeployUnit() {
        LogEvent();
        // Fleets don't need to be deployed. They are already on location.
        return true;
    }

    protected override void __IssueFirstUnitOrder(Action onCompleted) {
        LogEvent();
        WaitJobUtility.WaitForHours(1F, waitFinished: delegate {    // makes sure Owner's knowledge of universe has been constructed before selecting its target
            if (move) {                                               // avoids script execution order issue when this creator receives IsRunning before other creators
                if (attack) {
                    __GetFleetAttackUnderway();
                }
                else {
                    __GetFleetUnderway();
                }
            }
            onCompleted();
        });
    }

    private void __GetFleetUnderway() {
        LogEvent();
        Player fleetOwner = _owner;
        var fleetOwnerKnowledge = GameManager.Instance.PlayersKnowledge.GetKnowledge(fleetOwner);
        IEnumerable<IFleetNavigable> moveTgts = fleetOwnerKnowledge.Starbases.Where(sb => !fleetOwner.IsEnemyOf(sb.Owner)).Cast<IFleetNavigable>();
        if (!moveTgts.Any()) {
            // in case no starbases qualify
            moveTgts = fleetOwnerKnowledge.Settlements.Where(s => !fleetOwner.IsEnemyOf(s.Owner)).Cast<IFleetNavigable>();
            if (!moveTgts.Any()) {
                // in case no Settlements qualify
                moveTgts = fleetOwnerKnowledge.Planets.Where(p => !fleetOwner.IsEnemyOf(p.Owner)).Cast<IFleetNavigable>();
                if (!moveTgts.Any()) {
                    // in case no Planets qualify
                    moveTgts = fleetOwnerKnowledge.Systems.Where(sys => !fleetOwner.IsEnemyOf(sys.Owner)).Cast<IFleetNavigable>();
                    if (!moveTgts.Any()) {
                        // in case no Systems qualify
                        moveTgts = fleetOwnerKnowledge.Stars.Where(star => star.GetIntelCoverage(fleetOwner) == IntelCoverage.Basic || !fleetOwner.IsEnemyOf(star.Owner)).Cast<IFleetNavigable>();
                        if (!moveTgts.Any()) {
                            // in case no Stars qualify
                            moveTgts = SectorGrid.Instance.AllSectors.Where(s => s.Owner == TempGameValues.NoPlayer).Cast<IFleetNavigable>();
                            if (!moveTgts.Any()) {
                                D.Error("{0} can find no MoveTargets of any sort. MoveOrder has been canceled.", UnitName);
                                return;
                            }
                            D.Log("{0} can find no MoveTargets that meet the selection criteria. Picking an unowned Sector.", UnitName);
                        }
                    }
                }
            }
        }
        IFleetNavigable destination;
        if (findFarthest) {
            destination = moveTgts.MaxBy(mt => Vector3.SqrMagnitude(mt.Position - transform.position));
        }
        else {
            destination = moveTgts.MinBy(mt => Vector3.SqrMagnitude(mt.Position - transform.position));
        }
        //D.Log("{0} destination is {1}.", UnitName, destination.FullName);
        _command.CurrentOrder = new FleetOrder(FleetDirective.Move, OrderSource.CmdStaff, destination);
    }

    private void __GetFleetAttackUnderway() {
        LogEvent();
        Player fleetOwner = _owner;
        var fleetOwnerKnowledge = GameManager.Instance.PlayersKnowledge.GetKnowledge(fleetOwner);
        IEnumerable<IUnitAttackableTarget> attackTgts = fleetOwnerKnowledge.Fleets.Cast<IUnitAttackableTarget>().Where(f => f.IsAttackingAllowedBy(fleetOwner));
        if (attackTgts.IsNullOrEmpty()) {
            // in case no Fleets qualify
            attackTgts = fleetOwnerKnowledge.Starbases.Cast<IUnitAttackableTarget>().Where(sb => sb.IsAttackingAllowedBy(fleetOwner));
            if (attackTgts.IsNullOrEmpty()) {
                // in case no Starbases qualify
                attackTgts = fleetOwnerKnowledge.Settlements.Cast<IUnitAttackableTarget>().Where(s => s.IsAttackingAllowedBy(fleetOwner));
                if (attackTgts.IsNullOrEmpty()) {
                    // in case no Settlements qualify
                    attackTgts = fleetOwnerKnowledge.Planets.Cast<IUnitAttackableTarget>().Where(p => p.IsAttackingAllowedBy(fleetOwner));
                    if (attackTgts.IsNullOrEmpty()) {
                        D.Log("{0} can find no AttackTargets of any sort. Defaulting to __GetFleetUnderway().", UnitName);
                        __GetFleetUnderway();
                        return;
                    }
                }
            }
        }
        IUnitAttackableTarget attackTgt;
        if (findFarthest) {
            attackTgt = attackTgts.MaxBy(t => Vector3.SqrMagnitude(t.Position - transform.position));
        }
        else {
            attackTgt = attackTgts.MinBy(t => Vector3.SqrMagnitude(t.Position - transform.position));
        }
        //D.Log("{0} attack target is {1}.", UnitName, attackTgt.FullName);
        _command.CurrentOrder = new FleetOrder(FleetDirective.Attack, OrderSource.CmdStaff, attackTgt);
    }

    protected override int GetMaxLosWeaponsAllowed(ShipHullCategory hullCategory) {
        return hullCategory.__MaxLOSWeapons();
    }

    protected override int GetMaxMissileWeaponsAllowed(ShipHullCategory hullCategory) {
        return hullCategory.__MaxMissileWeapons();
    }

    private ShipCombatStance SelectCombatStance() {
        if (stanceExclusions == ShipCombatStanceExclusions.AllExceptBalanced) {
            return ShipCombatStance.Balanced;
        }
        if (stanceExclusions == ShipCombatStanceExclusions.AllExceptPointBlank) {
            return ShipCombatStance.PointBlank;
        }
        if (stanceExclusions == ShipCombatStanceExclusions.AllExceptStandoff) {
            return ShipCombatStance.Standoff;
        }
        else {
            IList<ShipCombatStance> excludedCombatStances = new List<ShipCombatStance>() { default(ShipCombatStance) };
            if (stanceExclusions == ShipCombatStanceExclusions.Disengage) {
                excludedCombatStances.Add(ShipCombatStance.Disengage);
            }
            else if (stanceExclusions == ShipCombatStanceExclusions.DefensiveAndDisengage) {
                excludedCombatStances.Add(ShipCombatStance.Disengage);
                excludedCombatStances.Add(ShipCombatStance.Defensive);
            }
            return Enums<ShipCombatStance>.GetRandomExcept(excludedCombatStances.ToArray());
        }
    }

    private UnitCmdStat __MakeCmdStat() {
        float maxHitPts = 10F;
        int maxCmdEffect = 100;
        Formation formation = Enums<Formation>.GetRandomExcept(default(Formation), Formation.Wedge);
        return new UnitCmdStat(UnitName, maxHitPts, maxCmdEffect, formation);
    }

    private CameraFleetCmdStat __MakeCmdCameraStat(float maxElementRadius) {
        float minViewDistance = maxElementRadius + 1F;
        float optViewDistanceAdder = 1F;    // the additional distance outside of the UnitRadius of the fleet
        // there is no optViewDistance value for a FleetCmd CameraStat
        return new CameraFleetCmdStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    }

    private CameraFollowableStat __MakeElementCameraStat(ShipHullStat hullStat) {
        ShipHullCategory hullCat = hullStat.HullCategory;
        float fov;
        switch (hullCat) {
            case ShipHullCategory.Dreadnought:
            case ShipHullCategory.Carrier:
            case ShipHullCategory.Troop:
                fov = 70F;
                break;
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Colonizer:
            case ShipHullCategory.Science:
                fov = 65F;
                break;
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Support:
                fov = 60F;
                break;
            case ShipHullCategory.Frigate:
                fov = 55F;
                break;
            case ShipHullCategory.Fighter:
            case ShipHullCategory.Scout:
            case ShipHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
        float radius = hullStat.HullDimensions.magnitude / 2F;
        //D.Log("Radius of {0} is {1:0.##}.", hullCat.GetValueName(), radius);
        float minViewDistance = radius * 2F;
        float optViewDistance = radius * 3F;
        float distanceDampener = 3F;    // default
        float rotationDampener = 10F;   // ships can change direction pretty fast
        return new CameraFollowableStat(minViewDistance, optViewDistance, fov, distanceDampener, rotationDampener);
    }

    private EnginesStat __MakeEnginesStat(ShipHullCategory hullCategory) {
        float maxTurnRate = UnityEngine.Random.Range(90F, 270F);
        float singleEngineSize = 10F;
        float singleEngineMass = __GetEngineMass(hullCategory);
        float singleEngineExpense = 5F;

        float fullStlPropulsionPower = __GetFullStlPropulsionPower(hullCategory);   // FullFtlOpenSpaceSpeed ~ 30-40 units/hour, FullStlSystemSpeed ~ 1.2 - 1.6 units/hour
        return new EnginesStat("EngineName", AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", fullStlPropulsionPower, maxTurnRate, singleEngineSize, singleEngineMass, singleEngineExpense, TempGameValues.__StlToFtlPropulsionPowerFactor, engineQty: 1);
    }

    private float __GetIncome(ShipHullCategory category) {
        switch (category) {
            case ShipHullCategory.Support:
                return 3F;
            case ShipHullCategory.Carrier:
            case ShipHullCategory.Colonizer:
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Dreadnought:
            case ShipHullCategory.Fighter:
            case ShipHullCategory.Frigate:
            case ShipHullCategory.Science:
            case ShipHullCategory.Scout:
            case ShipHullCategory.Troop:
                return Constants.ZeroF;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(category));
        }
    }

    private float __GetExpense(ShipHullCategory category) {
        switch (category) {
            case ShipHullCategory.Carrier:
            case ShipHullCategory.Dreadnought:
            case ShipHullCategory.Troop:
            case ShipHullCategory.Colonizer:
                return 5F;
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Support:  //TODO need Trader
            case ShipHullCategory.Science:
                return 3F;
            case ShipHullCategory.Destroyer:
                return 2F;
            case ShipHullCategory.Fighter:
            case ShipHullCategory.Frigate:
            case ShipHullCategory.Scout:
                return 1F;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(category));
        }
    }

    private float __GetHullMass(ShipHullCategory hullCat) {
        switch (hullCat) {
            case ShipHullCategory.Frigate:
                return 50F;                     // mass * drag = 2.5
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Support:
                return 100F;                     // mass * drag = 8
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Colonizer:
            case ShipHullCategory.Science:
                return 200F;                     // mass * drag = 20
            case ShipHullCategory.Dreadnought:
            case ShipHullCategory.Troop:
                return 400F;                     // mass * drag = 60
            case ShipHullCategory.Carrier:
                return 500F;                     // mass * drag = 125
            case ShipHullCategory.Scout:
            case ShipHullCategory.Fighter:
            case ShipHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
    }

    /// <summary>
    ///ShipHull Drag in Topography.OpenSpace.
    /// </summary>
    /// <param name="hullCat">The hull cat.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private float __GetHullDrag(ShipHullCategory hullCat) {
        switch (hullCat) {
            case ShipHullCategory.Frigate:
                return .05F;
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Support:
                return .08F;
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Colonizer:
            case ShipHullCategory.Science:
                return .10F;
            case ShipHullCategory.Dreadnought:
            case ShipHullCategory.Troop:
                return .15F;
            case ShipHullCategory.Carrier:
                return .25F;
            case ShipHullCategory.Scout:
            case ShipHullCategory.Fighter:
            case ShipHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
    }

    private float __GetEngineMass(ShipHullCategory hull) {
        return __GetHullMass(hull) * 0.10F;
    }

    /// <summary>
    /// Gets the power generated by the STL engines when operating at Full capability.
    /// </summary>
    /// <param name="hull">The ship hull.</param>
    /// <returns></returns>
    private float __GetFullStlPropulsionPower(ShipHullCategory hull) {
        float fastestFullFtlSpeedTgt = TempGameValues.__TargetFtlOpenSpaceFullSpeed; // 40F
        float slowestFullFtlSpeedTgt = fastestFullFtlSpeedTgt * 0.75F;   // this way, the slowest Speed.OneThird speed >= Speed.Slow
        float fullFtlSpeedTgt = UnityEngine.Random.Range(slowestFullFtlSpeedTgt, fastestFullFtlSpeedTgt);
        float hullMass = __GetHullMass(hull);   // most but not all of the mass of the ship
        float hullOpenSpaceDrag = __GetHullDrag(hull);

        //float reqdFullFtlPower = fullFtlSpeedTgt * hullMass * hullOpenSpaceDrag;
        float reqdFullFtlPower = GameUtility.CalculateReqdPropulsionPower(fullFtlSpeedTgt, hullMass, hullOpenSpaceDrag);
        return reqdFullFtlPower / TempGameValues.__StlToFtlPropulsionPowerFactor;
    }

    private Vector3 __GetHullDimensions(ShipHullCategory hullCat) {
        Vector3 dimensions;
        switch (hullCat) {  // 10.28.15 Hull collider dimensions increased to encompass turrets, 11.20.15 reduced mesh scale from 2 to 1
            case ShipHullCategory.Frigate:
                dimensions = new Vector3(.02F, .03F, .05F); //new Vector3(.04F, .035F, .10F);
                break;
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Support:
                dimensions = new Vector3(.06F, .035F, .10F);    //new Vector3(.08F, .05F, .18F);
                break;
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Science:
            case ShipHullCategory.Colonizer:
                dimensions = new Vector3(.09F, .05F, .16F); //new Vector3(.15F, .08F, .30F); 
                break;
            case ShipHullCategory.Dreadnought:
            case ShipHullCategory.Troop:
                dimensions = new Vector3(.12F, .05F, .25F); //new Vector3(.21F, .07F, .45F);
                break;
            case ShipHullCategory.Carrier:
                dimensions = new Vector3(.10F, .06F, .32F); // new Vector3(.20F, .10F, .60F); 
                break;
            case ShipHullCategory.Fighter:
            case ShipHullCategory.Scout:
            case ShipHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
        float radius = dimensions.magnitude / 2F;
        D.Warn(radius > TempGameValues.ShipMaxRadius, "Ship {0}.Radius {1:0.####} > MaxRadius {2:0.##}.", hullCat.GetValueName(), radius, TempGameValues.ShipMaxRadius);
        return dimensions;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested Classes

    public enum ShipCombatStanceExclusions {

        /// <summary>
        /// ShipCombatStance choice will be random.
        /// </summary>
        None,

        /// <summary>
        /// ShipCombatStance choice will be random, excluding Disengage.
        /// </summary>
        Disengage,

        /// <summary>
        /// ShipCombatStance choice will be random, excluding Disengage and Defensive.
        /// </summary>
        DefensiveAndDisengage,

        AllExceptStandoff,

        AllExceptBalanced,

        AllExceptPointBlank
    }

    #endregion

}

