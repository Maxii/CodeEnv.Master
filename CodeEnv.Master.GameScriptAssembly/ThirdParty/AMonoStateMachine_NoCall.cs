// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMonoStateMachine_NoCall.cs
//  Abstract Base class for MonoBehaviour State Machines to inherit from. This version supports
//  subscription to State Changes, but does not support Call() or Return().
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
///  Abstract Base class for MonoBehaviour State Machines to inherit from. 
///  WARNING: This version supports subscription to State Changes, but does not 
///  support Call() or Return() as not all state changes will be notified if they are 
///  used as they make state changes without going through SetProperty.
/// </summary>
/// <typeparam name="E">Th State Type being used, typically an enum type.</typeparam>
public class AMonoStateMachine_NoCall<E> : AMonoStateMachine<E> where E : struct {

    /// <summary>
    /// Gets or sets the current State.
    /// 
    /// NOTE: The sequencing when a change of state is initiated by setting CurrentState = newState
    /// 1. the state we are changing from is recorded as lastState
    /// 2. the event OnCurrentStateChanging(newState) is sent to subscribers
    /// 3. the value of the CurrentState enum is changed to newState
    /// 4. the lastState_ExitState() method is called 
    ///          - while in this method, realize that the CurrentState enum has already changed to newState
    /// 5. the CurrentState's delegates are updated 
    ///          - meaning the EnterState delegate is changed from lastState_EnterState to newState_EnterState
    /// 6. the newState_EnterState() method is called
    ///          - as the event in 7 has not yet been called, you CANNOT set CurrentState = nextState within newState_EnterState()
    ///              - this would initiate the whole cycle above again, BEFORE the event in 7 is called
    ///              - you also can't just use a coroutine to wait then change it as the event is still held up
    ///          - instead, change it in newState_Update() which allows the event in 7 to complete before this change occurs again
    /// 7. the event OnCurrentStateChanged() is sent to subscribers
    ///          - when this event is received, a get_CurrentState property inquiry will properly return newState
    /// </summary>
    public override E CurrentState {
        get { return state.currentState; }
        protected set { SetProperty<E>(ref state.currentState, value, "CurrentState", OnCurrentStateChanged, OnCurrentStateChanging); }
    }

    protected virtual void OnCurrentStateChanging(E incomingState) {
        ChangingState();
    }

    protected virtual void OnCurrentStateChanged() {
        ConfigureCurrentState();
    }

    public override void Call(E stateToActivate) {
        throw new NotImplementedException("Call() is not implemented in this version of MonoStateMachine.");
    }

    public override void Return() {
        throw new NotImplementedException("Return() is not implemented in this version of MonoStateMachine.");
    }

    public override void Return(E baseState) {
        throw new NotImplementedException("Return(baseState) is not implemented in this version of MonoStateMachine.");
    }

}

