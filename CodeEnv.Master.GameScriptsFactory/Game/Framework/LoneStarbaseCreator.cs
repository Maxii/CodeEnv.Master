// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LoneStarbaseCreator.cs
// Unit Creator that builds and deploys a Unit configured using a basic Cmd and LoneElement at its current location in the scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Unit Creator that builds and deploys a Unit configured using a basic Cmd and LoneElement at its current location in the scene.
/// </summary>
[System.Obsolete]
public class LoneStarbaseCreator : ALoneUnitCreator {

    protected override void MakeCommand(Player owner) {
        CmdCameraStat cameraStat = MakeCmdCameraStat(TempGameValues.FacilityMaxRadius);
        _command = _factory.MakeStarbaseCmdInstance(owner, cameraStat, Configuration.CmdDesignName, gameObject);
        _command.IsLoneCmd = true;
        if (_command.Data.ParentName != UnitName) {  // avoids equals warning
            _command.Data.ParentName = UnitName;
        }
    }

    protected override void PositionUnit() {
        // Starbases don't need to be deployed. They are already at the location of LoneElement
        // 4.25.17 PathfindingMgr expects to find the Starbase when it dies and calls RemoveFromGraph so be consistent and add it.
        // I think all this will do is make some of the approach waypoints for the base that is already there unwalkable
        // and add this base's approach waypoints.
        PathfindingManager.Instance.Graph.AddToGraph(_command as StarbaseCmdItem, SectorID);
    }

    private CmdCameraStat MakeCmdCameraStat(float maxElementRadius) {
        float minViewDistance = maxElementRadius + 1F; // close to the HQ Facility
        float optViewDistanceAdder = Constants.ZeroF;
        return new CmdCameraStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    }


}

