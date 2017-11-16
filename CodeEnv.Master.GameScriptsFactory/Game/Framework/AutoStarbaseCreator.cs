// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AutoStarbaseCreator.cs
// Unit Creator that builds and deploys an auto-configured starbase at its current location in the scene.
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
using UnityEngine;

/// <summary>
/// Unit Creator that builds and deploys an auto-configured starbase at its current location in the scene.
/// </summary>
public class AutoStarbaseCreator : AAutoUnitCreator {

    private StarbaseCmdItem _command;
    private IList<FacilityItem> _elements;

    protected override void InitializeRootUnitName() {
        RootUnitName = "AutoStarbase";
    }

    protected override void MakeElements() {
        _elements = new List<FacilityItem>();
        foreach (var designName in Configuration.ElementDesignNames) {
            FacilityDesign design = _gameMgr.PlayersDesigns.GetFacilityDesign(Owner, designName);
            string name = _factory.__GetUniqueFacilityName(design.DesignName);
            _elements.Add(_factory.MakeFacilityInstance(Owner, Topography.OpenSpace, design, name, gameObject));
        }
    }

    protected override void MakeCommand() {
        _command = _factory.MakeStarbaseCmdInstance(Owner, Configuration.CmdDesignName, gameObject, UnitName);
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

    protected override void PositionUnit() {
        base.PositionUnit();
        LogEvent();
        // Starbases don't need to be deployed. They are already on location
        PathfindingManager.Instance.Graph.AddToGraph(_command, SectorID);
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

    protected override void BeginElementsOperations() {
        LogEvent();
        _elements.ForAll(e => e.CommenceOperations(isInitialConstructionNeeded: false));
    }

    protected override bool BeginCommandOperations() {
        LogEvent();
        _command.CommenceOperations();
        return true;
    }

    private CmdCameraStat MakeCmdCameraStat(float maxElementRadius) {
        float minViewDistance = maxElementRadius + 1F; // close to the HQ Facility
        float optViewDistanceAdder = Constants.ZeroF;
        return new CmdCameraStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    }

    protected override void ClearElementReferences() {
        _elements.Clear();
    }

    #region Debug

    protected override void __ValidateNotDuplicateDeployment() {
        D.AssertNull(_command);
    }

    #endregion

    #region Archive

    //private StarbaseCmdItem _command;
    //private IList<FacilityItem> _elements;

    //protected override void InitializeRootUnitName() {
    //    RootUnitName = "AutoStarbase";
    //}

    //protected override void MakeElements() {
    //    _elements = new List<FacilityItem>();
    //    foreach (var designName in Configuration.ElementDesignNames) {
    //        FacilityDesign design = _gameMgr.PlayersDesigns.GetFacilityDesign(Owner, designName);
    //        string name = _factory.__GetUniqueFacilityName(design.DesignName);
    //        _elements.Add(_factory.MakeFacilityInstance(Owner, Topography.OpenSpace, design, name, gameObject));
    //    }
    //}

    //protected override void MakeCommand(Player owner) {
    //    CmdCameraStat cameraStat = MakeCmdCameraStat(TempGameValues.MaxFacilityRadius);
    //    Formation formation = Formation.Plane;
    //    _command = _factory.MakeStarbaseCmdInstance(owner, cameraStat, Configuration.CmdDesignName, gameObject, UnitName, formation);
    //}

    //protected override void AddElementsToCommand() {
    //    LogEvent();
    //    _elements.ForAll(e => _command.AddElement(e));
    //    // command IS NOT assigned as a target of each element's CameraLOSChangedRelay as that would make the CommandIcon disappear when the elements disappear
    //}

    //protected override void AssignHQElement() {
    //    LogEvent();
    //    _command.HQElement = _command.SelectHQElement();
    //}

    //protected override void PositionUnit() {
    //    LogEvent();
    //    // Starbases don't need to be deployed. They are already on location
    //    PathfindingManager.Instance.Graph.AddToGraph(_command, SectorID);
    //}

    //protected override void CompleteUnitInitialization() {
    //    LogEvent();
    //    _elements.ForAll(e => e.FinalInitialize());
    //    _command.FinalInitialize();
    //}

    //protected override void AddUnitToGameKnowledge() {
    //    LogEvent();
    //    //D.Log(ShowDebugLog, "{0} is adding Unit {1} to GameKnowledge.", DebugName, UnitName);
    //    _gameMgr.GameKnowledge.AddUnit(_command, _elements.Cast<IUnitElement>());
    //}

    //protected override void BeginElementsOperations() {
    //    LogEvent();
    //    _elements.ForAll(e => e.CommenceOperations(isInitialConstructionNeeded: false));
    //}

    //protected override bool BeginCommandOperations() {
    //    LogEvent();
    //    _command.CommenceOperations();
    //    return true;
    //}

    //private CmdCameraStat MakeCmdCameraStat(float maxElementRadius) {
    //    float minViewDistance = maxElementRadius + 1F; // close to the HQ Facility
    //    float optViewDistanceAdder = Constants.ZeroF;
    //    return new CmdCameraStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    //}

    //[Obsolete("Moved to UnitFactory")]
    //private FollowableItemCameraStat MakeElementCameraStat(FacilityHullStat hullStat) {
    //    FacilityHullCategory hullCat = hullStat.HullCategory;
    //    float fov;
    //    switch (hullCat) {
    //        case FacilityHullCategory.CentralHub:
    //        case FacilityHullCategory.Defense:
    //            fov = 70F;
    //            break;
    //        case FacilityHullCategory.Economic:
    //        case FacilityHullCategory.Factory:
    //        case FacilityHullCategory.Laboratory:
    //        case FacilityHullCategory.ColonyHab:
    //        case FacilityHullCategory.Barracks:
    //            fov = 60F;
    //            break;
    //        case FacilityHullCategory.None:
    //        default:
    //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
    //    }
    //    float radius = hullStat.HullDimensions.magnitude / 2F;
    //    //D.Log(ShowDebugLog, "Radius of {0} is {1:0.##}.", hullCat.GetValueName(), radius);
    //    float minViewDistance = radius * 2F;
    //    float optViewDistance = radius * 3F;
    //    return new FollowableItemCameraStat(minViewDistance, optViewDistance, fov);
    //}

    //protected override void ClearElementReferences() {
    //    _elements.Clear();
    //}

    //#region Debug

    //protected override void __ValidateNotDuplicateDeployment() {
    //    D.AssertNull(_command);
    //}

    //#endregion

    #endregion

}

