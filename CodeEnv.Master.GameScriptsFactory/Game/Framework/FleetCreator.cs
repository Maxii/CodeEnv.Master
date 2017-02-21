// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCreator.cs
// Unit Creator that builds and deploys an auto-configured fleet at its current location in the scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using MoreLinq;
using UnityEngine;

/// <summary>
/// Unit Creator that builds and deploys an auto-configured fleet at its current location in the scene.
/// </summary>
public class FleetCreator : AAutoUnitCreator {

    private FleetCmdItem _command;
    private IList<ShipItem> _elements;

    protected override void MakeElements() {
        _elements = new List<ShipItem>();
        foreach (var designName in Configuration.ElementDesignNames) {
            ShipDesign design = _gameMgr.PlayersDesigns.GetShipDesign(Owner, designName);
            FollowableItemCameraStat cameraStat = MakeElementCameraStat(design.HullStat);
            var ship = _factory.MakeShipInstance(Owner, cameraStat, design, gameObject);
            _elements.Add(ship);
        }
    }

    protected override void MakeCommand(Player owner) {
        FleetCmdCameraStat cameraStat = MakeCmdCameraStat(TempGameValues.ShipMaxRadius);
        _command = _factory.MakeFleetCmdInstance(owner, cameraStat, Configuration.CmdDesignName, gameObject);
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
        _elements.ForAll(e => e.FinalInitialize());
        _command.FinalInitialize();
    }

    protected override void AddUnitToGameKnowledge() {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} is adding Unit {1} to GameKnowledge.", DebugName, UnitName);
        _gameMgr.GameKnowledge.AddUnit(_command, _elements.Cast<IUnitElement>());
    }

    [Obsolete]
    protected override void AddUnitToOwnerAndAllysKnowledge() {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} is adding Unit {1} to {2}'s Knowledge.", DebugName, UnitName, Owner);
        var ownerAIMgr = _gameMgr.GetAIManagerFor(Owner);
        _elements.ForAll(e => ownerAIMgr.HandleGainedItemOwnership(e));
        ownerAIMgr.HandleGainedItemOwnership(_command);    // OPTIMIZE not really needed as this happens automatically when elements handled

        var alliedPlayers = Owner.GetOtherPlayersWithRelationship(DiplomaticRelationship.Alliance);
        if (alliedPlayers.Any()) {
            alliedPlayers.ForAll(ally => {
                //D.Log(ShowDebugLog, "{0} is adding Unit {1} to {2}'s Knowledge as Ally.", DebugName, UnitName, ally);
                var allyAIMgr = _gameMgr.GetAIManagerFor(ally);
                _elements.ForAll(e => allyAIMgr.HandleChgdItemOwnerIsAlly(e));
                allyAIMgr.HandleChgdItemOwnerIsAlly(_command);  // OPTIMIZE not really needed as this happens automatically when elements handled
            });
        }
    }

    [Obsolete]
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

    [Obsolete]
    protected override void __IssueFirstUnitOrder(Action onCompleted) {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} launching 1 hour wait on {1}. Frame {2}, UnityTime {3:0.0}, SystemTimeStamp {4}.", DebugName, GameTime.Instance.CurrentDate, Time.frameCount, Time.time, Utility.TimeStamp);

        // The following delay avoids script execution order issue when this creator receives IsRunning before other creators
        string jobName = "{0}.WaitToIssueFirstOrderJob".Inject(DebugName);
        _jobMgr.WaitForHours(1F, jobName, waitFinished: delegate {    // makes sure Owner's knowledge of universe has been constructed before selecting its target
            __GetFleetUnderway();
            onCompleted();
        });
    }

    [Obsolete]
    private void __GetFleetUnderway() { // 7.12.16 Removed 'not enemy' criteria for move
        LogEvent();
        var fleetOwnerKnowledge = _gameMgr.GetAIManagerFor(Owner).Knowledge;
        List<IFleetNavigable> moveTgts = fleetOwnerKnowledge.Starbases.Cast<IFleetNavigable>().ToList();
        moveTgts.AddRange(fleetOwnerKnowledge.Settlements.Cast<IFleetNavigable>());
        moveTgts.AddRange(fleetOwnerKnowledge.Planets.Cast<IFleetNavigable>());
        //moveTgts.AddRange(fleetOwnerKnowledge.Systems.Cast<IFleetNavigable>());   // UNCLEAR or Stars?
        moveTgts.AddRange(fleetOwnerKnowledge.Stars.Cast<IFleetNavigable>());
        if (fleetOwnerKnowledge.UniverseCenter != null) {
            moveTgts.Add(fleetOwnerKnowledge.UniverseCenter as IFleetNavigable);
        }

        if (!moveTgts.Any()) {
            D.Log("{0} can find no MoveTargets that meet the selection criteria. Picking an unowned Sector.", DebugName);
            moveTgts.AddRange(SectorGrid.Instance.Sectors.Where(s => s.Owner == TempGameValues.NoPlayer).Cast<IFleetNavigable>());
        }
        IFleetNavigable destination;
        destination = moveTgts.MaxBy(mt => Vector3.SqrMagnitude(mt.Position - transform.position));
        //D.Log(ShowDebugLog, "{0} destination is {1}.", UnitName, destination.DebugName);
        _command.CurrentOrder = new FleetOrder(FleetDirective.Move, OrderSource.CmdStaff, destination);
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
        //D.Log(ShowDebugLog, "Radius of {0} is {1:0.##}.", hullCat.GetValueName(), radius);
        float minViewDistance = radius * 2F;
        float optViewDistance = radius * 3F;
        float distanceDampener = 3F;    // default
        float rotationDampener = 10F;   // ships can change direction pretty fast
        return new FollowableItemCameraStat(minViewDistance, optViewDistance, fov, distanceDampener, rotationDampener);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

