// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AStateMachine.cs
// Abstract Base class for a State Machine implemented as a component of a MonoBehaviour.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using UnityEngine;

    /// <summary>
    ///  Abstract Base class for a State Machine implemented as a component of a MonoBehaviour.
    /// </summary>
    /// <typeparam name="E">Th State Type being used, typically an enum type.</typeparam>
    public abstract class AStateMachine<E> : APropertyChangeTracking where E : struct {

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
                                    //Create the coroutine to wait for the yieldinstruction
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


        #region RelayToCurrentState

        /// <summary>
        /// Optimized messaging replacement for SendMessage() that binds the
        /// message to the current state. Essentially calls the method on this MonoBehaviour
        /// instance that has the signature "CurrentState_CallingMethodName(param). 
        /// Usage:
        ///     void CallingMethodName(param)  { 
        ///         SendStateMessage(param);
        ///     }
        /// </summary>
        /// <param name='param'>
        /// Any parameter passed to the current handler that should be passed on
        /// </param>
        protected void RelayToCurrentState(params object[] param) {
            var message = CurrentState.ToString() + "_" + (new StackFrame(1)).GetMethod().Name;
            SendMessageEx(message, param);
        }

        private static Dictionary<Type, Dictionary<string, MethodInfo>> _messages = new Dictionary<Type, Dictionary<string, MethodInfo>>();
        private Dictionary<string, Action> _actions = new Dictionary<string, Action>();

        /// <summary>
        /// Optimized SendMessage replacement.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="param">The parameter.</param>
        private void SendMessageEx(string message, object[] param) {
            //Have we found that a delegate was already created
            var actionSpecified = false;
            //Try to get an Action delegate for the message
            Action a = null;
            //Try to uncache a delegate
            if (_actions.TryGetValue(message, out a)) {
                //If we got one then call it
                actionSpecified = true;
                //a will be null if we previously tried to get an action and failed
                if (a != null) {
                    a();
                    return;
                }
            }

            //Otherwise try to get the method for the name
            MethodInfo mtd = null;
            Dictionary<string, MethodInfo> lookup = null;
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
            //If this message exists		
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
                        //Otherwise flag that we cannot call this method
                        _actions[message] = null;
                    }
                }
                else
                    //Otherwise slow invoke the method passing the parameters
                    mtd.Invoke(this, param);
            }
        }

        #endregion

        /// <summary>
        /// The enter state coroutine.
        /// </summary>
        protected InterruptableCoroutine enterStateCoroutine;

        /// <summary>
        /// The exit state coroutine.
        /// </summary>
        protected InterruptableCoroutine exitStateCoroutine;

        /// <summary>
        /// The time that the current state was entered
        /// </summary>
        private float _timeEnteredState;

        /// <summary>
        /// Gets the number of seconds spent in the current state
        /// </summary>
        public float timeInCurrentState {
            get {
                return Time.time - _timeEnteredState;
            }
        }

        private MonoBehaviour _behaviour;

        /// <summary>
        /// Initializes a new instance of the <see cref="AStateMachine{E}"/> class.
        /// </summary>
        /// <param name="behaviour">The MonoBehaviour this state machine is attached too.</param>
        public AStateMachine(MonoBehaviour behaviour) {
            _behaviour = behaviour;
            //Create the interruptable coroutines
            enterStateCoroutine = new InterruptableCoroutine(behaviour);
            exitStateCoroutine = new InterruptableCoroutine(behaviour);
        }


        #region Default Implementations Of Delegates

        protected static IEnumerator DoNothingCoroutine() {
            yield break;
        }

        protected static void DoNothing() { }

        protected static void DoNothingCollider(Collider other) { }

        protected static void DoNothingCollision(Collision other) { }

        protected static void DoNothingBoolean(bool b) { }

        #endregion

        /// <summary>
        /// Container class that holds the settings associated with a particular state.
        /// </summary>
        public class State {

            public Action DoUpdate = DoNothing;
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

            public Action<bool> DoOnHover = DoNothingBoolean;
            public Action<bool> DoOnPress = DoNothingBoolean;
            public Action DoOnClick = DoNothing;
            public Action DoOnDoubleClick = DoNothing;

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
        public State state = new State();

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
        public E CurrentState {
            get { return state.currentState; }
            set { SetProperty<E>(ref state.currentState, value, "CurrentState", OnCurrentStateChanged, OnCurrentStateChanging); }
        }

        protected virtual void OnCurrentStateChanging(E incomingState) {
            ChangingState();
        }

        protected virtual void OnCurrentStateChanged() {
            D.Log("{0} {1} changed to {2}.", _behaviour.gameObject.name, typeof(E), CurrentState);
            ConfigureCurrentState();
        }

        /// <summary>
        /// The last state.
        /// </summary>
        public object lastState;

        //Stack of the previous running states
        private Stack<State> _stack = new Stack<State>();

        /// <summary>
        /// Call the specified state - activates the new state without deactivating the 
        /// current state.  Called states need to execute Return() when they are finished
        /// </summary>
        /// <param name='stateToActivate'>State to activate.</param>
        public void Call(E stateToActivate) {
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
                enterStateCoroutine.Run(state.enterStateEnumerator);
            }
        }

        /// <summary>
        /// Return this state from a call
        /// </summary>
        public void Return() {
            if (state.exitState != null) {
                state.exitStateEnumerator = state.exitState();
                exitStateCoroutine.Run(state.exitStateEnumerator);
            }
            if (_stack.Count > 0) {
                state = _stack.Pop();
                enterStateCoroutine.Run(state.enterStateEnumerator, state.enterStack);
                _timeEnteredState = Time.time - state.time;
            }
        }

        /// <summary>
        /// Return the state from a call with a specified state to 
        /// enter if this state wasn't called
        /// </summary>
        /// <param name='baseState'>
        /// The state to use if there is no waiting calling state
        /// </param>
        public void Return(E baseState) {
            if (state.exitState != null) {
                state.exitStateEnumerator = state.exitState();
                exitStateCoroutine.Run(state.exitStateEnumerator);

            }
            if (_stack.Count > 0) {
                state = _stack.Pop();
                enterStateCoroutine.Run(state.enterStateEnumerator, state.enterStack);
            }
            else {
                CurrentState = baseState;
            }
            _timeEnteredState = Time.time - state.time;
        }

        private void ChangingState() {
            lastState = state.currentState;
            _timeEnteredState = Time.time;
        }

        /// <summary>
        /// Configures the state machine for the current state
        /// </summary>
        private void ConfigureCurrentState() {
            if (state.exitState != null) {
                // runs the exitState of the PREVIOUS state as the state delegates haven't been changed yet
                exitStateCoroutine.Run(state.exitState());
            }

            GetStateMethods();

            if (state.enterState != null) {
                state.enterStateEnumerator = state.enterState();
                enterStateCoroutine.Run(state.enterStateEnumerator);
            }
        }

        //Retrieves all of the methods for the current state
        private void GetStateMethods() {
            //Now we need to configure all of the methods
            state.DoUpdate = ConfigureDelegate<Action>("Update", DoNothing);
            state.DoOccasionalUpdate = ConfigureDelegate<Action>("OccasionalUpdate", DoNothing);
            state.DoLateUpdate = ConfigureDelegate<Action>("LateUpdate", DoNothing);
            state.DoFixedUpdate = ConfigureDelegate<Action>("FixedUpdate", DoNothing);
            state.DoOnTriggerEnter = ConfigureDelegate<Action<Collider>>("OnTriggerEnter", DoNothingCollider);
            state.DoOnTriggerExit = ConfigureDelegate<Action<Collider>>("OnTriggerExit", DoNothingCollider);
            state.DoOnTriggerStay = ConfigureDelegate<Action<Collider>>("OnTriggerEnter", DoNothingCollider);
            state.DoOnCollisionEnter = ConfigureDelegate<Action<Collision>>("OnCollisionEnter", DoNothingCollision);
            state.DoOnCollisionExit = ConfigureDelegate<Action<Collision>>("OnCollisionExit", DoNothingCollision);
            state.DoOnCollisionStay = ConfigureDelegate<Action<Collision>>("OnCollisionStay", DoNothingCollision);

            state.DoOnHover = ConfigureDelegate<Action<bool>>("OnHover", DoNothingBoolean);
            state.DoOnPress = ConfigureDelegate<Action<bool>>("OnPress", DoNothingBoolean);
            state.DoOnClick = ConfigureDelegate<Action>("OnClick", DoNothing);
            state.DoOnDoubleClick = ConfigureDelegate<Action>("OnDoubleClick", DoNothing);

            state.enterState = ConfigureDelegate<Func<IEnumerator>>("EnterState", DoNothingCoroutine);
            state.exitState = ConfigureDelegate<Func<IEnumerator>>("ExitState", DoNothingCoroutine);
        }

        /// <summary>
        /// A cache of the delegates for a particular state and method
        /// </summary>
        private Dictionary<object, Dictionary<string, Delegate>> _cache = new Dictionary<object, Dictionary<string, Delegate>>();

        /// <summary>
        /// FInds or creates a delegate for the current state and Method name (aka CurrentState_OnClick), or
        /// if the Method name is not present in this State Machine, then returns Default. Also puts an 
        /// IEnumerator wrapper around EnterState or ExitState methods that return void rather than
        /// IEnumerator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="methodRoot">Substring of the methodName that follows "StateName_", eg EnterState from State1_EnterState.</param>
        /// <param name="Default">The default delegate to use if a method of the proper name is not found.</param>
        /// <returns></returns>
        private T ConfigureDelegate<T>(string methodRoot, T Default) where T : class {

            Dictionary<string, Delegate> lookup;
            if (!_cache.TryGetValue(state.currentState, out lookup)) {
                _cache[state.currentState] = lookup = new Dictionary<string, Delegate>();
            }
            Delegate returnValue;
            if (!lookup.TryGetValue(methodRoot, out returnValue)) {

                var mtd = GetType().GetMethod(state.currentState.ToString() + "_" + methodRoot, System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod);

                if (mtd != null) {
                    if (typeof(T) == typeof(Func<IEnumerator>) && mtd.ReturnType != typeof(IEnumerator)) {
                        Action a = Delegate.CreateDelegate(typeof(Action), this, mtd) as Action;
                        Func<IEnumerator> func = () => { a(); return null; };
                        returnValue = func;
                    }
                    else
                        returnValue = Delegate.CreateDelegate(typeof(T), this, mtd);
                }
                else {
                    returnValue = Default as Delegate;
                }
                lookup[methodRoot] = returnValue;
            }
            return returnValue as T;
        }

        #region Pass On Methods

        // These public methods must be called by the client
        // behaviour if the derived state machine implements
        // states that use them

        public void Update() {
            state.DoUpdate();
        }

        public void OccasionalUpdate() {
            state.DoOccasionalUpdate();
        }

        public void LateUpdate() {
            state.DoLateUpdate();
        }

        public void FixedUpdate() {
            state.DoFixedUpdate();
        }

        public void OnTriggerEnter(Collider other) {
            state.DoOnTriggerEnter(other);
        }

        public void OnTriggerExit(Collider other) {
            state.DoOnTriggerExit(other);
        }

        public void OnTriggerStay(Collider other) {
            state.DoOnTriggerStay(other);
        }

        public void OnCollisionEnter(Collision other) {
            state.DoOnCollisionEnter(other);
        }

        public void OnCollisionExit(Collision other) {
            state.DoOnCollisionExit(other);
        }

        public void OnCollisionStay(Collision other) {
            state.DoOnCollisionStay(other);
        }

        public void OnHover(bool isOver) {
            state.DoOnHover(isOver);
        }

        public void OnPress(bool isDown) {
            state.DoOnPress(isDown);
        }

        public void OnClick() {
            state.DoOnClick();
        }

        public void OnDoubleClick() {
            state.DoOnDoubleClick();
        }

        #endregion


    }
}

