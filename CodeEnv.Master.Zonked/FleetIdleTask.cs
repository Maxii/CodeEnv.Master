// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetIdleTask.cs
// COMMENT - one line to give a brief idea of what the file does.
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

/// <summary>
/// 
/// </summary>
public class FleetIdleTask : ATask {

    private FleetCmdModel _fleetCmd;

    public FleetIdleTask(FleetCmdModel fleetCmd, Action<ATask> onCompletion = null) {
        _fleetCmd = fleetCmd;
        this.onCompletion += onCompletion;
    }

    public override void Setup() {
        base.Setup();
        _fleetCmd.CurrentState = FleetState.Idling;
    }

    public override void Tick() {
        Finish();   // when using a task to change to Idle there is never a nextTask
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


