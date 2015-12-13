// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipAttackTask.cs
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
public class ShipAttackTask : ATask {

    private ShipModel _ship;

    public static ShipAttackTask CreateAndStartTask(ShipModel ship, Action<ATask> onCompletion = null) {
        var task = new ShipAttackTask(ship, onCompletion);
        TaskMgr.AddTask(task);
        return task;
    }

    public ShipAttackTask(ShipModel ship, Action<ATask> onCompletion = null) {
        _ship = ship;
        this.onCompletion += onCompletion;
    }

    public override void Setup() {
        base.Setup();
        _ship.CurrentState = ShipState.ExecuteAttackOrder;
    }

    public override void Tick() {
        // does nothing
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

