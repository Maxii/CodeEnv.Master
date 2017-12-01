// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AutoSettlementCreator.cs
// Unit Creator that builds and deploys an auto-configured Settlement in a randomly selected system in the scene. 
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
/// Unit Creator that builds and deploys an auto-configured Settlement in a randomly selected system in the scene. 
/// </summary>
public class AutoSettlementCreator : AAutoUnitCreator {

    private SettlementCmdItem _command;
    private IList<FacilityItem> _elements;

    protected override void InitializeRootUnitName() {
        RootUnitName = "AutoSettlement";
    }

    protected override void MakeElements() {
        _elements = new List<FacilityItem>();
        foreach (var designName in Configuration.ElementDesignNames) {
            FacilityDesign design = _gameMgr.PlayersDesigns.GetFacilityDesign(Owner, designName);
            string name = _factory.__GetUniqueFacilityName(design.DesignName);
            _elements.Add(_factory.MakeFacilityInstance(Owner, Topography.System, design, name, gameObject));
        }
    }

    protected override void MakeCommand() {
        _command = _factory.MakeSettlementCmdInstance(Owner, Configuration.CmdDesignName, gameObject, UnitName);
    }

    protected override void AddElementsToCommand() {
        LogEvent();
        _elements.ForAll(e => _command.AddElement(e));
        // command IS NOT assigned as a target of each element's CameraLOSChangedRelay as that would make the CommandIcon disappear when the elements disappear
    }

    /// <summary>
    /// Assigns the HQ element to the command. The assignment itself regenerates the formation,
    /// resulting in each element assuming the proper position.
    /// Note: This method must not be called before AddElementsToCommand().
    /// </summary>
    protected override void AssignHQElement() {
        LogEvent();
        _command.HQElement = _command.SelectHQElement();
    }

    protected override void PositionUnit() {
        base.PositionUnit();
        LogEvent(); // 10.6.16 Selection of system to deploy to moved to UniverseCreator
        var system = gameObject.GetSingleComponentInParents<SystemItem>();
        system.Settlement = _command;
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
        foreach (var element in _elements) {
            ////element.CommenceOperations(isInitialConstructionNeeded: false);
            element.CommenceOperations();
        }
    }

    protected override bool BeginCommandOperations() {
        LogEvent();
        _command.CommenceOperations();
        return true;
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

    //private SettlementCmdItem _command;
    //private IList<FacilityItem> _elements;

    //protected override void InitializeRootUnitName() {
    //    RootUnitName = "AutoSettlement";
    //}

    //protected override void MakeElements() {
    //    _elements = new List<FacilityItem>();
    //    foreach (var designName in Configuration.ElementDesignNames) {
    //        FacilityDesign design = _gameMgr.PlayersDesigns.GetFacilityDesign(Owner, designName);
    //        string name = _factory.__GetUniqueFacilityName(design.DesignName);
    //        _elements.Add(_factory.MakeFacilityInstance(Owner, Topography.System, design, name, gameObject));
    //    }
    //}

    //protected override void MakeCommand(Player owner) {
    //    CmdCameraStat cameraStat = MakeCmdCameraStat(TempGameValues.MaxFacilityRadius);
    //    Formation formation = Formation.Globe;
    //    _command = _factory.MakeSettlementCmdInstance(owner, cameraStat, Configuration.CmdDesignName, gameObject, UnitName, formation);
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
    //    LogEvent(); // 10.6.16 Selection of system to deploy to moved to UniverseCreator
    //    var system = gameObject.GetSingleComponentInParents<SystemItem>();
    //    system.Settlement = _command;
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

