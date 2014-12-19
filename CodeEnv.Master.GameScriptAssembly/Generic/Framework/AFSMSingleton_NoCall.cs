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

    public event Action onCurrentStateChanged;
    public event Action<E> onCurrentStateChanging;

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

    public override E CurrentState {
        get { return state.currentState; }
        protected set {
            D.Assert(!state.Equals(value)); // a state object and a state's E CurrentState should never be equal
            if (state.currentState.Equals(value)) {
                D.Warn("{0} duplicate state {1} set attempt.", GetType().Name, value);
            }
            SetProperty<E>(ref state.currentState, value, "CurrentState", OnCurrentStateChanged, OnCurrentStateChanging);
        }
    }

    protected virtual void OnCurrentStateChanging(E incomingState) {
        __ValidateNoNewStateSetDuringEnterState(incomingState);
        ChangingState();
        if (onCurrentStateChanging != null) {
            onCurrentStateChanging(incomingState);
        }
    }

    protected virtual void OnCurrentStateChanged() {
        ConfigureCurrentState();
        if (onCurrentStateChanged != null) {
            onCurrentStateChanged();
        }
        __ResetStateChangeValidationTest();
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

    #region Debug

    private bool __isOnCurrentStateChangingProcessed;

    /// <summary>
    /// Validates that a void State_EnterState() method (called in ConfigureCurrentState()) does not attempt to set a new state.
    /// Note: State_EnterState() methods that return IEnumerator that set a new state value should not fail this test as 
    /// the coroutine is not immediately run, allowing the CurrentState's OnChanging and OnChanged notification events to complete
    /// processing before the state change is made.
    /// </summary>
    private void __ValidateNoNewStateSetDuringEnterState(E incomingState) {
        D.Assert(!__isOnCurrentStateChangingProcessed, "{0} should not change state to {1} while executing {2}_EnterState().".Inject(GetType().Name, incomingState, CurrentState));
        __isOnCurrentStateChangingProcessed = true;
    }

    private void __ResetStateChangeValidationTest() {
        __isOnCurrentStateChangingProcessed = false;
    }

    #endregion

}

