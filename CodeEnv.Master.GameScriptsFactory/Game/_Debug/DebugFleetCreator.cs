// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugFleetCreator.cs
// Unit Creator that builds and deploys an editor-configured fleet at its current location in the scene.
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
/// Unit Creator that builds and deploys an editor-configured fleet at its current location in the scene.
/// </summary>
public class DebugFleetCreator : ADebugUnitCreator, IDebugFleetCreator {

    #region Serialized Editor fields

    [Range(1, TempGameValues.MaxShipsPerFleet)]
    [SerializeField]
    private int _elementQty = 8;

    [SerializeField]
    private DebugFleetFormation _formation = DebugFleetFormation.Random;

    /// <summary>
    /// Indicates whether this Fleet should move to a destination.
    /// </summary>
    [SerializeField]
    private bool _move = false;

    /// <summary>
    /// If fleet is to move to a destination, should it pick the farthest or the closest?
    /// </summary>
    [SerializeField]
    private bool _findFarthest = false;

    /// <summary>
    /// The fleet is to move to a destination, should it attack it?
    /// </summary>
    [SerializeField]
    private bool _attack = false;

    /// <summary>
    /// Indicates whether the FTL drive of all the ships in the fleet should start damaged, aka not operational.
    /// They can still repair themselves.
    /// </summary>
    [SerializeField]
    private bool _ftlStartsDamaged = false;

    /// <summary>
    /// The exclusions when randomly picking ShipCombatStances.
    /// </summary>
    [SerializeField]
    private DebugShipCombatStanceExclusions _stanceExclusions = default(DebugShipCombatStanceExclusions);

    #endregion

    private FleetCreatorEditorSettings _editorSettings;
    public override AUnitCreatorEditorSettings EditorSettings {
        get {
            if (_editorSettings == null) {
                if (IsCompositionPreset) {
                    var presetHullCats = gameObject.GetSafeComponentsInChildren<ShipHull>().Select(hull => hull.HullCategory).ToList();
                    _editorSettings = new FleetCreatorEditorSettings(UnitName, _isOwnerUser, _ownerRelationshipWithUser, _countermeasuresPerCmd, _activeCMsPerElement, DateToDeploy,
                        _losWeaponsPerElement, _missileWeaponsPerElement, _passiveCMsPerElement, _shieldGeneratorsPerElement, _sensorsPerElement,
                        _formation, _move, _findFarthest, _attack, _stanceExclusions, presetHullCats);
                }
                else {
                    _editorSettings = new FleetCreatorEditorSettings(UnitName, _isOwnerUser, _elementQty, _ownerRelationshipWithUser, _countermeasuresPerCmd, _activeCMsPerElement,
                        DateToDeploy, _losWeaponsPerElement, _missileWeaponsPerElement, _passiveCMsPerElement, _shieldGeneratorsPerElement, _sensorsPerElement,
                        _formation, _move, _findFarthest, _attack, _stanceExclusions);
                }
            }
            return _editorSettings;
        }
    }

    private FleetCmdItem _command;
    private IList<ShipItem> _elements;

    protected override void ValidateStaticSetting() {
        if (gameObject.isStatic) {
            D.Warn("{0} should not start as static. Correcting.", Name);
            gameObject.isStatic = false;
        }
    }

    protected override void MakeElements() {
        _elements = new List<ShipItem>();

        if (IsCompositionPreset) {
            IList<ShipDesign> designs = new List<ShipDesign>();
            foreach (var designName in Configuration.ElementDesignNames) {
                ShipDesign design = _gameMgr.PlayersDesigns.GetShipDesign(Owner, designName);
                designs.Add(design);
            }

            IList<ShipHullCategory> designHullCategories = designs.Select(d => d.HullCategory).ToList();
            IList<ShipItem> existingElements = gameObject.GetSafeComponentsInChildren<ShipItem>();
            foreach (var hullCat in designHullCategories) {
                var categoryElements = existingElements.Where(e => e.gameObject.GetSingleComponentInChildren<ShipHull>().HullCategory == hullCat);
                var categoryElementsStillAvailable = categoryElements.Except(_elements);
                ShipItem element = categoryElementsStillAvailable.First();

                var design = designs.First(d => d.HullCategory == hullCat);
                designs.Remove(design);

                var cameraStat = MakeElementCameraStat(design.HullStat);
                _factory.PopulateInstance(Owner, cameraStat, design, ref element);

                // Note: Need to tell each element where this creator is located. This assures that whichever element is picked as the HQElement
                // will start with this position. However, the elements here are all placed on top of each other. When the physics engine starts
                // rigidbodies that are not kinematic are imparted with both linear and angular velocity from this intentional collision. 
                // This occurs before the elements are moved away from each other by being formed into a formation. 
                // Accordingly, all element rigidbodies start as kinematic, then I change ships to non-kinematic during CommenceOperations.
                element.transform.position = transform.position;
                _elements.Add(element);
            }
            D.AssertEqual(Constants.Zero, designs.Count);
        }
        else {
            foreach (var designName in Configuration.ElementDesignNames) {
                ShipDesign design = _gameMgr.PlayersDesigns.GetShipDesign(Owner, designName);
                FollowableItemCameraStat cameraStat = MakeElementCameraStat(design.HullStat);
                _elements.Add(_factory.MakeShipInstance(Owner, cameraStat, design, gameObject));
            }
        }
    }

    protected override void MakeCommand(Player owner) {
        FleetCmdCameraStat cameraStat = MakeCmdCameraStat(TempGameValues.ShipMaxRadius);
        if (IsCompositionPreset) {
            _command = gameObject.GetSingleComponentInChildren<FleetCmdItem>();
            _factory.PopulateInstance(owner, cameraStat, Configuration.CmdDesignName, ref _command);
        }
        else {
            _command = _factory.MakeFleetCmdInstance(owner, cameraStat, Configuration.CmdDesignName, gameObject);
        }
    }

    protected override void AddElementsToCommand() {
        LogEvent();
        _elements.ForAll(e => _command.AddElement(e));
        // command IS NOT assigned as a target of each element's CameraLOSChangedRelay as that would make the CommandIcon disappear when the elements disappear
    }

    protected override void AssignHQElement() {
        LogEvent();
        _command.HQElement = _command.SelectHQElement();
    }

    protected override bool DeployUnit() {
        LogEvent();
        // Fleets don't need to be deployed. They are already on location.
        return true;
    }

    protected override void CompleteUnitInitialization() {
        LogEvent();
        _elements.ForAll(e => {
            e.FinalInitialize();
            __SetFtlDamagedState(e);
        });
        _command.FinalInitialize();
    }

    protected override void AddUnitToGameKnowledge() {
        LogEvent();
        //D.Log("{0} is adding Unit {1} to GameKnowledge.", Name, UnitName);
        _gameMgr.GameKnowledge.AddUnit(_command, _elements.Cast<IUnitElement>());
    }

    protected override void AddUnitToOwnerAndAllysKnowledge() {
        LogEvent();
        //D.Log("{0} is adding Unit {1} to {2}'s Knowledge.", Name, UnitName, Owner);
        var ownerAIMgr = _gameMgr.GetAIManagerFor(Owner);
        _elements.ForAll(e => ownerAIMgr.HandleGainedItemOwnership(e));
        ownerAIMgr.HandleGainedItemOwnership(_command);    // OPTIMIZE not really needed as this happens automatically when elements handled?

        var alliedPlayers = Owner.GetOtherPlayersWithRelationship(DiplomaticRelationship.Alliance);
        if (alliedPlayers.Any()) {
            alliedPlayers.ForAll(ally => {
                //D.Log("{0} is adding Unit {1} to {2}'s Knowledge as Ally.", Name, UnitName, ally);
                var allyAIMgr = _gameMgr.GetAIManagerFor(ally);
                _elements.ForAll(e => allyAIMgr.HandleChgdItemOwnerIsAlly(e));
                allyAIMgr.HandleChgdItemOwnerIsAlly(_command);  // OPTIMIZE not really needed as this happens automatically when elements handled?
            });
        }
    }

    protected override void RegisterCommandForOrders() {
        var ownerAIMgr = _gameMgr.GetAIManagerFor(Owner);
        ownerAIMgr.RegisterForOrders(_command);
    }

    protected override void BeginElementsOperations() {
        LogEvent();
        _elements.ForAll(e => e.CommenceOperations());
    }

    protected override void BeginCommandOperations() {
        LogEvent();
        _command.CommenceOperations();
    }

    private void __SetFtlDamagedState(ShipItem element) {
        element.Data.IsFtlDamaged = _ftlStartsDamaged;
    }

    [Obsolete]
    protected override void __IssueFirstUnitOrder(Action onCompleted) {
        LogEvent();
        //D.Log("{0} launching 1 hour wait on {1}. Frame {2}, UnityTime {3:0.0}, SystemTimeStamp {4}.", Name, GameTime.Instance.CurrentDate, Time.frameCount, Time.time, Utility.TimeStamp);

        // The following delay avoids script execution order issue when this creator receives IsRunning before other creators
        string jobName = "{0}.WaitToIssueFirstOrderJob".Inject(Name);
        _jobMgr.WaitForHours(1F, jobName, waitFinished: delegate {    // makes sure Owner's knowledge of universe has been constructed before selecting its target
            //D.Log("{0} finished 1 hour wait on {1}. Frame {2}, UnityTime {3:0.0}, SystemTimeStamp {4}.", Name, GameTime.Instance.CurrentDate, Time.frameCount, Time.time, Utility.TimeStamp);
            if (_move) {
                if (_attack) {
                    __GetFleetAttackUnderway();
                }
                else {
                    __GetFleetUnderway();
                }
            }
            onCompleted();
        });
    }

    [Obsolete]
    private void __GetFleetUnderway() { // 7.12.16 Removed 'not enemy' criteria for move
        LogEvent();
        Player fleetOwner = Owner;
        var fleetOwnerKnowledge = GameManager.Instance.GetAIManagerFor(fleetOwner).Knowledge;
        List<IFleetNavigable> moveTgts = fleetOwnerKnowledge.Starbases.Cast<IFleetNavigable>().ToList();
        moveTgts.AddRange(fleetOwnerKnowledge.Settlements.Cast<IFleetNavigable>());
        moveTgts.AddRange(fleetOwnerKnowledge.Planets.Cast<IFleetNavigable>());
        //moveTgts.AddRange(fleetOwnerKnowledge.Systems.Cast<IFleetNavigable>());   // UNCLEAR or Stars?
        moveTgts.AddRange(fleetOwnerKnowledge.Stars.Cast<IFleetNavigable>());
        if (fleetOwnerKnowledge.UniverseCenter != null) {
            moveTgts.Add(fleetOwnerKnowledge.UniverseCenter as IFleetNavigable);
        }

        if (!moveTgts.Any()) {
            D.Log("{0} can find no MoveTargets that meet the selection criteria. Picking an unowned Sector.", Name);
            moveTgts.AddRange(SectorGrid.Instance.Sectors.Where(s => s.Owner == TempGameValues.NoPlayer).Cast<IFleetNavigable>());
        }
        IFleetNavigable destination;
        if (_findFarthest) {
            destination = moveTgts.MaxBy(mt => Vector3.SqrMagnitude(mt.Position - transform.position));
        }
        else {
            destination = moveTgts.MinBy(mt => Vector3.SqrMagnitude(mt.Position - transform.position));
        }
        //D.Log("{0} destination is {1}.", UnitName, destination.FullName);
        _command.CurrentOrder = new FleetOrder(FleetDirective.Move, OrderSource.CmdStaff, destination);
    }

    [Obsolete]
    private void __GetFleetAttackUnderway() {
        LogEvent();
        Player fleetOwner = Owner;
        var fleetOwnerKnowledge = _gameMgr.GetAIManagerFor(fleetOwner).Knowledge;
        List<IUnitAttackable> attackTgts = fleetOwnerKnowledge.Fleets.Cast<IUnitAttackable>().Where(f => f.IsAttackByAllowed(fleetOwner)).ToList();
        attackTgts.AddRange(fleetOwnerKnowledge.Starbases.Cast<IUnitAttackable>().Where(sb => sb.IsAttackByAllowed(fleetOwner)));
        attackTgts.AddRange(fleetOwnerKnowledge.Settlements.Cast<IUnitAttackable>().Where(s => s.IsAttackByAllowed(fleetOwner)));
        attackTgts.AddRange(fleetOwnerKnowledge.Planets.Cast<IUnitAttackable>().Where(p => p.IsAttackByAllowed(fleetOwner)));
        if (attackTgts.IsNullOrEmpty()) {
            D.Log("{0} can find no AttackTargets of any sort. Defaulting to __GetFleetUnderway().", Name);
            __GetFleetUnderway();
            return;
        }
        IUnitAttackable attackTgt;
        if (_findFarthest) {
            attackTgt = attackTgts.MaxBy(t => Vector3.SqrMagnitude(t.Position - transform.position));
        }
        else {
            attackTgt = attackTgts.MinBy(t => Vector3.SqrMagnitude(t.Position - transform.position));
        }
        //D.Log("{0} attack target is {1}.", UnitName, attackTgt.FullName);
        _command.CurrentOrder = new FleetOrder(FleetDirective.Attack, OrderSource.CmdStaff, attackTgt);
    }

    private FleetCmdCameraStat MakeCmdCameraStat(float maxElementRadius) {
        float minViewDistance = maxElementRadius + 1F;
        float optViewDistanceAdder = 1F;    // the additional distance outside of the UnitRadius of the fleet
        // there is no optViewDistance value for a FleetCmd CameraStat
        return new FleetCmdCameraStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    }

    private FollowableItemCameraStat MakeElementCameraStat(ShipHullStat hullStat) {
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
            case ShipHullCategory.Investigator:
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
        return new FollowableItemCameraStat(minViewDistance, optViewDistance, fov, distanceDampener, rotationDampener);
    }

    protected override void __AdjustElementQtyFieldTo(int qty) {
        _elementQty = qty;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region GetFleetUnderway Archive

    //private void __GetFleetUnderway() { // 7.12.16 Removed 'not enemy' criteria for move
    //    LogEvent();
    //    Player fleetOwner = _owner;
    //    var fleetOwnerKnowledge = GameManager.Instance.GetAIManagerFor(fleetOwner).Knowledge;
    //    IEnumerable<IFleetNavigable> moveTgts = fleetOwnerKnowledge.Starbases.Cast<IFleetNavigable>();
    //    if (!moveTgts.Any()) {
    //        // in case no starbases qualify
    //        moveTgts = fleetOwnerKnowledge.Settlements.Cast<IFleetNavigable>();
    //        if (!moveTgts.Any()) {
    //            // in case no Settlements qualify
    //            moveTgts = fleetOwnerKnowledge.Planets.Cast<IFleetNavigable>();
    //            if (!moveTgts.Any()) {
    //                // in case no Planets qualify
    //                moveTgts = fleetOwnerKnowledge.Systems.Cast<IFleetNavigable>();
    //                if (!moveTgts.Any()) {
    //                    // in case no Systems qualify
    //                    moveTgts = fleetOwnerKnowledge.Stars.Cast<IFleetNavigable>();
    //                    if (!moveTgts.Any()) {
    //                        // in case no Stars qualify
    //                        moveTgts = SectorGrid.Instance.AllSectors.Where(s => s.Owner == TempGameValues.NoPlayer).Cast<IFleetNavigable>();
    //                        if (!moveTgts.Any()) {
    //                            D.Error("{0} can find no MoveTargets of any sort. MoveOrder has been canceled.", UnitName);
    //                            return;
    //                        }
    //                        D.Log("{0} can find no MoveTargets that meet the selection criteria. Picking an unowned Sector.", UnitName);
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    IFleetNavigable destination;
    //    if (findFarthest) {
    //        destination = moveTgts.MaxBy(mt => Vector3.SqrMagnitude(mt.Position - transform.position));
    //    }
    //    else {
    //        destination = moveTgts.MinBy(mt => Vector3.SqrMagnitude(mt.Position - transform.position));
    //    }
    //    //D.Log("{0} destination is {1}.", UnitName, destination.FullName);
    //    _command.CurrentOrder = new FleetOrder(FleetDirective.Move, OrderSource.CmdStaff, destination);
    //}

    //private void __GetFleetAttackUnderway() {
    //    LogEvent();
    //    Player fleetOwner = _owner;
    //    var fleetOwnerKnowledge = GameManager.Instance.GetAIManagerFor(fleetOwner).Knowledge;
    //    IEnumerable<IUnitAttackable> attackTgts = fleetOwnerKnowledge.Fleets.Cast<IUnitAttackable>().Where(f => f.IsAttackingAllowedBy(fleetOwner));
    //    if (attackTgts.IsNullOrEmpty()) {
    //        // in case no Fleets qualify
    //        attackTgts = fleetOwnerKnowledge.Starbases.Cast<IUnitAttackable>().Where(s => s.IsAttackingAllowedBy(fleetOwner));
    //        if (attackTgts.IsNullOrEmpty()) {
    //            // in case no Starbases qualify
    //            attackTgts = fleetOwnerKnowledge.Settlements.Cast<IUnitAttackable>().Where(s => s.IsAttackingAllowedBy(fleetOwner));
    //            if (attackTgts.IsNullOrEmpty()) {
    //                // in case no Settlements qualify
    //                attackTgts = fleetOwnerKnowledge.Planets.Cast<IUnitAttackable>().Where(p => p.IsAttackingAllowedBy(fleetOwner));
    //                if (attackTgts.IsNullOrEmpty()) {
    //                    D.Log("{0} can find no AttackTargets of any sort. Defaulting to __GetFleetUnderway().", UnitName);
    //                    __GetFleetUnderway();
    //                    return;
    //                }
    //            }
    //        }
    //    }
    //    IUnitAttackable attackTgt;
    //    if (findFarthest) {
    //        attackTgt = attackTgts.MaxBy(t => Vector3.SqrMagnitude(t.Position - transform.position));
    //    }
    //    else {
    //        attackTgt = attackTgts.MinBy(t => Vector3.SqrMagnitude(t.Position - transform.position));
    //    }
    //    //D.Log("{0} attack target is {1}.", UnitName, attackTgt.FullName);
    //    _command.CurrentOrder = new FleetOrder(FleetDirective.Attack, OrderSource.CmdStaff, attackTgt);
    //}

    #endregion


}

