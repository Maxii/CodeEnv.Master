// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AutoFleetCreator.cs
// Unit Creator that builds and deploys an auto-configured fleet at its current location in the scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Unit Creator that builds and deploys an auto-configured fleet at its current location in the scene.
/// </summary>
public class AutoFleetCreator : AAutoUnitCreator {

    private FleetCmdItem _command;
    private IList<ShipItem> _elements;

    protected override void InitializeRootUnitName() {
        RootUnitName = "Fleet_AutoConfig";
    }

    protected override void MakeElements() {
        _elements = new List<ShipItem>();
        foreach (var designName in Configuration.ElementDesignNames) {
            ShipDesign design = _ownerDesigns.__GetShipDesign(designName);
            string name = _factory.__GetUniqueShipName(design.DesignName);
            var ship = _factory.MakeShipInstance(Owner, design, name, gameObject);
            _elements.Add(ship);
        }
    }

    protected override void MakeCommand() {
        _command = _factory.MakeFleetCmdInstance(Owner, Configuration.CmdModDesignName, gameObject, UnitName);
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

    //private FleetCmdItem _command;
    //private IList<ShipItem> _elements;

    //protected override void InitializeRootUnitName() {
    //    RootUnitName = "AutoFleet";
    //}

    //protected override void MakeElements() {
    //    _elements = new List<ShipItem>();
    //    foreach (var designName in Configuration.ElementDesignNames) {
    //        ShipDesign design = _gameMgr.PlayersDesigns.GetShipDesign(Owner, designName);
    //        string name = _factory.__GetUniqueShipName(design.DesignName);
    //        var ship = _factory.MakeShipInstance(Owner, design, name, gameObject);
    //        _elements.Add(ship);
    //    }
    //}

    //protected override void MakeCommand(Player owner) {
    //    FleetCmdCameraStat cameraStat = MakeCmdCameraStat(TempGameValues.MaxShipRadius);
    //    Formation formation = Formation.Globe;
    //    _command = _factory.MakeFleetCmdInstance(owner, cameraStat, Configuration.CmdDesignName, gameObject, UnitName, formation);
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
    //    // Fleets don't need to be deployed. They are already on location.
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
    //    foreach (var element in _elements) {
    //        element.CommenceOperations();
    //    }
    //}

    //protected override bool BeginCommandOperations() {
    //    LogEvent();
    //    _command.CommenceOperations();
    //    return true;
    //}

    //private FleetCmdCameraStat MakeCmdCameraStat(float maxElementRadius) {
    //    float minViewDistance = maxElementRadius + 1F;
    //    float optViewDistanceAdder = 1F;    // the additional distance outside of the UnitRadius of the fleet
    //    // there is no optViewDistance value for a FleetCmd CameraStat
    //    return new FleetCmdCameraStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    //}

    //protected override void ClearElementReferences() {
    //    _elements.Clear();
    //}

    #endregion

}

