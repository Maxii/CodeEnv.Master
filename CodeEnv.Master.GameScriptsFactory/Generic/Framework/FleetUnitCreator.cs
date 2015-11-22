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
using System.Collections;
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
/// </summary>
public class FleetUnitCreator : AUnitCreator<ShipItem, ShipHullCategory, ShipData, ShipHullStat, FleetCmdItem> {

    public bool move;   // Has Editor
    public bool attack;

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
        var combatStance = Enums<ShipCombatStance>.GetRandom(excludeDefault: true);
        var engineStat = __MakeEngineStat(hullCategory);

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
        return _factory.MakeInstance(_owner, cameraStat, design);
    }

    protected override void PopulateElement(string designName, ref ShipItem element) {
        ShipDesign design = GameManager.Instance.PlayersDesigns.GetShipDesign(_owner, designName);
        CameraFollowableStat cameraStat = __MakeElementCameraStat(design.HullStat);
        _factory.PopulateInstance(_owner, cameraStat, design, ref element);
    }

    protected override ShipHullCategory GetCategory(ShipHullStat hullStat) { return hullStat.HullCategory; }

    protected override ShipHullCategory GetCategory(AHull hull) { return (hull as ShipHull).HullCategory; }

    protected override ShipHullCategory[] ElementCategories {
        get {
            return new ShipHullCategory[] { ShipHullCategory.Frigate, ShipHullCategory.Destroyer, ShipHullCategory.Cruiser, ShipHullCategory.Carrier, ShipHullCategory.Dreadnaught,
        ShipHullCategory.Colonizer, ShipHullCategory.Science, ShipHullCategory.Troop, ShipHullCategory.Support};
        }
    }

    protected override ShipHullCategory[] HQElementCategories {
        get { return new ShipHullCategory[] { ShipHullCategory.Cruiser, ShipHullCategory.Carrier, ShipHullCategory.Dreadnaught }; }
    }

    protected override void AssignHQElement() {
        LogEvent();
        var candidateHQElements = _command.Elements.Where(e => HQElementCategories.Contains((e as ShipItem).Data.HullCategory));
        if (candidateHQElements.IsNullOrEmpty()) {
            // _command might not hold a valid HQ Element if preset
            D.Warn("No valid HQElements for {0} found.", UnitName);
            candidateHQElements = _command.Elements;
        }
        _command.HQElement = RandomExtended.Choice(candidateHQElements) as ShipItem;
    }

    protected override bool DeployUnit() {
        LogEvent();
        // Fleets don't need to be deployed. They are already on location.
        return true;
    }

    protected override void __IssueFirstUnitOrder(Action onCompleted) {
        LogEvent();
        UnityUtility.WaitOneToExecute(onWaitFinished: delegate {    // makes sure all targets are present in scene if they are supposed to be
            if (move) {                                             // avoids script execution order issue when this creator receives IsRunning before other creators
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
        IEnumerable<INavigableTarget> moveTgts = StarbaseUnitCreator.AllUnitCommands.Where(sb => sb.IsOperational && fleetOwner.IsRelationship(sb.Owner, DiplomaticRelationship.Ally)).Cast<INavigableTarget>();
        if (!moveTgts.Any()) {
            // in case no starbases qualify
            moveTgts = SettlementUnitCreator.AllUnitCommands.Where(s => s.IsOperational && fleetOwner.IsRelationship(s.Owner, DiplomaticRelationship.Ally)).Cast<INavigableTarget>();
            if (!moveTgts.Any()) {
                // in case no Settlements qualify
                moveTgts = SystemCreator.AllPlanetoids.Where(p => p is PlanetItem && p.IsOperational && p.Owner == TempGameValues.NoPlayer).Cast<INavigableTarget>();
                if (!moveTgts.Any()) {
                    // in case no Planets qualify
                    moveTgts = SystemCreator.AllSystems.Where(sys => sys.Owner == TempGameValues.NoPlayer).Cast<INavigableTarget>();
                    if (!moveTgts.Any()) {
                        // in case no Systems qualify
                        moveTgts = FleetUnitCreator.AllUnitCommands.Where(f => f.IsOperational && fleetOwner.IsRelationship(f.Owner, DiplomaticRelationship.Ally)).Cast<INavigableTarget>();
                        if (!moveTgts.Any()) {
                            // in case no fleets qualify
                            moveTgts = SectorGrid.Instance.AllSectors.Where(s => s.Owner == TempGameValues.NoPlayer).Cast<INavigableTarget>();
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
        INavigableTarget destination = moveTgts.MaxBy(mt => Vector3.SqrMagnitude(mt.Position - transform.position));
        //INavigableTarget destination = moveTgts.MinBy(mt => Vector3.SqrMagnitude(mt.Position - transform.position));
        D.Log("{0} destination is {1}.", UnitName, destination.FullName);
        _command.CurrentOrder = new FleetOrder(FleetDirective.Move, destination, Speed.FleetStandard);
    }

    private void __GetFleetAttackUnderway() {
        LogEvent();
        Player fleetOwner = _owner;
        IEnumerable<IUnitAttackableTarget> attackTgts = StarbaseUnitCreator.AllUnitCommands.Where(sb => sb.IsOperational && fleetOwner.IsEnemyOf(sb.Owner)).Cast<IUnitAttackableTarget>();
        if (attackTgts.IsNullOrEmpty()) {
            // in case no Starbases qualify
            attackTgts = SettlementUnitCreator.AllUnitCommands.Where(s => s.IsOperational && fleetOwner.IsEnemyOf(s.Owner)).Cast<IUnitAttackableTarget>();
            if (attackTgts.IsNullOrEmpty()) {
                // in case no Settlements qualify
                attackTgts = FleetUnitCreator.AllUnitCommands.Where(f => f.IsOperational && fleetOwner.IsEnemyOf(f.Owner)).Cast<IUnitAttackableTarget>();
                if (attackTgts.IsNullOrEmpty()) {
                    // in case no Fleets qualify
                    attackTgts = SystemCreator.AllPlanetoids.Where(p => p is PlanetItem && p.IsOperational && fleetOwner.IsEnemyOf(p.Owner)).Cast<IUnitAttackableTarget>();
                    if (attackTgts.IsNullOrEmpty()) {
                        // in case no enemy Planets qualify
                        D.Log("{0} can find no AttackTargets of any sort. Defaulting to __GetFleetUnderway().", UnitName);
                        __GetFleetUnderway();
                        return;
                    }
                }
            }
        }
        IUnitAttackableTarget attackTgt = attackTgts.MinBy(t => Vector3.SqrMagnitude(t.Position - transform.position));
        //IUnitAttackableTarget attackTgt = attackTgts.MaxBy(t => Vector3.SqrMagnitude(t.Position - transform.position));
        D.Log("{0} attack target is {1}.", UnitName, attackTgt.FullName);
        _command.CurrentOrder = new FleetOrder(FleetDirective.Attack, attackTgt);
    }

    protected override int GetMaxLosWeaponsAllowed(ShipHullCategory hullCategory) {
        return hullCategory.__MaxLOSWeapons();
    }

    protected override int GetMaxMissileWeaponsAllowed(ShipHullCategory hullCategory) {
        return hullCategory.__MaxMissileWeapons();
    }

    private UnitCmdStat __MakeCmdStat() {
        float maxHitPts = 10F;
        int maxCmdEffect = 100;
        return new UnitCmdStat(UnitName, maxHitPts, maxCmdEffect, Formation.Circle);
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
            case ShipHullCategory.Dreadnaught:
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
        return new CameraFollowableStat(minViewDistance, optViewDistance, fov);
    }

    private EngineStat __MakeEngineStat(ShipHullCategory hullCategory) {
        float maxTurnRate = UnityEngine.Random.Range(90F, 270F);
        float engineMass = __GetEngineMass(hullCategory);

        float fullStlPower = __GetFullStlPower(hullCategory);  // FullStlSpeed ~ 1.5 - 3 units/hour
        float fullFtlPower = fullStlPower * TempGameValues.__FtlMultiplier;   // FullFtlSpeed ~ 15 - 30 units/hour
        return new EngineStat("EngineName", AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", fullStlPower, fullFtlPower,
            maxTurnRate, 0F, engineMass, 0F, 0F);
    }

    private float __GetIncome(ShipHullCategory category) {
        switch (category) {
            case ShipHullCategory.Support:
                return 3F;
            case ShipHullCategory.Carrier:
            case ShipHullCategory.Colonizer:
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Dreadnaught:
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
            case ShipHullCategory.Dreadnaught:
            case ShipHullCategory.Troop:
            case ShipHullCategory.Colonizer:
                return 5F;
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Support:  // TODO need Trader
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
                return 50F;
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Support:
                return 100F;
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Colonizer:
            case ShipHullCategory.Science:
                return 200F;
            case ShipHullCategory.Dreadnaught:
            case ShipHullCategory.Troop:
                return 400F;
            case ShipHullCategory.Carrier:
                return 500F;
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
            case ShipHullCategory.Dreadnaught:
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

    private float __GetFullStlPower(ShipHullCategory hull) { // generates StlSpeed ~ 1.5 - 3 units/hr;  planetoids ~ 0.1 units/hour, so Slow min = 0.15 units/hr
        switch (hull) {
            case ShipHullCategory.Frigate:
                return UnityEngine.Random.Range(5F, 15F);
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Support:
                return UnityEngine.Random.Range(10F, 30F);
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Colonizer:
            case ShipHullCategory.Science:
                return UnityEngine.Random.Range(20F, 60F);
            case ShipHullCategory.Dreadnaught:
            case ShipHullCategory.Troop:
                return UnityEngine.Random.Range(40F, 120F);
            case ShipHullCategory.Carrier:
                return UnityEngine.Random.Range(50F, 150F);
            case ShipHullCategory.Scout:
            case ShipHullCategory.Fighter:
            case ShipHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hull));
        }
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
            case ShipHullCategory.Dreadnaught:
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


}

