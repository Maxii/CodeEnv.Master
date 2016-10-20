// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFSMSingleton_NoCall.cs
//  Abstract Base class for MonoBehaviour State Machine Singletons to inherit from. 
//  WARNING: This version supports subscription to State Changes, but does not support Call() or Return().
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;

/// <summary>
/// Abstract Base class for MonoBehaviour State Machine Singletons to inherit from.
/// WARNING: This version supports subscription to State Changes, but does not
/// support Call() or Return() as not all state changes will be notified if they are
/// used as they make state changes without going through SetProperty.
/// </summary>
/// <typeparam name="T">The Type of the derived class.</typeparam>
/// <typeparam name="E">Th State Type being used, typically an enum type.</typeparam>
public abstract class AFSMSingleton_NoCall<T, E> : AFSMSingleton<T, E>
    where T : AFSMSingleton_NoCall<T, E>
    where E : struct {

    // ************************************************************************************************************
    // NOTE: The sequencing when a change of state is initiated by setting CurrentState = newState
    //
    // 1. the state we are changing from is recorded as lastState
    // 2. the 2 events indicating a state is about to change are sent to subscribers
    // 3. the value of the CurrentState enum is changed to newState
    // 4. the lastState_ExitState() method is called 
    //          - while in this method, realize that the CurrentState enum has already changed to newState
    // 5. the CurrentState's delegates are updated 
    //          - meaning the EnterState delegate is changed from lastState_EnterState to newState_EnterState
    // 6. the newState_EnterState() method is called
    //          - as the event in 7 has not yet been called, you CANNOT set CurrentState = nextState within newState_EnterState()
    //              - this would initiate the whole cycle above again, BEFORE the event in 7 is called
    //              - you also can't just use a coroutine to wait then change it as the event is still held up
    //          - instead, change it a frame later after the EnterState method has completed, and the events have been fired
    // 7. the 2 events indicating a state has just changed are sent to subscribers
    //          - when this event is received, a get_CurrentState property inquiry will properly return newState
    // *************************************************************************************************************

    // ***********************************************************************************************************
    // WARNING: IEnumerator State_EnterState methods are executed when the frame's Coroutine's are run, 
    // not when the state itself is changed. The order in which those state execution coroutines 
    // are run has nothing to do with the order in which the item's state is changed, aka if item1's state
    // is changed before item2's state, that DOES NOT mean item1's enterState will be called before item2's enterState.
    // ***********************************************************************************************************

    /// <summary>
    /// The current State of Type E of this FSM. 
    /// </summary>
    public override E CurrentState {
        get { return state.currentState; }
        protected set {
            D.Assert(!state.Equals(value)); // a state object and a state's E CurrentState should never be equal
            if (state.currentState.Equals(value)) {
                //D.Log("{0} duplicate state {1} set attempt.", GetType().Name, value);
            }
            SetProperty<E>(ref state.currentState, value, "CurrentState", CurrentStatePropChangedHandler, CurrentStatePropChangingHandler);
        }
    }

    #region Event and Property Change Handlers

    protected virtual void CurrentStatePropChangingHandler(E incomingState) {
        __ValidateNoNewStateSetDuringEnterState(incomingState);
        ChangingState();
    }

    protected virtual void CurrentStatePropChangedHandler() {
        ConfigureCurrentState();
        __ResetStateChangeValidationTest();
    }

    #endregion

    public override void Call(E stateToActivate) {
        throw new NotImplementedException("Call() is not implemented in this version of MonoStateMachine.");
    }

    public override void Return() {
        throw new NotImplementedException("Return() is not implemented in this version of MonoStateMachine.");
    }

    public override void Return(E baseState) {
        throw new NotImplementedException("Return(baseState) is not implemented in this version of MonoStateMachine.");
    }

    #region Debug

    // WARNING: Making a state change directly or indirectly (e.g. thru another method or issuing an order) from a void EnterState() method
    // will cause the state machine to lose its place. This even occurs if the state change is the last line of code in the EnterState().
    // What happens: when the void EnterState() executes, it executes in ConfigureCurrentState(). When that void EnterState() changes
    // the state during that execution, CurrentState_set is called. This occurs before EnterState() and ConfigureCurrentState() completes. 
    // The 'return to here to complete' code pointer remembers it has more code in EnterState() and then ConfigureCurrentState() to execute. 
    // As a result of CurrentState_set, ConfigureCurrentState() is called again but for the new state. This ConfigureCurrentState() call 
    // is executed all the way through, including executing ExitState() and executing (or scheduling for execution via coroutine) EnterState().
    // Once this ConfigureCurrentState() completes execution, the remainder of CurrentState_set is completed. It is at this stage, that
    // the code execution path returns to finishing the original EnterState(). Once this is finished, the next line of code to execute is 
    // back to the original ConfigureCurrentState() to finish up which includes calling Run(IEnumerator). Unfortunately, since the 
    // original EnterState() returns void, the IEnumerator is null and it overwrites the previously scheduled (but not yet executed) new
    // EnterState() which then never executes. The state machine is still operational, but it has missed executing a method it was supposed
    // to execute. It will however respond to a new state change.

    // A similar problem occurs if the state machine supports state change notification events. Changing state while executing 
    // CurrentState_set means the first state change notification that occurs is the new state, not the state that caused CurrentState_set
    // to be called again. In other words, the state change notifications get out of sync. This problem is not present in 
    // AMortalItemStateMachine as it doesn't support state change notifications.

    private bool __hasCurrentState_setFinishedWithoutInterveningSet = true;

    /// <summary>
    /// Validates that a void State_EnterState() method (called in ConfigureCurrentState()) does not attempt to set a new state.
    /// Note: State_EnterState() methods that return IEnumerator that set a new state value should not fail this test as 
    /// the coroutine that executes the EnterState() is only run after CurrentState_set completes.
    /// </summary>
    private void __ValidateNoNewStateSetDuringEnterState(E incomingState) {
        //D.Log("{0}.__ValidateNoNewStateSetDuringEnterState() called. CurrentState = {1}, IncomingState = {2}.", GetType().Name, CurrentState, incomingState);
        if (!__hasCurrentState_setFinishedWithoutInterveningSet) {
            D.Error("{0} cannot change state to {1} while executing {2}_EnterState().", GetType().Name, incomingState, CurrentState);
            return;
        }
        __hasCurrentState_setFinishedWithoutInterveningSet = false;
    }

    private void __ResetStateChangeValidationTest() {
        //D.Log("{0}.__ResetStateChangeValidationTest() called. CurrentState = {1}.", GetType().Name, CurrentState);
        __hasCurrentState_setFinishedWithoutInterveningSet = true;
    }

    #endregion

}

