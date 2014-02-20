// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipMoveTask.cs
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
public class ShipMoveTask : ATask {

    private ShipModel _ship;

    public static ShipMoveTask CreateAndStartTask(ShipModel ship, Action<ATask> onCompletion = null) {
        var task = new ShipMoveTask(ship, onCompletion);
        TaskMgr.AddTask(task);
        return task;
    }

    public ShipMoveTask(ShipModel ship, Action<ATask> onCompletion = null) {
        _ship = ship;
        this.onCompletion += onCompletion;
    }

    public override void Setup() {
        base.Setup();
        _ship.CurrentState = ShipState.Moving;
    }

    public override void Tick() {
        // does nothing
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

