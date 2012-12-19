// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EventManager.cs
// SingletonPattern. COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Resources;

    /// <summary>
    /// Derive GameEventSubclass[es] from GameEvent that carry any parameters that
    /// objects listening for the event might need to process it.
    /// </summary>
    public class GameEvent { }

    /// <summary>
    /// Singleton Event Manager.
    /// </summary>
    public sealed class EventManager {

        #region SingletonPattern
        private static readonly EventManager _instance;

        /// <summary>
        /// Explicit static constructor that enables lazy instantiation by telling C# compiler
        /// not to mark type as beforefieldinit.
        /// </summary>
        static EventManager() {
            // try, catch and resolve any possible exceptions here
            _instance = new EventManager();
        }

        /// <summary>
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="EventManager"/>.
        /// </summary>
        private EventManager() {
            Initialize();
        }

        /// <summary>Returns the singleton instance of this class.</summary>
        public static EventManager Instance {
            get { return _instance; }
        }
        #endregion

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        private void Initialize() {
            // Add initialization code here if any
        }


        public delegate void EventDelegate<T>(T e) where T : GameEvent;

        readonly Dictionary<Type, Delegate> listenerDelegates = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Adds the listener's EventHandler method to the list listening for GameEvent T.
        /// Usage on MonoBehaviours:
        ///     void OnEnable() {
        ///        EventManager.instance.AddListener&ltGameEventSubclass&gt(EventHandlerMethodName);
        ///     }
        ///     
        ///     void EventHandlerMethodName(GameEventSubclass event) {
        ///         handle event...
        ///     }
        /// </summary>
        /// <typeparam name="T">The Type of GameEvent.</typeparam>
        /// <param name="listener">The EventDelegate encapsulating the listener's EventHandler method.</param>
        public void AddListener<T>(EventDelegate<T> listener) where T : GameEvent {
            Delegate d;
            if (listenerDelegates.TryGetValue(typeof(T), out d)) {
                listenerDelegates[typeof(T)] = Delegate.Combine(d, listener);
            }
            else {
                listenerDelegates[typeof(T)] = listener;
            }
        }

        /// <summary>
        /// Removes the listener's EventHandler method from the list listening for GameEvent T.
        /// Usage on MonoBehaviours:
        ///     void OnDisable() {
        ///        EventManager.instance.RemoveListener&ltGameEventSubclass&gt(EventHandlerMethodName);
        ///     }
        ///    
        ///     void EventHandlerMethodName(GameEventSubclass event) {
        ///         handle event...
        ///     }
        /// </summary>
        /// <typeparam name="T">The Type of GameEvent.</typeparam>
        /// <param name="listener">TThe EventDelegate encapsulating the listener's EventHandler method.</param>
        public void RemoveListener<T>(EventDelegate<T> listener) where T : GameEvent {
            Delegate d;
            if (listenerDelegates.TryGetValue(typeof(T), out d)) {
                Delegate currentDel = Delegate.Remove(d, listener);

                if (currentDel == null) {
                    listenerDelegates.Remove(typeof(T));
                }
                else {
                    listenerDelegates[typeof(T)] = currentDel;
                }
            }
        }

        /// <summary>
        /// Raises (broadcasts) the provided GameEvent instance to all listeners.
        /// Usage:
        ///     EventManager.instance.Raise(new GameEventSubclass());
        /// </summary>
        /// <typeparam name="T">The Type of GameEvent.</typeparam>
        /// <param name="e">The instance of GameEvent to raise.</param>
        public void Raise<T>(T e) where T : GameEvent {
            Arguments.ValidateNotNull(e);

            Delegate d;
            if (listenerDelegates.TryGetValue(typeof(T), out d)) {
                EventDelegate<T> callback = d as EventDelegate<T>;
                if (callback != null) {
                    callback(e);
                }
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}


