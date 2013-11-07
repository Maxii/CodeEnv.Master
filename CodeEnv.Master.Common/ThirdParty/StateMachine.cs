// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StateMachine.cs
//  Abstract Base class for non-MonoBehaviour state machines.
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
    using System.Linq;
    using System.Reflection;
    using UnityEngine;

    /// <summary>
    /// Abstract Base class for non-MonoBehaviour state machines.
    /// </summary>
    /// <typeparam name="T">The Type of the States being used, typically an enum type.</typeparam>
    public abstract class StateMachine<T> : StateMachine {

        public T ActiveState {
            get {
                return (T)CurrentState;
            }
            set {
                CurrentState = value;
            }
        }
    }

    /// <summary>
    /// Abstract Base class for non-MonoBehaviour state machines.
    /// </summary>
    public abstract class StateMachine : APropertyChangeTracking {

        /// <summary>
        /// Optimized message broadcasting replacement for SendMessage() that binds the
        /// message to the current state. Essentially calls all methods anywhere that have the 
        /// signature "CurrentState_CallingMethodName(param). Usage:
        ///     void CallingMethodName(param)  { 
        ///         SendStateMessage(param);
        ///     }
        /// </summary>
        /// <param name='param'>
        /// Any parameter passed to the current handler that should be passed on
        /// </param>
        protected void SendStateMessage(params object[] param) {
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
            MethodInfo d = null;
            Dictionary<string, MethodInfo> lookup = null;
            //See if we have scanned this type already
            if (!_messages.TryGetValue(GetType(), out lookup)) {
                //If we haven't then create a lookup for it, this will cache message names to their method info
                lookup = new Dictionary<string, MethodInfo>();
                _messages[GetType()] = lookup;
            }
            //See if we have already search for this message for this type (not instance)
            if (!lookup.TryGetValue(message, out d)) {
                //If we haven't then try to find it
                d = GetType().GetMethod(message, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                //Cache for later
                lookup[message] = d;
            }
            //If this message exists		
            if (d != null) {
                //If we haven't already tried to create an action
                if (!actionSpecified) {
                    //Ensure that the message requires no parameters and returns nothing
                    if (d.GetParameters().Length == 0 && d.ReturnType == typeof(void)) {
                        //Create an action delegate for it
                        var action = (Action)Delegate.CreateDelegate(typeof(Action), this, d);
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
                    d.Invoke(this, param);
            }
        }

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

        public StateMachine() { }

        #region Default Implementations Of Delegates

        private static void DoNothing() { }

        #endregion

        /// <summary>
        /// Container class that holds the settings associated with a particular state.
        /// </summary>
        public class State {

            public object currentState;

            //The amount of time that was spend in this state when pushed to the stack
            public float time;

        }

        /// <summary>
        /// The state this state machine is in
        /// </summary>
        [HideInInspector]
        public State state = new State();

        /// <summary>
        /// Gets or sets the current state
        /// </summary>
        /// <value>
        /// The state to use
        /// </value>
        public object CurrentState {
            get {
                return state.currentState;
            }
            set {
                SetProperty<object>(ref state.currentState, value, "currentState", OnCurrentStateChanged, OnCurrentStateChanging);
            }
        }

        private void OnCurrentStateChanged() {
            ConfigureCurrentState();
        }

        private void OnCurrentStateChanging(object newState) {
            ChangingState();
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
        public void Call(object stateToActivate) {
            state.time = timeInCurrentState;
            ChangingState();

            _stack.Push(state);
            state = new State();
            state.currentState = stateToActivate;

            ConfigureCurrentStateForCall();
        }

        //Configures the state machine when the new state has been called
        private void ConfigureCurrentStateForCall() {
            GetStateMethods();
        }

        /// <summary>
        /// Return this state from a call
        /// </summary>
        public void Return() {
            if (_stack.Count > 0) {
                state = _stack.Pop();
                _timeEnteredState = Time.time - state.time;
            }
        }

        /// <summary>
        /// Return from the current state with a specific state to 
        /// enter in case this state wasn't entered via Call(state).
        /// </summary>
        /// <param name='baseState'>
        /// The state to use if there is no waiting state that called this state.
        /// </param>
        public void Return(object baseState) {
            if (_stack.Count > 0) {
                state = _stack.Pop();
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
            GetStateMethods();
        }

        //Retrieves all of the methods for the current state
        private void GetStateMethods() {
            UnwireEvents();
            WireEvents();
        }

        /// <summary>
        /// A cache of the delegates for a particular state and method
        /// </summary>
        private Dictionary<object, Dictionary<string, Delegate>> _cache = new Dictionary<object, Dictionary<string, Delegate>>();

        //Creates a delegate for a particular method on the current state machine
        //if a suitable method is not found then the default is used instead
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
        /// Automatically finds (thru reflection) and caches any event handlers present on this state machine type for the specific state (or all states using All_On) we are in. 
        /// The event handlers will be of the form STATENAME_OnEVENTNAME_SELECTOR where SELECTOR is typically the NAME of the gameObject containing the state
        /// machine. GameObjects with Tags can also be designated for the search. 
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
                    .Concat(type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
                            .Where(m => m.Name.StartsWith("All_On")))
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
                        .Where(e => e.@event != null);
                foreach (var source in sources) {
                    var dlg = Delegate.CreateDelegate(source.@event.EventHandlerType, this, evt.method);
                    if (dlg != null) {
                        source.@event.AddEventHandler(source.source, dlg);
                        var we = new WiredEvent { dlg = dlg, evt = source.@event, source = source.source };
                        D.Log("Event wired for {0}: name = {1}, selector = {2}, sourceType = {3}.", cs, evt.eventName, evt.selector, we.source.GetType().Name);
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
            if (eventInfo.selector.StartsWith("Tag_")) {
                objects.AddRange(GameObject.FindGameObjectsWithTag(eventInfo.selector.Substring(4)));
                return objects;
            }

            objects.Add(GameObject.Find(eventInfo.selector));
            if (objects.Count > 0) {
                D.Log("Slow GameObject.Find(Selector) was used to find target gameobjects for search.");
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
}

