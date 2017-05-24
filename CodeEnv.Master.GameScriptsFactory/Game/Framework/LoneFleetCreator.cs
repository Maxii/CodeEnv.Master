// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LoneFleetCreator.cs
// Unit Creator that builds and deploys a Unit configured using a basic Cmd and LoneElement at its current location in the scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;

/// <summary>
/// Unit Creator that builds and deploys a Unit configured using a basic Cmd and LoneElement at its current location in the scene.
/// </summary>
public class LoneFleetCreator : ALoneUnitCreator {

    protected override void MakeCommand(Player owner) {
        FleetCmdCameraStat cameraStat = MakeCmdCameraStat(TempGameValues.ShipMaxRadius);
        _command = _factory.MakeFleetCmdInstance(owner, cameraStat, Configuration.CmdDesignName, gameObject);
        _command.transform.rotation = LoneElement.transform.rotation;
        _command.IsLoneCmd = true;
        if (_command.Data.ParentName != UnitName) {  // avoids equals warning
            _command.Data.ParentName = UnitName;
        }
    }

    protected override void PositionUnit() {
        // Fleets don't need to be deployed. They are already on location.
    }

    private FleetCmdCameraStat MakeCmdCameraStat(float maxElementRadius) {
        float minViewDistance = maxElementRadius + 1F;
        float optViewDistanceAdder = 1F;    // the additional distance outside of the UnitRadius of the fleet
        // there is no optViewDistance value for a FleetCmd CameraStat
        return new FleetCmdCameraStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    }

}

