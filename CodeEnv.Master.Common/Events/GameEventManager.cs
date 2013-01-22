// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameEventManager.cs
// SingletonPattern. COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common.Resources;
    using UnityEngine;

    /// <summary>
    /// Derive GameEventSubclass[es] from GameEvent that carry any parameters that
    /// objects listening for the event might need to process it.
    /// </summary>
    public class GameEvent { }

    /// <summary>
    /// Singleton Event Manager.
    /// </summary>
    public sealed class GameEventManager {

        #region SingletonPattern
        private static readonly GameEventManager _instance;

        /// <summary>
        /// Explicit static constructor that enables lazy instantiation by telling C# compiler
        /// not to mark type as beforefieldinit.
        /// </summary>
        static GameEventManager() {
            // try, catch and resolve any possible exceptions here
            _instance = new GameEventManager();
        }

        /// <summary>
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="GameEventManager"/>.
        /// </summary>
        private GameEventManager() {
            Initialize();
        }

        /// <summary>Returns the singleton instance of this class.</summary>
        public static GameEventManager Instance {
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
        ///        GameEventManager.instance.AddListener&ltGameEventSubclass&gt(EventHandlerMethodName);
        ///     }
        ///     
        ///     void EventHandlerMethodName(GameEventSubclass event) {
        ///         handle event...
        ///     }
        /// </summary>
        /// <typeparam name="T">The Type of GameEvent.</typeparam>
        /// <param name="listener">The EventDelegate encapsulating the listener's EventHandler method.</param>
        public void AddListener<T>(EventDelegate<T> listener) where T : GameEvent {
            Delegate delegateWithInvocationList;
            if (listenerDelegates.TryGetValue(typeof(T), out delegateWithInvocationList)) {
                listenerDelegates[typeof(T)] = Delegate.Combine(delegateWithInvocationList, listener);
            }
            else {
                listenerDelegates[typeof(T)] = listener;
            }
            Debug.Log("Event Listener  for Type {0} added.".Inject(typeof(T)));
        }

        /// <summary>
        /// Removes the listener's EventHandler method from the list listening for GameEvent T.
        /// Usage on MonoBehaviours:
        ///     void OnDisable() {
        ///        GameEventManager.instance.RemoveListener&ltGameEventSubclass&gt(EventHandlerMethodName);
        ///     }
        ///    
        ///     void EventHandlerMethodName(GameEventSubclass event) {
        ///         handle event...
        ///     }
        /// </summary>
        /// <typeparam name="T">The Type of GameEvent.</typeparam>
        /// <param name="listener">TThe EventDelegate encapsulating the listener's EventHandler method.</param>
        public void RemoveListener<T>(EventDelegate<T> listener) where T : GameEvent {
            Delegate delegateWithInvocationList;
            if (listenerDelegates.TryGetValue(typeof(T), out delegateWithInvocationList)) {
                Delegate delegateAfterRemoval = Delegate.Remove(delegateWithInvocationList, listener);

                if (delegateAfterRemoval == null) {
                    // no more listeners for this GameEvent type, so remove the key
                    listenerDelegates.Remove(typeof(T));
                }
                else {
                    listenerDelegates[typeof(T)] = delegateAfterRemoval;
                }
                Debug.Log("Event Listener for Type {0} removed.".Inject(typeof(T)));
            }
        }

        /// <summary>
        /// Raises (broadcasts) the provided GameEvent instance to all listeners.
        /// Usage:
        ///     GameEventManager.instance.Raise(new GameEventSubclass());
        /// </summary>
        /// <typeparam name="T">The Type of GameEvent.</typeparam>
        /// <param name="gameEvent">The instance of GameEvent to raise.</param>
        public void Raise<T>(T gameEvent) where T : GameEvent {
            Arguments.ValidateNotNull(gameEvent);

            Delegate delegateWithInvocationList;
            if (listenerDelegates.TryGetValue(typeof(T), out delegateWithInvocationList)) {
                EventDelegate<T> callback = delegateWithInvocationList as EventDelegate<T>;
                if (callback != null) {
                    callback(gameEvent);
                    Debug.Log("Event Raised for Type {0}.".Inject(typeof(T)));
                }
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}


