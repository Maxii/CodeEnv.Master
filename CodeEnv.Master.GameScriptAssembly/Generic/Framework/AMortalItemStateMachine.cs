// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemStateMachine.cs
// Abstract Base class for MortalItem State Machines to inherit from.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeEnv.Master.Common;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
///  Abstract Base class for MortalItem State Machines to inherit from.
///  WARNING: This version does not support subscribing to State Changes 
///  as Call() and Return() make changes without going through CurrentState.set.
/// </summary>
public abstract class AMortalItemStateMachine : AMortalItem {

    private const string MethodNameFormat = "{0}_{1}";
    private const string ExitStateText = "ExitState";
    private const string EnterStateText = "EnterState";

    #region RelayToCurrentState

    /// <summary>
    /// Optimized messaging replacement for SendMessage() that binds the
    /// message to the current state. Essentially calls the method on this MonoBehaviour
    /// instance that has the signature "CurrentState_CallingMethodName(param).
    /// Returns <c>true</c> if a method in the current state was invoked, false if no method is present.
    /// Usage:
    /// void CallingMethodName(param)  {
    /// RelayToCurrentState(param);
    /// }
    /// IMPROVE // add Action&lt;float&gt; delegate
    /// </summary>
    /// <param name="param">Any parameter passed to the current handler that should be passed on.</param>
    /// <returns>true if a method in the current state was invoked, false if no method is present.</returns>
    protected bool RelayToCurrentState(params object[] param) {
        if (CurrentState == null) { return false; }
        string callingMethodName = new System.Diagnostics.StackFrame(1).GetMethod().Name;
        if (!callingMethodName.StartsWith("Upon")) {
            D.Warn("Calling method name: {0) should start with 'Upon'.", callingMethodName);
        }
        var message = CurrentState.ToString() + Constants.Underscore + callingMethodName;
        //D.Log(ShowDebugLog, "{0} looking for method signature {1}.", DebugName, message);
        return SendMessageEx(message, param);
    }

    //Holds a cache of whether a message is available on a type
    private static IDictionary<Type, IDictionary<string, MethodInfo>> _messages = new Dictionary<Type, IDictionary<string, MethodInfo>>();
    //Holds a local cache of Action delegates where the method is an action
    private IDictionary<string, Action> _actions = new Dictionary<string, Action>();

    /// <summary>
    /// Optimized SendMessage replacement.
    /// WARNING: BindingFlags.NonPublic DOES NOT find private methods in classes that are a base class of a derived class! 
    /// Do not interpret this to mean a base class of this instance. I mean a base class period. This is noted in GetMethods() 
    /// below, but NOT in the comparable GetMethod() documentation!
    /// <see cref="https://msdn.microsoft.com/en-us/library/4d848zkb(v=vs.110).aspx"/>
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="param">The parameters.</param>
    /// <returns>true if the method with signature <c>message</c> was invoked.</returns>
    private bool SendMessageEx(string message, object[] param) {
        //Have we found that a delegate was already created
        var actionSpecified = false;
        //Try to get an Action delegate for the message
        Action a = null;
        //Try to find a cached delegate
        if (_actions.TryGetValue(message, out a)) {
            //If we got one then call it
            actionSpecified = true;
            //a will be null if we previously tried to get an action and failed
            if (a != null) {
                a();
                return true;    // my addition of true
            }
        }

        //Otherwise try to get the method for the name
        MethodInfo mtd = null;
        IDictionary<string, MethodInfo> lookup = null;
        //See if we have scanned this type already
        if (!_messages.TryGetValue(GetType(), out lookup)) {
            //If we haven't then create a lookup for it, this will cache message names to their method info
            lookup = new Dictionary<string, MethodInfo>();
            _messages[GetType()] = lookup;
        }
        //See if we have already search for this message for this type (not instance)
        if (!lookup.TryGetValue(message, out mtd)) {
            //If we haven't then try to find it
            mtd = GetType().GetMethod(message, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            //Cache for later
            lookup[message] = mtd;
        }
        //If this message exists as a method...	
        if (mtd != null) {
            //If we haven't already tried to create an action
            if (!actionSpecified) {
                //Ensure that the message requires no parameters and returns nothing
                if (mtd.GetParameters().Length == 0 && mtd.ReturnType == typeof(void)) {
                    //Create an action delegate for it
                    var action = (Action)Delegate.CreateDelegate(typeof(Action), this, mtd);
                    //Cache the delegate
                    _actions[message] = action;
                    //Call the function
                    action();
                }
                else {
                    //Otherwise flag that we cannot call this method thru the delegate system, then slow invoke it
                    _actions[message] = null;
                    mtd.Invoke(this, param);    // my addition
                }
            }
            else {
                //Otherwise slow invoke the method passing the parameters
                mtd.Invoke(this, param);
            }
            return true; // my addition
        }
        else {
            int paramCount = param.IsNullOrEmpty() ? Constants.Zero : param.Length;
            string lastStateMsg = LastState != null ? LastState.ToString() : "null";
            D.Warn("{0} did not find Method with signature {1}(paramCount: {2}). LastState: {3}. Is it a private method in a base class?",
                DebugName, message, paramCount, lastStateMsg);  // my addition
            return false;   // my addition
        }
    }

    #endregion

    protected virtual bool IsPaused { get { return false; } }

    /// <summary>
    /// Gets the seconds spent in the current state
    /// </summary>
    protected float timeInCurrentState {
        get {
            return Time.time - _timeEnteredState;
        }
    }

    /// <summary>
    /// Indicates whether this FSM is in the 1 frame time gap between a Call()ed state's
    /// Return() and FsmReturnHandler's processing of the Return cause.
    /// <remarks>Used by clients to detect presence in this 1 frame gap for debug and filtering.
    /// 6.23.18 Currently used by Cmd clients to detect and/or filter out leaked order outcome callbacks.</remarks>
    /// </summary>
    protected bool _isWaitingToProcessReturn;

    /// <summary>
    /// The enter state coroutine.
    /// </summary>
    [HideInInspector]
    private InterruptableCoroutine enterStateCoroutine;

    /// <summary>
    /// The exit state coroutine.
    /// </summary>
    [HideInInspector]
    private InterruptableCoroutine exitStateCoroutine;

    /// <summary>
    /// The time that the current state was entered
    /// </summary>
    private float _timeEnteredState;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        // I changed the order in which these are called. If left as it was, if ExitState() was IEnumerable
        // along with EnterState(), then the EnterState would be executed before ExitState. I never saw this as
        // a problem as all my ExitState()s return void.
        exitStateCoroutine = new InterruptableCoroutine(this, isEnterStateCoroutine: false);
        enterStateCoroutine = new InterruptableCoroutine(this, isEnterStateCoroutine: true);
    }

    #region Default Implementations Of Delegates

    private static IEnumerator DoNothingCoroutine() {
        yield break;
    }

    private static void DoNothing() { }

    private static void DoNothingCollider(Collider other) { }

    private static void DoNothingCollision(Collision other) { }

    //private static void DoNothingBoolean(bool b) { }

    #endregion

    /// <summary>
    /// Container class that holds the settings associated with a particular state.
    /// </summary>
    private class State {

        public Action DoUpdate = DoNothing;
        [Obsolete]
        public Action DoOccasionalUpdate = DoNothing;
        public Action DoLateUpdate = DoNothing;
        public Action DoFixedUpdate = DoNothing;
        public Action<Collider> DoOnTriggerEnter = DoNothingCollider;
        public Action<Collider> DoOnTriggerStay = DoNothingCollider;
        public Action<Collider> DoOnTriggerExit = DoNothingCollider;
        public Action<Collision> DoOnCollisionEnter = DoNothingCollision;
        public Action<Collision> DoOnCollisionStay = DoNothingCollision;
        public Action<Collision> DoOnCollisionExit = DoNothingCollision;

        public Func<IEnumerator> enterState = DoNothingCoroutine;
        public Func<IEnumerator> exitState = DoNothingCoroutine;
        public IEnumerator enterStateEnumerator = null;
        public IEnumerator exitStateEnumerator = null;

        //public Action<bool> DoOnHover = DoNothingBoolean;
        //public Action<bool> DoOnPress = DoNothingBoolean;
        //public Action DoOnClick = DoNothing;
        //public Action DoOnDoubleClick = DoNothing;

        public object currentState;

        //Stack of the enter state enumerators
        public Stack<IEnumerator> enterStack;

        //Stack of the exit state enumerators
        public Stack<IEnumerator> exitStack;

        //The amount of time that was spend in this state when pushed to the stack
        public float time;

    }

    /// <summary>
    /// A state container instance.
    /// </summary>
    [HideInInspector]
    private State state = new State();

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

    protected object CurrentState {
        get { return state.currentState; }
        set {
            D.Assert(!state.Equals(value)); // a state object and a state's currentState should never be equal
            __ValidateNoNewStateSetDuringVoidEnterState(value);
            ChangingState();
            //string lastStateMsg = LastState != null ? LastState.ToString() : "null";
            //D.Log(ShowDebugLog, "{0} changing CurrentState from {1} to {2}. Frame: {3}.", DebugName, lastStateMsg, value.ToString(), Time.frameCount);
            state.currentState = value;
            ConfigureCurrentState();
            __ResetStateChangeValidationTest();
        }
    }

    /// <summary>
    /// The State previous to CurrentState.
    /// </summary>
    protected object LastState { get; private set; }

    //Stack of the previous running states
    private Stack<State> _stack = new Stack<State>();

    /// <summary>
    /// Call the specified state - immediately suspends execution of the current state
    /// method that Call()ed, then loads the new state and executes its EnterState() method 
    /// WITHOUT running the ExitState() of the current state.  If stateToActivate's EnterState() 
    /// method returns void, it is executed immediately. If it returns IEnumerator, it is 
    /// executed during the next Update(). Called states need to execute Return() when they terminate.
    /// </summary>
    /// <param name='stateToActivate'> State to activate. </param>
    protected void Call(object stateToActivate) {
        var callerEnterState = state.enterState;
        D.AssertEqual(typeof(IEnumerator), callerEnterState.Method.ReturnType);
        //D.Log(ShowDebugLog, "{0}.Call({1}) called.", DebugName, stateToActivate.ToString());
        state.time = timeInCurrentState;
        state.enterStack = enterStateCoroutine.CreateStack();
        state.exitStack = exitStateCoroutine.CreateStack();

        ChangingState();

        _stack.Push(state);
        state = new State();
        state.currentState = stateToActivate;
        ConfigureCurrentStateForCall();
    }

    //Configures the state machine when the new state has been called
    private void ConfigureCurrentStateForCall() {
        //D.Log(ShowDebugLog, "{0}.ConfigureCurrentStateForCall() called.", DebugName);
        GetStateMethods();
        if (state.enterState != null) {
            PreconfigureCurrentState(); // 10.16.16 My addition
            //D.Log(ShowDebugLog, "{0} setting up {1}_EnterState() to execute a Call().", DebugName, CurrentState.ToString());
            state.enterStateEnumerator = state.enterState();
            enterStateCoroutine.Run(state.enterStateEnumerator);    // must call as null stops any prior IEnumerator still running
        }
    }

    /// <summary>
    /// Return this state from a call. Immediately after the completion of this method, CurrentState will reflect the CallingState
    /// that Call()ed CalledState. As CallingState_EnterState() must return IEnumerator to have Call()ed CalledState, 
    /// CallingState_EnterState() will resume execution from where it Call()ed CalledState during the next Update().
    /// </summary>
    protected void Return() {
        // 6.23.18 Don't Assert !_isWaitingToProcessReturn as multiple Return()s can be called in sequence without 
        // any FsmReturnHandler processing when Call()ed states are two or more deep - see client's ReturnFromCalledStates()
        _isWaitingToProcessReturn = true;

        //D.Log("{0}: Return() from state {1} called.", DebugName, CurrentState.ToString());
        if (state.exitState != null) {
            //D.Log(ShowDebugLog, "{0} setting up {1}_ExitState() to run in Return(). MethodName: {2}.", DebugName, CurrentState.ToString(), state.exitState.Method.Name);
            state.exitStateEnumerator = state.exitState();  // a void exitState() method executes immediately here rather than wait until the enterCoroutine makes its next pass
            exitStateCoroutine.Run(state.exitStateEnumerator);  // must call as null stops any prior IEnumerator still running
        }

        // My ChangingState() addition moved below as CurrentState doesn't change if state calling Return() wasn't Call()ed

        if (_stack.Count > 0) {
            ChangingState();    // my addition to keep lastState in sync
            state = _stack.Pop();
            //D.Log(ShowDebugLog, "{0} setting up resumption of {1}_EnterState() in Return(). MethodName: {2}.", DebugName, CurrentState.ToString(), state.enterState.Method.Name);
            // 1.24.18 No need to attempt to execute a void EnterState, aka state.enterState() as Return() only used by state that was Call()ed.
            // Call()ed states can only be Call()ed from and IEnumerator EnterState
            enterStateCoroutine.Run(state.enterStateEnumerator, state.enterStack);
            _timeEnteredState = Time.time - state.time;
        }
        else {
            D.Error("{0} StateMachine: Return() called from state {1} that wasn't Call()ed.", DebugName, state.currentState.ToString());
            // UNCLEAR CurrentState remains the same, but it has already run it's ExitState(). What does that mean?
            // Shouldn't ExitState() run only if _stack.Count > 0? -> Return() is ignored if state wasn't Call()ed
        }
    }

    /// <summary>
    /// Return the state from a call with a specified state to 
    /// enter if this state wasn't called.
    /// </summary>
    /// <param name='baseState'>
    /// The state to use if there is no waiting calling state.
    /// </param>
    protected void Return(object baseState) {
        // 6.23.18 Don't Assert !_isWaitingToProcessReturn as multiple Return()s can be called in sequence without 
        // any FsmReturnHandler processing when Call()ed states are two or more deep - see client's ReturnFromCalledStates()
        _isWaitingToProcessReturn = true;

        //D.Log("{0}.Return({1}) from state {2} called.", DebugName, baseState.ToString(), CurrentState.ToString());
        if (state.exitState != null) {
            state.exitStateEnumerator = state.exitState();
            exitStateCoroutine.Run(state.exitStateEnumerator);  // must call as null stops any prior IEnumerator still running
        }

        ChangingState();    // my addition to keep lastState in sync

        if (_stack.Count > 0) {
            state = _stack.Pop();
            enterStateCoroutine.Run(state.enterStateEnumerator, state.enterStack);
        }
        else {
            // Warn just for visibility
            D.Warn("{0} StateMachine: Return({1}) called from state {2} that wasn't Call()ed.",
                DebugName, baseState.ToString(), state.currentState.ToString());
            CurrentState = baseState;
        }
        _timeEnteredState = Time.time - state.time;
    }

    /// <summary>
    /// Caches previous states
    /// </summary>
    private void ChangingState() {
        //D.Log(ShowDebugLog, "{0}.ChangingState() called.", DebugName);
        LastState = state.currentState;
        _timeEnteredState = Time.time;
    }

    /// <summary>
    /// Configures the state machine for the current state.
    /// <remarks>11.17.17 ExitState methods no longer allowed to return IEnumerator as next state's void PreconfigureCurrentState() will
    /// always run before the following state's IEnumerator ExitState(). Error check occurs in ConfigureDelegate().</remarks>
    /// </summary>
    private void ConfigureCurrentState() {
        //D.Log(ShowDebugLog, "{0}.ConfigureCurrentState() called.", DebugName);
        if (state.exitState != null) {
            if (LastState != null) {
                //D.Log(ShowDebugLog, "{0} setting up {1}_ExitState() to run.", DebugName, LastState.ToString());
            }

            // runs the exitState of the PREVIOUS state as the state delegates haven't been changed yet
            state.exitStateEnumerator = state.exitState();
            exitStateCoroutine.Run(state.exitStateEnumerator);  // must call as null stops any prior IEnumerator still running
        }

        GetStateMethods();

        if (state.enterState != null) {
            PreconfigureCurrentState(); // 10.16.16 My addition

            //D.Log(ShowDebugLog, "{0} setting up {1}_EnterState() to run. MethodName: {2}.", DebugName, CurrentState.ToString(), state.enterState.Method.Name);
            state.enterStateEnumerator = state.enterState();    // a void enterState() method executes immediately here rather than wait until the enterCoroutine makes its next pass
                                                                //D.Log(ShowDebugLog && state.enterStateEnumerator == null, "{0}: {1}.enterStateEnumerator is null and about to run in EnterStateCoroutine. MethodName: {2}.", 
                                                                //DebugName, CurrentState.ToString(), state.enterState.Method.Name);
            enterStateCoroutine.Run(state.enterStateEnumerator);    // must call as null stops any prior IEnumerator still running
            //D.Log(ShowDebugLog, "{0} after setting up {1}_EnterState() to run.", DebugName, CurrentState.ToString());
        }
    }

    //Retrieves all of the methods for the current state
    private void GetStateMethods() {
        //D.Log(ShowDebugLog, "{0}.GetStateMethods() called.", DebugName);
        //Now we need to configure all of the methods
        state.DoUpdate = ConfigureDelegate<Action>("Update", DoNothing);
        //state.DoOccasionalUpdate = ConfigureDelegate<Action>("OccasionalUpdate", DoNothing);
        state.DoLateUpdate = ConfigureDelegate<Action>("LateUpdate", DoNothing);
        state.DoFixedUpdate = ConfigureDelegate<Action>("FixedUpdate", DoNothing);
        state.DoOnTriggerEnter = ConfigureDelegate<Action<Collider>>("OnTriggerEnter", DoNothingCollider);
        state.DoOnTriggerExit = ConfigureDelegate<Action<Collider>>("OnTriggerExit", DoNothingCollider);
        state.DoOnTriggerStay = ConfigureDelegate<Action<Collider>>("OnTriggerStay", DoNothingCollider);
        state.DoOnCollisionEnter = ConfigureDelegate<Action<Collision>>("OnCollisionEnter", DoNothingCollision);
        state.DoOnCollisionExit = ConfigureDelegate<Action<Collision>>("OnCollisionExit", DoNothingCollision);
        state.DoOnCollisionStay = ConfigureDelegate<Action<Collision>>("OnCollisionStay", DoNothingCollision);

        //state.DoOnHover = ConfigureDelegate<Action<bool>>("OnHover", DoNothingBoolean);
        //state.DoOnPress = ConfigureDelegate<Action<bool>>("OnPress", DoNothingBoolean);
        //state.DoOnClick = ConfigureDelegate<Action>("OnClick", DoNothing);
        //state.DoOnDoubleClick = ConfigureDelegate<Action>("OnDoubleClick", DoNothing);

        state.enterState = ConfigureDelegate<Func<IEnumerator>>(EnterStateText, DoNothingCoroutine);
        state.exitState = ConfigureDelegate<Func<IEnumerator>>(ExitStateText, DoNothingCoroutine);
    }

    /// <summary>
    /// Hook that allows a derived class to do any state-specific work required in preparation 
    /// for the state to run, starting with State_EnterState(). Default does nothing.
    /// <remarks>Intended to bridge the gap in this FSM engine design that allows a state to be 
    /// the 'CurrentState' for a period prior to the state's EnterState() executing. This is because
    /// many EnterState()s execute as coroutines which means there is up to a 1 frame gap between 
    /// the time the state becomes the 'CurrentState', to the time the state's EnterState() begins execution.
    /// This is a guaranteed 'hard to find' bug if RelayToCurrentState is used to bring async events to the state.
    /// As these events can happen at any time, they can also occur before EnterState() has begun, aka when no 
    /// state condition really exists as nothing has been set. This method allows the derived class to 
    /// configure the state for operation IMMEDIATELY (read: atomically) after the state becomes the 'CurrentState'.
    /// </remarks>
    /// <remarks>4.8.17 Now know that an IEnumerator EnterState is processed during the same frame as 
    /// PreconfigureCurrentState, albeit later in the frame during Coroutine processing after most code
    /// to be executed during the frame has already executed, including that in PreconfigureCurrentState.</remarks>
    /// </summary>
    protected virtual void PreconfigureCurrentState() { }

    /// <summary>
    /// A cache of the delegates for a particular state and method
    /// </summary>
    private Dictionary<object, Dictionary<string, Delegate>> _cache = new Dictionary<object, Dictionary<string, Delegate>>();

    /// <summary>
    /// FInds or creates a delegate for the current state and Method name (aka CurrentState_OnClick), or
    /// if the Method name is not present in this State Machine, then returns Default. Also puts an 
    /// IEnumerator wrapper around EnterState or ExitState methods that return void rather than
    /// IEnumerator.
    /// WARNING: BindingFlags.NonPublic DOES NOT find private methods in classes that are a base class of a derived class! 
    /// Do not interpret this to mean a base class of this instance. I mean a base class period. This is noted in GetMethods() 
    /// below, but NOT in the comparable GetMethod() documentation!
    /// <see cref="https://msdn.microsoft.com/en-us/library/4d848zkb(v=vs.110).aspx"/>
    /// </summary>
    /// <typeparam name="T">The type of delegate.</typeparam>
    /// <param name="methodRoot">Substring of the methodName that follows "StateName_", e.g. EnterState from State1_EnterState.</param>
    /// <param name="Default">The default delegate to use if a method of the proper name is not found.</param>
    /// <returns></returns>
    private T ConfigureDelegate<T>(string methodRoot, T Default) where T : class {

        Dictionary<string, Delegate> lookup;
        if (!_cache.TryGetValue(state.currentState, out lookup)) {
            _cache[state.currentState] = lookup = new Dictionary<string, Delegate>();
        }
        Delegate returnValue;
        if (!lookup.TryGetValue(methodRoot, out returnValue)) {

            var mtd = GetType().GetMethod(MethodNameFormat.Inject(state.currentState.ToString(), methodRoot), System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod);

            if (mtd != null) {
                // only enterState and exitState T delegates are of Type Func<IEnumerator> see GetStateMethods above
                bool isEnterExitStateMethod = typeof(T) == typeof(Func<IEnumerator>);
                bool isVoidReturnType = mtd.ReturnType != typeof(IEnumerator);
                bool isExitStateMethod = isEnterExitStateMethod && methodRoot == ExitStateText;
                if (isExitStateMethod && !isVoidReturnType) {
                    D.Error("{0} Illegal exit state return type as nextState.PreconfigureState() will always run before lastState.ExitState()!", DebugName);
                }

                if (isEnterExitStateMethod && isVoidReturnType) {
                    // the enter or exit method returns void, so adjust it to execute properly when placed in the IEnumerator delegate
                    Action a = Delegate.CreateDelegate(typeof(Action), this, mtd) as Action;
                    Func<IEnumerator> func = () => { a(); return null; };
                    returnValue = func;
                }
                else {
                    returnValue = Delegate.CreateDelegate(typeof(T), this, mtd);
                }
            }
            else {
                returnValue = Default as Delegate;
                if (methodRoot == EnterStateText || methodRoot == ExitStateText) {
                    D.Warn("{0} did not find method {1}_{2}. Is it a private method in a base class?",
                        DebugName, state.currentState.ToString(), methodRoot);
                }
            }
            lookup[methodRoot] = returnValue;
        }
        return returnValue as T;
    }

    #region Pass On Methods

    //void Update() {
    //    state.DoUpdate();
    //}

    [Obsolete]
    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        state.DoOccasionalUpdate();
    }

    //void LateUpdate() {
    //    state.DoLateUpdate();
    //}

    //void FixedUpdate() {
    //    state.DoFixedUpdate();
    //}

    //void OnTriggerEnter(Collider other) {
    //    state.DoOnTriggerEnter(other);
    //}

    //void OnTriggerStay(Collider other) {
    //    state.DoOnTriggerStay(other);
    //}

    //void OnTriggerExit(Collider other) {
    //    state.DoOnTriggerExit(other);
    //}

    void OnCollisionEnter(Collision collision) {
        state.DoOnCollisionEnter(collision);
    }

    //void OnCollisionStay(Collision col) {
    //    state.DoOnCollisionStay(col);
    //}

    //void OnCollisionExit(Collision col) {
    //    state.DoOnCollisionExit(col);
    //}

    ////protected override void OnHover(bool isOver) {
    ////    base.OnHover(isOver);
    ////    state.DoOnHover(isOver);
    ////}

    ////protected override void OnPress(bool isDown) {
    ////    base.OnPress(isDown);
    ////    state.DoOnPress(isDown);
    ////}

    ////protected override void OnClick() {
    ////    base.OnClick();
    ////    state.DoOnClick();
    ////}

    ////protected override void OnDoubleClick() {
    ////    base.OnDoubleClick();
    ////    state.DoOnDoubleClick();
    ////}

    #endregion

    #region Debug

    // Changing state during ConfigureCurrentState Test
    // ------------------------------------------------
    // WARNING: Making a state change directly or indirectly (e.g. thru another method or issuing an order) from a void 
    // previousState_ExitState(), state_UponPreconfigureState() or state_EnterState() method ('the void method') will cause the state machine
    // to lose its place. This even occurs if the state change is the last line of code in 'the void method'. 
    // What happens: when 'the void method' executes, it executes in ConfigureCurrentState(). When 'the void method' changes
    // the state during that execution, CurrentState_set is called. This occurs before 'the void method' and ConfigureCurrentState() completes. 
    // The 'return to here to complete' code pointer remembers it has more code in 'the void method' and ConfigureCurrentState() to execute. 
    // As a result of CurrentState_set, ConfigureCurrentState() is called again but for the new state. This ConfigureCurrentState() call 
    // is executed all the way through, including executing _ExitState(), _UponPreconfigureState() and executing (or scheduling for execution
    // via coroutine) _EnterState(). Once this ConfigureCurrentState() completes execution, the remainder of CurrentState_set is completed. 
    // It is at this stage, that the code execution path returns to finishing 'the [original] void method'. Once this is finished, 
    // the next line of code to execute is back to the original ConfigureCurrentState() to finish up which includes calling Run(IEnumerator). 
    // Unfortunately, since 'the [original] void method' returns void, the IEnumerator is null and it overwrites the previously 
    // scheduled (but not yet executed) new 'the void method' which then never executes. The state machine is still operational, 
    // but it has missed executing a method it was supposed to execute. It will however respond to a new state change.

    // A similar problem occurs if the state machine supports state change notification events. Changing state while executing 
    // CurrentState_set means the first state change notification that occurs is the new state, not the state that caused CurrentState_set
    // to be called again. In other words, the state change notifications get out of sync. This problem is not present in 
    // AMortalItemStateMachine as it doesn't support state change notifications.

    private bool __hasCurrentState_setFinishedWithoutInterveningSet = true;

    /// <summary>
    /// Validates that a void previousState_ExitState(), State_UponPreconfigureState() or State_EnterState() method 
    /// (called in ConfigureCurrentState()) does not attempt to set a new state.
    /// <remarks>State_EnterState() methods that return IEnumerator that set a new state value should not fail this test as 
    /// the coroutine that executes the EnterState() is only run after CurrentState_set completes.</remarks>
    /// </summary>
    private void __ValidateNoNewStateSetDuringVoidEnterState(object incomingState) {
        //D.Log(ShowDebugLog, "{0}.__ValidateNoNewStateSetDuringVoidEnterState() called. CurrentState = {1}, IncomingState = {2}.", DebugName, CurrentState, incomingState);
        if (!__hasCurrentState_setFinishedWithoutInterveningSet) {
            D.Error("{0} cannot change state to {1} while executing {2}_EnterState().", DebugName, incomingState, CurrentState);
            return;
        }
        __hasCurrentState_setFinishedWithoutInterveningSet = false;
    }

    private void __ResetStateChangeValidationTest() {
        //D.Log(ShowDebugLog, "{0}.__ResetStateChangeValidationTest() called. CurrentState = {1}.", DebugName, CurrentState);
        __hasCurrentState_setFinishedWithoutInterveningSet = true;
    }

    [Obsolete]  // 11.17.17 didn't work as I can't determine the parameters since EnterState() and ExitState() are both Func<IEnumerator>
    private void __ValidateMethodReturnTypes(bool exitStateMethodReturnsIEnumerator, bool enterStateMethodReturnsVoid) {
        if (exitStateMethodReturnsIEnumerator) {    // deadly as preConfigureState will execute before exitState
            D.Warn("{0} Illegal exit state return type as nextState.PreconfigureState() will always run before lastState.ExitState()!", DebugName);
            if (enterStateMethodReturnsVoid) {
                string lastStateMsg = LastState != null ? LastState.ToString() : "null";
                string msg = "{0} Illegal Combination of return types. ExitState: {1}, EntryState: {2}.".Inject(DebugName, lastStateMsg, CurrentState.ToString());
                throw new InvalidOperationException(msg);  // deadly combination as enter will execute before exit
            }
        }
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// A coroutine executor that can be interrupted
    /// </summary>
    private class InterruptableCoroutine {

        private const string DebugNameFormat = "{0}_{1}";

        private string _debugName;
        private string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(_fsm.DebugName, _coroutineName);
                }
                return _debugName;
            }
        }

        /// <summary>
        /// Returns the appropriate state name depending on whether this Coroutine
        /// is for EntryState methods or ExitState methods.
        /// </summary>
        private string ApplicableStateName {
            get {
                string stateName = "NullState";
                if (_isEnterStateCoroutine) {
                    if (_fsm.CurrentState != null) {
                        stateName = _fsm.CurrentState.ToString();
                    }
                }
                else {
                    if (_fsm.LastState != null) {
                        stateName = _fsm.LastState.ToString();
                    }
                }
                return stateName;
            }
        }

        private bool ShowDebugLog { get { return _fsm.ShowDebugLog; } }

        /// <summary>
        /// Stack of executing coroutines
        /// </summary>
        private Stack<IEnumerator> _stack = new Stack<IEnumerator>();
        private IEnumerator _enumerator;
        private AMortalItemStateMachine _fsm;
        private string _coroutineName;
        private bool _isEnterStateCoroutine;
        private bool __isPausedLogged;

        /// <summary>
        /// Initializes a new instance of the <see cref="InterruptableCoroutine" /> class.
        /// </summary>
        /// <param name="fsm">The FSM.</param>
        /// <param name="isEnterStateCoroutine">if set to <c>true</c> [is enter state coroutine].</param>
        public InterruptableCoroutine(AMortalItemStateMachine fsm, bool isEnterStateCoroutine) {
            _fsm = fsm;
            _isEnterStateCoroutine = isEnterStateCoroutine;
            _coroutineName = isEnterStateCoroutine ? "EnterStateCoroutine" : "ExitStateCoroutine";
            _fsm.StartCoroutine(Run());
        }

        /// <summary>
        /// A coroutine that runs a single yield instruction
        /// </summary>
        /// <returns>
        /// The instruction coroutine.
        /// </returns>
        /// <param name='info'>
        /// The info packet for the coroutine to run
        /// </param>
        private IEnumerator YieldInstructionCoroutine(CoroutineInfo info) {
            info.done = false;
            yield return info.instruction;
            info.done = true;
        }

        /// <summary>
        /// Waits for a yield instruction
        /// </summary>
        /// <returns>
        /// The coroutine to execute
        /// </returns>
        /// <param name='instruction'>
        /// The instruction to run
        /// </param>
        private IEnumerator WaitForCoroutine(YieldInstruction instruction) {
            var ci = new CoroutineInfo { instruction = instruction, done = false };
            _fsm.StartCoroutine(YieldInstructionCoroutine(ci));
            while (!ci.done) {
                yield return null;
            }
        }

        private IEnumerator Run() {
            //Loop forever
            while (true) {
                // 10.27.17 Moved pause wait before _enumerator null check as _enumerator will be changed to null (even while paused) when 
                // a state with a IEnumerator Enter/Exit method changes to a state with a void EnterExit method (e.g. when changing to Dead)
                while (_fsm.IsPaused) {
                    // 5.3.17 My addition to pause FSM while Game is paused
                    if (!__isPausedLogged) {
                        D.Log(ShowDebugLog, "{0} is waiting while game is paused.", DebugName);
                        __isPausedLogged = true;
                    }
                    yield return null;
                }
                __isPausedLogged = false;


                //Check if we have a current coroutine
                //D.Log(ShowDebugLog, "{0} beginning another while(true) pass during Frame {1}.", DebugName, Time.frameCount);
                if (_enumerator != null) {

                    //D.Log(ShowDebugLog, "Updating {0}.Run() with non-null IEnumerator. State: {1}, Frame: {2}.", DebugName, ApplicableStateName, Time.frameCount);
                    //Make a copy of the enumerator in case it changes
                    var enm = _enumerator;
                    //Execute the next step of the coroutine    
                    //D.Log(ShowDebugLog, "{0} code block is about to execute. State: {1}, Frame: {2}.", DebugName, ApplicableStateName, Time.frameCount);
                    // MoveNext executes the code block up to the next yield or the end of the method. Returns true when it finds a yield at the end
                    // of the code block, false if no yield indicating no more code remaining to execute.
                    var valid = _enumerator.MoveNext();
                    //D.Log(ShowDebugLog, "{0} after MoveNext(). State: {1}, Valid = {2}, Frame: {3}.", DebugName, ApplicableStateName, valid, Time.frameCount);
                    // See if the enumerator has changed. This only happens as a result of the just executed code block changing state.
                    if (enm == _enumerator) {
                        //If this is the same enumerator
                        if (_enumerator != null && valid) {
                            //Get the result of the yield
                            var result = _enumerator.Current;   // the return value of the yield
                            //Check if it is a coroutine
                            if (result is IEnumerator) {    // wait until another coroutine completes, e.g. CustomYieldInstruction
                                //D.Log(ShowDebugLog, "{0} CodeBlock is IEnumerator. State: {1}.", DebugName, ApplicableStateName); 
                                //Push the current coroutine and execute the new one
                                _stack.Push(_enumerator);
                                _enumerator = result as IEnumerator;
                                yield return null;
                            }
                            //Check if it is a yield instruction
                            else if (result is YieldInstruction) {  // a YieldInstruction is the parent of the built in WaitFor... classes
                                //D.Log(ShowDebugLog, "{0} CodeBlock is YieldInstruction. State: {1}.", DebugName, ApplicableStateName); 
                                //To be able to interrupt yield instructions we need to run them as a separate coroutine and wait for them
                                _stack.Push(_enumerator);
                                //Create the coroutine to wait for the yield instruction
                                _enumerator = WaitForCoroutine(result as YieldInstruction);
                                yield return null;
                            }
                            // Otherwise return the value return by the yield (typically null, aka yield return null)
                            else {
                                //string currentMsg = result != null ? "_enumerator.Current ({0})".Inject(result.ToString()) : "_enumerator.Current (null)";
                                //D.Log(ShowDebugLog, "{0} returning {1}. Next comment should be from frame {2}.", DebugName, currentMsg, Time.frameCount + 1);
                                yield return result;    //_enumerator.Current;
                            }
                        }
                        else {
                            //If the enumerator was set to null below then we need to mark this as invalid. Could also get here if valid
                            // is already false (aka no more yields in method) so no harm setting false again.
                            valid = false;
                            yield return null;
                        }
                        // Check if we are in a valid state. 
                        // Note: This is reached the frame following the above yields as this Run() coroutine will resume its own execution
                        // on the line following the yield return null, aka this line.
                        if (!valid) {
                            //If not then see if there are any stacked coroutines
                            if (_stack.Count >= 1) {
                                //Get the stacked coroutine back
                                _enumerator = _stack.Pop();
                            }
                            else {
                                //Ensure we don't use this enumerator again

                                //if (_enumerator != enm) { 
                                //    D.Error("{0} attempting to null _enumerator that has changed. ApplicableState: {1}, _enumerator is null: {2}, enm is null: {3}, CurrentState: {4}, LastState: {5}.",
                                //    DebugName, ApplicableStateName, _enumerator == null, enm == null, _fsm.CurrentState.ToString(), _fsm.LastState.ToString());
                                //}

                                // Note: Nulling _enumerator after _enumerator has just changed will result in the new _enumerator NOT 
                                // executing with no reported error. This occurred when I placed a yield return null at end of 
                                // Idling_EnterState causing _enumerator to be nulled one frame later, sometimes just after _enumerator 
                                // was set to ExecuteMoveOrder_EnterState. _enumerator is nulled when it is no longer valid (execution 
                                // of last code block finished). This occurs 1 frame after it becomes invalid. As all enumerators become 
                                // invalid, it is possible _enumerator could be externally changed during this frame - from an
                                // order change for instance. This happens during patrolling for instance when completion of the previous
                                // patrol move sets the state to Idling, followed a frame later by a state change to ExecuteMoveOrder as
                                // a result of an order from fleet to make the next move of the patrol pattern. enm is Idling_EnterState, 
                                // and the new _enumerator is ExecuteMoveOrder_EnterState. Without this equality filter below, 
                                // ExecuteMoveOrder_EnterState never executes as it is set to null here. The state remains ExecuteMoveOrder
                                // but with its EnterState having not run, the state machine is lost. 
                                if (_enumerator == enm) {
                                    // _enumerator hasn't been changed externally during the frame it took to get here so it is OK to null
                                    _enumerator = null;
                                    //D.Log(ShowDebugLog, "{0}.Run() _enumerator set to null. Frame: {1}.", DebugName, Time.frameCount);
                                }
                            }
                            // Starts at top again without waiting for the next frame
                            //D.Log(ShowDebugLog, "{0}.Run() is continuing in same frame. Frame: {1}.", DebugName, Time.frameCount);
                        }
                    }
                    else {
                        //_enumerator changed by MoveNext() executed code assigning a new state. _enumerator here will be null if the
                        // Enter/ExitState() method of the new state returns void
                        //D.Log(ShowDebugLog, "{0}.Run()._enumerator was changed by executed code block. State: {1}, Frame: {2}.", DebugName, ApplicableStateName, Time.frameCount);
                        yield return null;
                    }
                }
                else {
                    //If the enumerator was null then just yield    // aka there is no method currently present to execute right now
                    yield return null;
                }
                //D.Log(ShowDebugLog, "{0} is returning for another while(true) pass during Frame {1}.", DebugName, Time.frameCount);
            }
        }

        /// <summary>
        /// Call the specified coroutine
        /// </summary>
        /// <param name='enm'>
        /// The coroutine to call
        /// </param>
        public void Call(IEnumerator enm) {
            _stack.Push(_enumerator);
            _enumerator = enm;
        }

        /// <summary>
        /// Run the specified coroutine with an optional stack
        /// </summary>
        /// <param name='enm'>
        /// The coroutine to run
        /// </param>
        /// <param name='stack'>
        /// The stack that should be used for this coroutine
        /// </param>
        public void Run(IEnumerator enm, Stack<IEnumerator> stack = null) {
            //string msg = en != null ? "IEnumerator" : "NULL_IEnumerator";
            //D.Log(ShowDebugLog, "{0}.Run({1}) called. CurrentState: {2}, Frame: {3}.", DebugName, msg, CurrentStateName, Time.frameCount);
            //if (msg == "NULL_IEnumerator") {
            //    D.Log(ShowDebugLog, "{0}.Run() was called with null IEnumerator so has already executed. {0} will not execute.", DebugName);
            //}
            _enumerator = enm;
            if (stack != null) {
                _stack = stack;
            }
            else {
                _stack.Clear();
            }
            //enm.MoveNext(); // added per author, still getting NRE
        }

        /// <summary>
        /// Creates a new stack for executing coroutines and return the stack 
        /// that was executing. This effectively suspends the execution of the method
        /// on the stack that was executing as the stack that is returned is no
        /// longer being run by the EnterState or ExitStateCoroutine.
        /// </summary>
        /// <returns>
        /// The stack that was previously being executed, now suspended.
        /// </returns>
        public Stack<IEnumerator> CreateStack() {
            var current = _stack;
            _stack = new Stack<IEnumerator>();
            return current;
        }

        /// <summary>
        /// Cancel the current coroutine
        /// </summary>
        public void Cancel() {
            _enumerator = null;
            _stack.Clear();
        }

        #region Nested Classes

        /// <summary>
        /// Coroutine info for running YieldInstructions as a separate coroutine
        /// </summary>
        private class CoroutineInfo {

            /// <summary>
            /// The instruction to execute
            /// </summary>
            public YieldInstruction instruction;

            /// <summary>
            /// Whether the coroutine is complete
            /// </summary>
            public bool done;
        }

        #endregion
    }

    #endregion

    #region ConfigureDelegate Archive

    //private T ConfigureDelegate<T>(string methodRoot, T Default) where T : class {

    //    Dictionary<string, Delegate> lookup;
    //    if (!_cache.TryGetValue(state.currentState, out lookup)) {
    //        _cache[state.currentState] = lookup = new Dictionary<string, Delegate>();
    //    }
    //    Delegate returnValue;
    //    if (!lookup.TryGetValue(methodRoot, out returnValue)) {

    //        var mtd = GetType().GetMethod(MethodNameFormat.Inject(state.currentState.ToString(), methodRoot), System.Reflection.BindingFlags.Instance
    //            | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod);

    //        if (mtd != null) {
    //            // only enterState and exitState T delegates are of Type Func<IEnumerator> see GetStateMethods above
    //            if (typeof(T) == typeof(Func<IEnumerator>) && mtd.ReturnType != typeof(IEnumerator)) {
    //                // the enter or exit method returns void, so adjust it to execute properly when placed in the IEnumerator delegate
    //                Action a = Delegate.CreateDelegate(typeof(Action), this, mtd) as Action;
    //                Func<IEnumerator> func = () => { a(); return null; };
    //                returnValue = func;
    //            }
    //            else {
    //                returnValue = Delegate.CreateDelegate(typeof(T), this, mtd);
    //            }
    //        }
    //        else {
    //            returnValue = Default as Delegate;
    //            if (methodRoot == EnterStateText || methodRoot == ExitStateText) {
    //                D.Warn("{0} did not find method {1}_{2}. Is it a private method in a base class?",
    //                    DebugName, state.currentState.ToString(), methodRoot);
    //            }
    //            else {
    //                //                    D.Log(ShowDebugLog, @"{0} did not find method {1}_{2}. \n
    //                //                            This is probably because it is not present, but could it be a private method in a base class?",
    //                //                            DebugName, state.currentState.ToString(), methodRoot);
    //            }
    //        }
    //        lookup[methodRoot] = returnValue;
    //    }
    //    return returnValue as T;
    //}

    #endregion

    #region Event Wiring Archive

    // System that hooks declared events to state-specific event handlers automatically via a naming convention. 
    //
    // The way it works is that the SELECTOR element is a way of finding objects - by default 3 selector types are provided out of the box:
    //      void SOMESTATE_OnEVENTNAME_SELECTOR(int x) {}
    //      void Waiting_OnClicked_BigButton() {}
    //
    // _NAME - where the name is the name of a game object.  All scripts attached to the object will be searched for an event called EVENTNAME 
    // and any local methods called SOMESTATE_OnEVENTNAME with a compatible signature will be wired to them
    //
    // _Child_NAME_NAME_NAME -  the NAME is then the path to the name of the child where normal path / are replaced with _.  
    // For example Waiting_OnClicked_Child_Status_BigButton - would find the child at the path from the current object /Status/BigButton
    //
    // _Tag_TAGNAME - all game objects with the TAGNAME that contain scripts with an event called EVENTNAME will be wired to a local method
    // called SOMESTATE_OnEVENTNAME if the signatures are compatible.
    //
    // You can write your own way of finding objects that might contain the events too: By overriding OnWireEvents(EventDef eventInfo) you can 
    // access the selector and the event name and return an array of objects that should be considered for wiring.  For example you could have just
    // an array of GameObjects that you returned always - those that you had set in the Inspector perhaps - or you could process the SELECTOR 
    // yourself using any format you like.  This makes it pretty flexible.

    //public class EventDef {
    //    public string eventName;
    //    public string selector;
    //    public MethodInfo method;
    //}

    //private class WiredEvent {
    //    public System.Reflection.EventInfo evt;
    //    public Delegate dlg;
    //    public object source;
    //}

    //private List<WiredEvent> _wiredEvents = new List<WiredEvent>();

    //private static Dictionary<Type, Dictionary<string, EventDef[]>> _cachedEvents = new Dictionary<Type, Dictionary<string, EventDef[]>>();

    ///// <summary>
    ///// Automatically finds (thru reflection) and caches any event handlers present on this state machine type for the specific state we are in. The event
    ///// handlers will be of the form STATENAME_OnEVENTNAME_SELECTOR where SELECTOR is typically the NAME of the gameObject containing the state
    ///// machine. Child gameObjects and GameObjects with Tags can also be designated for the search. 
    ///// </summary>
    //private void WireEvents() {
    //    var cs = CurrentState.ToString();
    //    var type = GetType();
    //    EventDef[] events;
    //    Dictionary<string, EventDef[]> lookup;

    //    if (!_cachedEvents.TryGetValue(type, out lookup)) {
    //        lookup = new Dictionary<string, EventDef[]>();
    //        _cachedEvents[type] = lookup;
    //    }
    //    if (!lookup.TryGetValue(cs, out events)) {
    //        var length = CurrentState.ToString().Length + 3;
    //        events = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
    //            .Where(m => m.Name.StartsWith(CurrentState.ToString() + "_On"))
    //            .Select(m => new { name = m.Name.Substring(length), method = m })
    //            .Where(n => n.name.IndexOf("_") > 1)
    //            .Select(n => new EventDef {
    //                eventName = n.name.Substring(0, n.name.IndexOf("_")),
    //                selector = n.name.Substring(n.name.IndexOf("_") + 1),
    //                method = n.method
    //            })
    //            .ToArray();
    //        lookup[cs] = events;
    //    }

    //    foreach (var evt in events) {
    //        var list = OnWire(evt);
    //        list.AddRange(list.ToList().Where(l => l is Component).SelectMany(l => ((Component)l).GetComponents<MonoBehaviour>()).Cast<object>());
    //        list.AddRange(list.ToList().Where(l => l is GameObject).SelectMany(l => ((GameObject)l).GetComponents<MonoBehaviour>()).Cast<object>());
    //        var sources =
    //            list.Select(l => new { @event = l.GetType().GetEvent(evt.eventName), source = l })
    //                .Where(e => e.@event != null);  // to qualify as a source, you must have declared an event called eventName
    //        foreach (var source in sources) {
    //            var dlg = Delegate.CreateDelegate(source.@event.EventHandlerType, this, evt.method);
    //            if (dlg != null) {
    //                source.@event.AddEventHandler(source.source, dlg);
    //                var we = new WiredEvent { dlg = dlg, evt = source.@event, source = source.source };
    //                D.Log("Event wired for {0}: name = {1}, selector = {2}, sourceType = {3}.", cs, evt.eventName, evt.selector, we.source.GetType().Name);
    //                D.Log("EventHandler name = {0}.", dlg.Method.Name);
    //                _wiredEvents.Add(we);
    //            }
    //        }
    //    }
    //}

    //public void RefreshEvents() {
    //    UnwireEvents();
    //    WireEvents();
    //}

    //private void UnwireEvents() {
    //    foreach (var evt in _wiredEvents) {
    //        evt.evt.RemoveEventHandler(evt.source, evt.dlg);
    //    }
    //}

    ///// <summary>
    /////Returns a list of Components and GameObjects that have handlers compatible with the event defined by eventInfo.
    ///// </summary>
    ///// <param name="eventInfo">The event information.</param>
    ///// <returns></returns>
    //private List<object> OnWire(EventDef eventInfo) {
    //    List<object> objects = new List<object>();
    //    var extra = OnWireEvent(eventInfo);
    //    if (extra != null) {
    //        objects.AddRange(extra);
    //        if (objects.Count > 0) {
    //            D.Log("Fast OnWireEvent was the source of target gameObjects for search.");
    //            return objects;
    //        }
    //    }
    //    if (eventInfo.selector.StartsWith("Child_")) {
    //        var cs = eventInfo.selector.Substring(6).Replace("_", "/");
    //        objects.Add(transform.Find(cs));
    //        return objects;
    //    }
    //    if (eventInfo.selector.StartsWith("Tag_")) {
    //        objects.AddRange(GameObject.FindGameObjectsWithTag(eventInfo.selector.Substring(4)));
    //        return objects;
    //    }

    //    objects.Add(GameObject.Find(eventInfo.selector));
    //    if (objects.Count > 0) {
    //        D.Log("Slow GameObject.Find(Selector) was used to find target gameObject for search.");
    //    }
    //    return objects;
    //}

    ///// <summary>
    ///// Hook for supplying your list of components and game objects holding MonoBehaviours 
    ///// containing event handlers for this event. If utilized, there will be no other search 
    ///// process pursued as it is presumed you have provided all the gameobjects that
    ///// have the appropriate handlers.
    ///// </summary>
    ///// <param name="eventInfo">The event definition.</param>
    ///// <returns></returns>
    //protected virtual IEnumerable<object> OnWireEvent(EventDef eventInfo) {
    //    return null;
    //}

    #endregion

}

