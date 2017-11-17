// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFSMSingleton.cs
//  Abstract Base class for MonoBehaviour State Machine Singletons to inherit from.
//  WARNING: This version does not support subscribing to State Changes 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Abstract Base class for MonoBehaviour State Machine Singletons to inherit from.
/// WARNING: This version does not support subscribing to State Changes
/// as Call() and Return() make changes without going through CurrentState.set.
/// </summary>
/// <typeparam name="T">The Type of the derived class.</typeparam>
/// <typeparam name="E">The State Type being used, typically an enum type.</typeparam>
public abstract class AFSMSingleton<T, E> : AMonoSingleton<T>
    where T : AMonoSingleton<T>
    where E : struct {

    private const string ExitStateText = "ExitState";
    private const string EnterStateText = "EnterState";
    private const string MethodNameFormat = "{0}_{1}";

    #region RelayToCurrentState

    /// <summary>
    /// Optimized messaging replacement for SendMessage() that binds the
    /// message to the current state. Essentially calls the method on this MonoBehaviour
    /// instance that has the signature "CurrentState_CallingMethodName(param). 
    /// Usage:
    ///     void CallingMethodName(param)  { 
    ///         RelayToCurrentState(param);
    ///     }
    /// </summary>
    /// <param name='param'>
    /// Any parameter passed to the current handler that should be passed on
    /// </param>
    protected void RelayToCurrentState(params object[] param) {
        if (CurrentState.Equals(default(E))) {
            //D.Warn("{0}.RelayToCurrentState() called using default({1}).", GetType().Name, typeof(E).Name);
        }
        string callingMethodName = new System.Diagnostics.StackFrame(1).GetMethod().Name;
        if (!callingMethodName.StartsWith("Upon")) {
            D.Warn("Calling method name: {0) should start with 'Upon'.", callingMethodName);
        }
        var message = CurrentState.ToString() + Constants.Underscore + callingMethodName;
        //D.Log("{0}.RelayToCurrentState content: {1}.", GetType().Name, message);
        SendMessageEx(message, param);
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
    /// <param name="param">The parameter.</param>
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
                transform.name, message, paramCount, lastStateMsg);  // my addition
            return false;   // my addition
        }
    }

    #region SendMessageEx Archive

    ///// <summary>
    ///// Optimized SendMessage replacement.
    ///// </summary>
    ///// <param name="message">The message.</param>
    ///// <param name="param">The parameter.</param>
    //private void SendMessageEx(string message, object[] param) {
    //    //Have we found that a delegate was already created
    //    var actionSpecified = false;
    //    //Try to get an Action delegate for the message
    //    Action a = null;
    //    //Try to find a cached delegate
    //    if (_actions.TryGetValue(message, out a)) {
    //        //If we got one then call it
    //        actionSpecified = true;
    //        //a will be null if we previously tried to get an action and failed
    //        if (a != null) {
    //            a();
    //            return;
    //        }
    //    }

    //    //Otherwise try to get the method for the name
    //    MethodInfo mtd = null;
    //    IDictionary<string, MethodInfo> lookup = null;
    //    //See if we have scanned this type already
    //    if (!_messages.TryGetValue(GetType(), out lookup)) {
    //        //If we haven't then create a lookup for it, this will cache message names to their method info
    //        lookup = new Dictionary<string, MethodInfo>();
    //        _messages[GetType()] = lookup;
    //    }
    //    //See if we have already search for this message for this type (not instance)
    //    if (!lookup.TryGetValue(message, out mtd)) {
    //        //If we haven't then try to find it
    //        mtd = GetType().GetMethod(message, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    //        //Cache for later
    //        lookup[message] = mtd;
    //    }
    //    //If this message exists		
    //    if (mtd != null) {
    //        //If we haven't already tried to create an action
    //        if (!actionSpecified) {
    //            //Ensure that the message requires no parameters and returns nothing
    //            if (mtd.GetParameters().Length == 0 && mtd.ReturnType == typeof(void)) {
    //                //Create an action delegate for it
    //                var action = (Action)Delegate.CreateDelegate(typeof(Action), this, mtd);
    //                //Cache the delegate
    //                _actions[message] = action;
    //                //Call the function
    //                action();
    //            }
    //            else {
    //                //Otherwise flag that we cannot call this method
    //                _actions[message] = null;
    //            }
    //        }
    //        else
    //            //Otherwise slow invoke the method passing the parameters
    //            mtd.Invoke(this, param);
    //    }
    //}

    #endregion

    #endregion

    /// <summary>
    /// The enter state coroutine.
    /// </summary>
    [HideInInspector]
    protected InterruptableCoroutine enterStateCoroutine;

    /// <summary>
    /// The exit state coroutine.
    /// </summary>
    [HideInInspector]
    protected InterruptableCoroutine exitStateCoroutine;

    /// <summary>
    /// The time that the current state was entered
    /// </summary>
    private float _timeEnteredState;

    /// <summary>
    /// Gets the number of seconds spent in the current state.
    /// </summary>
    public float timeInCurrentState {
        get {
            return Time.time - _timeEnteredState;
        }
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        // I changed the order in which these are called. If left as it was, if ExitState() was IEnumerable
        // along with EnterState(), then the EnterState would be executed before ExitState. I never saw this as
        // a problem as all my ExitState()s return void.
        exitStateCoroutine = new InterruptableCoroutine(this);
        enterStateCoroutine = new InterruptableCoroutine(this);
    }

    #region Default Implementations Of Delegates

    private static IEnumerator DoNothingCoroutine() {
        yield break;
    }

    private static void DoNothing() { }

    private static void DoNothingCollider(Collider other) { }

    private static void DoNothingCollision(Collision other) { }

    ////private static void DoNothingBoolean(bool b) { }

    #endregion

    /// <summary>
    /// Container class that holds the settings associated with a particular state.
    /// </summary>
    public class State {

        public Action DoUpdate = DoNothing;
        [Obsolete]
        public Action DoOccasionalUpdate = DoNothing;
        public Action DoLateUpdate = DoNothing;
        public Action DoFixedUpdate = DoNothing;
        //public Action<Collider> DoOnTriggerEnter = DoNothingCollider;
        //public Action<Collider> DoOnTriggerStay = DoNothingCollider;
        //public Action<Collider> DoOnTriggerExit = DoNothingCollider;
        //public Action<Collision> DoOnCollisionEnter = DoNothingCollision;
        //public Action<Collision> DoOnCollisionStay = DoNothingCollision;
        //public Action<Collision> DoOnCollisionExit = DoNothingCollision;

        public Func<IEnumerator> enterState = DoNothingCoroutine;
        public Func<IEnumerator> exitState = DoNothingCoroutine;
        public IEnumerator enterStateEnumerator = null;
        public IEnumerator exitStateEnumerator = null;

        //public Action<bool> DoOnHover = DoNothingBoolean;
        ///public Action<bool> DoOnPress = DoNothingBoolean;
        //public Action DoOnClick = DoNothing;
        //public Action DoOnDoubleClick = DoNothing;

        public E currentState;

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
    public State state = new State();

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

    public virtual E CurrentState {
        get {
            return state.currentState;
        }
        protected set {
            D.Assert(!state.Equals(value)); // a state object and a state's E CurrentState should never be equal
            ChangingState();
            if (state.currentState.Equals(value)) {
                D.Warn("{0} duplicate state {1} set attempt.", GetType().Name, value);
            }
            state.currentState = value;
            ConfigureCurrentState();
        }
    }

    /// <summary>
    /// The state previous to CurrentState.
    /// WARNING: DONOT CHANGE OBJECT TO E AS MONO COMPILER THROWS UP WITH NO ERROR MESSAGE!
    /// </summary>
    protected object LastState { get; private set; }

    //Stack of the previous running states
    private Stack<State> _stack = new Stack<State>();

    /// <summary>
    /// Call the specified state - activates the new state without deactivating the 
    /// current state.  Called states need to execute Return() when they are finished
    /// </summary>
    /// <param name='stateToActivate'>
    /// State to activate.
    /// </param>
    public virtual void Call(E stateToActivate) {
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
        GetStateMethods();
        if (state.enterState != null) {
            state.enterStateEnumerator = state.enterState();
            enterStateCoroutine.Run(state.enterStateEnumerator);    // must call as null stops any prior IEnumerator still running
        }
    }

    /// <summary>
    /// Return this state from a call
    /// </summary>
    public virtual void Return() {
        if (state.exitState != null) {
            state.exitStateEnumerator = state.exitState();
            exitStateCoroutine.Run(state.exitStateEnumerator);  // must call as null stops any prior IEnumerator still running
        }

        // My ChangingState() addition moved below as CurrentState doesn't change if state calling Return() wasn't Call()ed

        if (_stack.Count > 0) {
            ChangingState();    // my addition to keep lastState in sync
            state = _stack.Pop();
            //D.Log(ShowDebugLog, "{0} setting up resumption of {1}_EnterState() in Return(). MethodName: {2}.", DebugName, CurrentState.ToString(), state.enterState.Method.Name);
            enterStateCoroutine.Run(state.enterStateEnumerator, state.enterStack);
            _timeEnteredState = Time.time - state.time;
        }
        else {
            D.Error("{0} StateMachine: Return() called from state {1} that wasn't Call()ed.", GetType().Name, state.currentState.ToString());
            // UNCLEAR CurrentState remains the same, but it has already run it's ExitState(). What does that mean?
            // Shouldn't ExitState() run only if _stack.Count > 0? -> Return() is ignored if state wasn't Call()ed
        }
    }

    /// <summary>
    /// Return the state from a call with a specified state to 
    /// enter if this state wasn't called
    /// </summary>
    /// <param name='baseState'>
    /// The state to use if there is no waiting calling state
    /// </param>
    public virtual void Return(E baseState) {
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
            D.Warn("{0} StateMachine: Return({1}) called from state {2} that wasn't Call()ed.",
                GetType().Name, baseState.ToString(), state.currentState.ToString());
            CurrentState = baseState;
        }
        _timeEnteredState = Time.time - state.time;
    }

    /// <summary>
    /// Caches previous states.
    /// </summary>
    protected void ChangingState() {
        LastState = state.currentState;
        _timeEnteredState = Time.time;
    }

    /// <summary>
    /// Configures the state machine for the current state.
    /// <remarks>11.17.17 ExitState methods no longer allowed to return IEnumerator as next state's void PreconfigureCurrentState() will
    /// always run before the following state's IEnumerator ExitState(). Error check occurs in ConfigureDelegate().</remarks>
    /// </summary>
    protected void ConfigureCurrentState() {
        if (state.exitState != null) {
            // runs the exitState of the PREVIOUS state as the state delegates haven't been changed yet
            exitStateCoroutine.Run(state.exitState());  // must call as null stops any prior IEnumerator still running
        }

        GetStateMethods();

        if (state.enterState != null) {
            PreconfigureCurrentState();

            state.enterStateEnumerator = state.enterState();    // a void enterState() method executes immediately here rather than wait until the enterCoroutine makes its next pass
            enterStateCoroutine.Run(state.enterStateEnumerator);    // must call as null stops any prior IEnumerator still running
        }
    }

    //Retrieves all of the methods for the current state
    private void GetStateMethods() {
        //Now we need to configure all of the methods
        state.DoUpdate = ConfigureDelegate<Action>("Update", DoNothing);
        //state.DoOccasionalUpdate = ConfigureDelegate<Action>("OccasionalUpdate", DoNothing);
        state.DoLateUpdate = ConfigureDelegate<Action>("LateUpdate", DoNothing);
        state.DoFixedUpdate = ConfigureDelegate<Action>("FixedUpdate", DoNothing);
        //state.DoOnTriggerEnter = ConfigureDelegate<Action<Collider>>("OnTriggerEnter", DoNothingCollider);
        //state.DoOnTriggerExit = ConfigureDelegate<Action<Collider>>("OnTriggerExit", DoNothingCollider);
        //state.DoOnTriggerStay = ConfigureDelegate<Action<Collider>>("OnTriggerStay", DoNothingCollider);
        //state.DoOnCollisionEnter = ConfigureDelegate<Action<Collision>>("OnCollisionEnter", DoNothingCollision);
        //state.DoOnCollisionExit = ConfigureDelegate<Action<Collision>>("OnCollisionExit", DoNothingCollision);
        //state.DoOnCollisionStay = ConfigureDelegate<Action<Collision>>("OnCollisionStay", DoNothingCollision);

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
    /// </summary>
    protected virtual void PreconfigureCurrentState() { }

    /// <summary>
    /// A cache of the delegates for a particular state and method
    /// </summary>
    private Dictionary<object, Dictionary<string, Delegate>> _cache = new Dictionary<object, Dictionary<string, Delegate>>();

    /// <summary>
    /// FInds or creates a delegate for the current state and Method name (aka CurrentState_OnClick), or
    /// if the Method name is not present in this State Machine, then returns Default. Also puts an 
    /// IEnumerator wrapper around EnterState or ExitState methods that return void rather than IEnumerator.
    /// WARNING: BindingFlags.NonPublic DOES NOT find private methods in classes that are a base class of a derived class! 
    /// Do not interpret this to mean a base class of this instance. I mean a base class period. This is noted in GetMethods() 
    /// below, but NOT in the comparable GetMethod() documentation!
    /// <see cref="https://msdn.microsoft.com/en-us/library/4d848zkb(v=vs.110).aspx"/>
    /// </summary>
    /// <typeparam name="R"></typeparam>
    /// <param name="methodRoot">Substring of the methodName that follows "StateName_", e.g. EnterState from State1_EnterState.</param>
    /// <param name="Default">The default delegate to use if a method of the proper name is not found.</param>
    /// <returns></returns>
    private R ConfigureDelegate<R>(string methodRoot, R Default) where R : class {

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
                bool isEnterExitStateMethod = typeof(R) == typeof(Func<IEnumerator>);
                bool isVoidReturnType = mtd.ReturnType != typeof(IEnumerator);
                bool isExitStateMethod = isEnterExitStateMethod && methodRoot == ExitStateText;
                if (isExitStateMethod && !isVoidReturnType) {
                    D.Error("{0} Illegal exit state return type as nextState.PreconfigureState() will always run before lastState.ExitState()!", transform.name);
                }

                if (isEnterExitStateMethod && isVoidReturnType) {
                    // the enter or exit method returns void, so adjust it to execute properly when placed in the IEnumerator delegate
                    Action a = Delegate.CreateDelegate(typeof(Action), this, mtd) as Action;
                    Func<IEnumerator> func = () => { a(); return null; };
                    returnValue = func;
                }
                else {
                    returnValue = Delegate.CreateDelegate(typeof(R), this, mtd);
                }
            }
            else {
                returnValue = Default as Delegate;
                if (methodRoot == EnterStateText || methodRoot == ExitStateText) {
                    D.Warn("{0} did not find method {1}_{2}. Is it a private method in a base class?",
                        transform.name, state.currentState.ToString(), methodRoot);
                }
            }
            lookup[methodRoot] = returnValue;
        }
        return returnValue as R;
    }

    #region Pass On Methods

    protected virtual void Update() {   // GameMgr uses Update to keep GameTime current
        state.DoUpdate();               // MainCameraControl does not. It uses DoUpdate, DoLateUpdate and DoFixedUpdate
    }

    [Obsolete]
    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        state.DoOccasionalUpdate();
    }

    void LateUpdate() {
        state.DoLateUpdate();
    }

    void FixedUpdate() {
        state.DoFixedUpdate();
    }

    //void OnTriggerEnter(Collider other) {
    //    state.DoOnTriggerEnter(other);
    //}

    //void OnTriggerExit(Collider other) {
    //    state.DoOnTriggerExit(other);
    //}

    //void OnTriggerStay(Collider other) {
    //    state.DoOnTriggerStay(other);
    //}

    //void OnCollisionEnter(Collision col) {
    //    state.DoOnCollisionEnter(col);
    //}

    //void OnCollisionExit(Collision col) {
    //    state.DoOnCollisionExit(col);
    //}

    //void OnCollisionStay(Collision col) {
    //    state.DoOnCollisionStay(col);
    //}

    ////void OnHover(bool isOver) {
    ////    state.DoOnHover(isOver);
    ////}

    ////void OnPress(bool isDown) {
    ////    state.DoOnPress(isDown);
    ////}

    ////void OnClick() {
    ////    state.DoOnClick();
    ////}

    ////void OnDoubleClick() {
    ////    state.DoOnDoubleClick();
    ////}

    #endregion

    #region Debug

    #endregion

    #region Nested Classes

    /// <summary>
    /// A coroutine executor that can be interrupted
    /// </summary>
    public class InterruptableCoroutine {

        private IEnumerator _enumerator;
        private MonoBehaviour _behaviour;

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
            _behaviour.StartCoroutine(YieldInstructionCoroutine(ci));
            while (!ci.done)
                yield return null;
        }

        private IEnumerator Run() {
            //Loop forever
            while (true) {
                //Check if we have a current coroutine
                if (_enumerator != null) {
                    //Make a copy of the enumerator in case it changes
                    var enm = _enumerator;
                    //Execute the next step of the coroutine
                    var valid = _enumerator.MoveNext();
                    //See if the enumerator has changed
                    if (enm == _enumerator) {
                        //If this is the same enumerator
                        if (_enumerator != null && valid) {
                            //Get the result of the yield
                            var result = _enumerator.Current;
                            //Check if it is a coroutine
                            if (result is IEnumerator) {
                                //Push the current coroutine and execute the new one
                                _stack.Push(_enumerator);
                                _enumerator = result as IEnumerator;
                                yield return null;
                            }
                            //Check if it is a yield instruction
                            else if (result is YieldInstruction) {
                                //To be able to interrupt yield instructions
                                //we need to run them as a separate coroutine
                                //and wait for them
                                _stack.Push(_enumerator);
                                //Create the coroutine to wait for the yield instruction
                                _enumerator = WaitForCoroutine(result as YieldInstruction);
                                yield return null;
                            }
                            else {
                                //Otherwise return the value
                                yield return _enumerator.Current;
                            }
                        }
                        else {
                            //If the enumerator was set to null then we
                            //need to mark this as invalid
                            valid = false;
                            yield return null;
                        }
                        //Check if we are in a valid state
                        if (!valid) {
                            //If not then see if there are any stacked coroutines
                            if (_stack.Count >= 1) {
                                //Get the stacked coroutine back
                                _enumerator = _stack.Pop();
                            }
                            else {
                                //Ensure we don't use this enumerator again
                                _enumerator = null;
                            }
                        }
                    }
                    else {
                        //If the enumerator changed then just yield
                        yield return null;
                    }
                }
                else {
                    //If the enumerator was null then just yield
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachineBehaviour.InterruptableCoroutine"/> class.
        /// </summary>
        /// <param name='behaviour'>
        /// The behaviour on which the coroutines should run
        /// </param>
        public InterruptableCoroutine(MonoBehaviour behaviour) {
            _behaviour = behaviour;
            _behaviour.StartCoroutine(Run());
        }

        /// <summary>
        /// Stack of executing coroutines
        /// </summary>
        private Stack<IEnumerator> _stack = new Stack<IEnumerator>();

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
        /// Creates a new stack for executing coroutines
        /// </summary>
        /// <returns>
        /// The stack.
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
    }

    #endregion

}

