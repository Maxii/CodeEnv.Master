// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMonoStateMachine.cs
//  Abstract Base class for MonoBehaviour State Machines to inherit from.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
///  Abstract Base class for MonoBehaviour State Machines to inherit from.
/// </summary>
/// <typeparam name="E">Th State Type being used, typically an enum type.</typeparam>
public abstract class AMonoStateMachine<E> : AMonoBase where E : struct {

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

    //Holds a cache of whether a message is available on a type
    private static Dictionary<Type, Dictionary<string, MethodInfo>> _messages = new Dictionary<Type, Dictionary<string, MethodInfo>>();
    //Holds a local cache of Action delegates where the method is an action
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
    /// Gets the amount of time spent in the current state
    /// </summary>
    /// <value>
    /// The number of seconds in the current state
    /// </value>
    public float timeInCurrentState {
        get {
            return Time.time - _timeEnteredState;
        }
    }

    protected override void Awake() {
        base.Awake();
        //Create the interruptable coroutines
        enterStateCoroutine = new InterruptableCoroutine(this);
        exitStateCoroutine = new InterruptableCoroutine(this);
    }

    #region Default Implementations Of Delegates

    private static IEnumerator DoNothingCoroutine() {
        yield break;
    }

    private static void DoNothing() { }

    private static void DoNothingCollider(Collider other) { }

    private static void DoNothingCollision(Collision other) { }

    private static void DoNothingBoolean(bool b) { }

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
    [HideInInspector]
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
        protected set { SetProperty<E>(ref state.currentState, value, "CurrentState", OnCurrentStateChanged, OnCurrentStateChanging); }
    }

    protected virtual void OnCurrentStateChanging(E incomingState) {
        ChangingState();
    }

    protected virtual void OnCurrentStateChanged() {
        ConfigureCurrentState();
    }

    [HideInInspector]
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
    /// <param name='stateToActivate'>
    /// State to activate.
    /// </param>
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
        //UnwireEvents();
        if (_stack.Count > 0) {
            state = _stack.Pop();
            enterStateCoroutine.Run(state.enterStateEnumerator, state.enterStack);
            //WireEvents();
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
        //UnwireEvents();
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
        //WireEvents();
        _timeEnteredState = Time.time - state.time;
    }

    /// <summary>
    /// Caches previous states
    /// </summary>
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
        //UnwireEvents();
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

        //WireEvents();
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
    /// <typeparam name="D"></typeparam>
    /// <param name="methodRoot">Substring of the methodName that follows "StateName_", eg EnterState from State1_EnterState.</param>
    /// <param name="Default">The default delegate to use if a method of the proper name is not found.</param>
    /// <returns></returns>
    private D ConfigureDelegate<D>(string methodRoot, D Default) where D : class {

        Dictionary<string, Delegate> lookup;
        if (!_cache.TryGetValue(state.currentState, out lookup)) {
            _cache[state.currentState] = lookup = new Dictionary<string, Delegate>();
        }
        Delegate returnValue;
        if (!lookup.TryGetValue(methodRoot, out returnValue)) {

            var mtd = GetType().GetMethod(state.currentState.ToString() + "_" + methodRoot, System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod);

            if (mtd != null) {
                if (typeof(D) == typeof(Func<IEnumerator>) && mtd.ReturnType != typeof(IEnumerator)) {
                    Action a = Delegate.CreateDelegate(typeof(Action), this, mtd) as Action;
                    Func<IEnumerator> func = () => { a(); return null; };
                    returnValue = func;
                }
                else
                    returnValue = Delegate.CreateDelegate(typeof(D), this, mtd);
            }
            else {
                returnValue = Default as Delegate;
            }
            lookup[methodRoot] = returnValue;
        }
        return returnValue as D;

    }

    #region Pass On Methods

    protected override void Update() {
        base.Update();
        state.DoUpdate();
    }

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

    void OnTriggerEnter(Collider other) {
        state.DoOnTriggerEnter(other);
    }

    void OnTriggerExit(Collider other) {
        state.DoOnTriggerExit(other);
    }

    void OnTriggerStay(Collider other) {
        state.DoOnTriggerStay(other);
    }

    void OnCollisionEnter(Collision other) {
        state.DoOnCollisionEnter(other);
    }

    void OnCollisionExit(Collision other) {
        state.DoOnCollisionExit(other);
    }

    void OnCollisionStay(Collision other) {
        state.DoOnCollisionStay(other);
    }

    void OnHover(bool isOver) {
        state.DoOnHover(isOver);
    }

    void OnPress(bool isDown) {
        state.DoOnPress(isDown);
    }

    void OnClick() {
        state.DoOnClick();
    }

    void OnDoubleClick() {
        state.DoOnDoubleClick();
    }

    #endregion

    #region Event Wiring

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

    public class EventDef {
        public string eventName;
        public string selector;
        public MethodInfo method;
    }

    private class WiredEvent {
        public System.Reflection.EventInfo evt;
        public Delegate dlg;
        public object source;
    }

    private List<WiredEvent> _wiredEvents = new List<WiredEvent>();

    private static Dictionary<Type, Dictionary<string, EventDef[]>> _cachedEvents = new Dictionary<Type, Dictionary<string, EventDef[]>>();

    /// <summary>
    /// Automatically finds (thru reflection) and caches any event handlers present on this state machine type for the specific state we are in. The event
    /// handlers will be of the form STATENAME_OnEVENTNAME_SELECTOR where SELECTOR is typically the NAME of the gameObject containing the state
    /// machine. Child gameObjects and GameObjects with Tags can also be designated for the search. 
    /// </summary>
    private void WireEvents() {
        var cs = CurrentState.ToString();
        var type = GetType();
        EventDef[] events;
        Dictionary<string, EventDef[]> lookup;

        if (!_cachedEvents.TryGetValue(type, out lookup)) {
            lookup = new Dictionary<string, EventDef[]>();
            _cachedEvents[type] = lookup;
        }
        if (!lookup.TryGetValue(cs, out events)) {
            var len = CurrentState.ToString().Length + 3;
            events = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
                .Where(m => m.Name.StartsWith(CurrentState.ToString() + "_On"))
                .Select(m => new { name = m.Name.Substring(len), method = m })
                .Where(n => n.name.IndexOf("_") > 1)
                .Select(n => new EventDef {
                    eventName = n.name.Substring(0, n.name.IndexOf("_")),
                    selector = n.name.Substring(n.name.IndexOf("_") + 1),
                    method = n.method
                })
                .ToArray();
            lookup[cs] = events;
        }

        foreach (var evt in events) {
            var list = OnWire(evt);
            list.AddRange(list.ToList().Where(l => l is Component).SelectMany(l => ((Component)l).GetComponents<MonoBehaviour>()).Cast<object>());
            list.AddRange(list.ToList().Where(l => l is GameObject).SelectMany(l => ((GameObject)l).GetComponents<MonoBehaviour>()).Cast<object>());
            var sources =
                list.Select(l => new { @event = l.GetType().GetEvent(evt.eventName), source = l })
                    .Where(e => e.@event != null);  // to qualify as a source, you must have declared an event called eventName
            foreach (var source in sources) {
                var dlg = Delegate.CreateDelegate(source.@event.EventHandlerType, this, evt.method);
                if (dlg != null) {
                    source.@event.AddEventHandler(source.source, dlg);
                    var we = new WiredEvent { dlg = dlg, evt = source.@event, source = source.source };
                    D.Log("Event wired for {0}: name = {1}, selector = {2}, sourceType = {3}.", cs, evt.eventName, evt.selector, we.source.GetType().Name);
                    D.Log("EventHandler name = {0}.", dlg.Method.Name);
                    _wiredEvents.Add(we);
                }
            }
        }
    }

    public void RefreshEvents() {
        UnwireEvents();
        WireEvents();
    }

    private void UnwireEvents() {
        foreach (var evt in _wiredEvents) {
            evt.evt.RemoveEventHandler(evt.source, evt.dlg);
        }
    }

    /// <summary>
    ///Returns a list of Components and GameObjects that have handlers compatible with the event defined by eventInfo.
    /// </summary>
    /// <param name="eventInfo">The event information.</param>
    /// <returns></returns>
    private List<object> OnWire(EventDef eventInfo) {
        List<object> objects = new List<object>();
        var extra = OnWireEvent(eventInfo);
        if (extra != null) {
            objects.AddRange(extra);
            if (objects.Count > 0) {
                D.Log("Fast OnWireEvent was the source of target gameobjects for search.");
                return objects;
            }
        }
        if (eventInfo.selector.StartsWith("Child_")) {
            var cs = eventInfo.selector.Substring(6).Replace("_", "/");
            objects.Add(transform.Find(cs));
            return objects;
        }
        if (eventInfo.selector.StartsWith("Tag_")) {
            objects.AddRange(GameObject.FindGameObjectsWithTag(eventInfo.selector.Substring(4)));
            return objects;
        }

        objects.Add(GameObject.Find(eventInfo.selector));
        if (objects.Count > 0) {
            D.Log("Slow GameObject.Find(Selector) was used to find target gameobject for search.");
        }
        return objects;
    }

    /// <summary>
    /// Hook for supplying your list of components and game objects holding MonoBehaviours 
    /// containing event handlers for this event. If utilized, there will be no other search 
    /// process pursued as it is presumed you have provided all the gameobjects that
    /// have the appropriate handlers.
    /// </summary>
    /// <param name="eventInfo">The event definition.</param>
    /// <returns></returns>
    protected virtual IEnumerable<object> OnWireEvent(EventDef eventInfo) {
        return null;
    }

    #endregion

}

