// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FramesLastUpdate.cs
// Simple management class that will always be the last to update.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Simple management class that will always be the last to update.
/// Updates the CoroutineScheduler so coroutines run after update just like Unity
/// and then raises a UnityUpdateEvent to tell non-MonoBehaviours that the last update
/// this frame has just occured. This allows them to do something on the same update
/// schedule as Unity without being a MonoBehaviour.
/// </summary>
[Obsolete]
public class FramesLastUpdate : AMonoBehaviourBase {

    private GameEventManager _eventMgr;

    protected override void Awake() {
        base.Awake();
        _eventMgr = GameEventManager.Instance;
    }

    void Update() {
        //CoroutineScheduler.UpdateAllCoroutines(Time.frameCount, Time.time);
        _eventMgr.Raise<UnityUpdateEvent>(new UnityUpdateEvent(this));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

