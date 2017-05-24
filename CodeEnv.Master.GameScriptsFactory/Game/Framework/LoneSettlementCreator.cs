// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LoneSettlementCreator.cs
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
public class LoneSettlementCreator : ALoneUnitCreator {

    protected override void MakeCommand(Player owner) {
        CmdCameraStat cameraStat = MakeCmdCameraStat(TempGameValues.FacilityMaxRadius);
        _command = _factory.MakeSettlementCmdInstance(owner, cameraStat, Configuration.CmdDesignName, gameObject);
        _command.IsLoneCmd = true;
        if (_command.Data.ParentName != UnitName) {  // avoids equals warning
            _command.Data.ParentName = UnitName;
        }
    }

    protected override void PositionUnit() {
        LogEvent();
    }

    private CmdCameraStat MakeCmdCameraStat(float maxElementRadius) {
        float minViewDistance = maxElementRadius + 1F; // close to the HQ Facility
        float optViewDistanceAdder = Constants.ZeroF;
        return new CmdCameraStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    }

}

