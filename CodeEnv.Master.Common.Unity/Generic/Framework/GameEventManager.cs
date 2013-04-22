// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameEventManager.cs
// Singleton Event Manager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR
#define DEBUG_LOG

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Singleton Event Manager.
    /// </summary>
    [SerializeAll]
    public sealed class GameEventManager {

        #region SingletonPattern
        private static readonly GameEventManager instance;

        /// <summary>
        /// Explicit static constructor that enables lazy instantiation by telling C# compiler
        /// not to mark type as beforefieldinit.
        /// </summary>
        static GameEventManager() {
            // try, catch and resolve any possible exceptions here
            instance = new GameEventManager();
        }

        /// <summary>
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="GameEventManager"/>.
        /// </summary>
        private GameEventManager() {
            Initialize();
        }

        /// <summary>Returns the singleton instance of this class.</summary>
        public static GameEventManager Instance {
            get { return instance; }
        }
        #endregion

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        private void Initialize() {
            // Add initialization code here if any
        }


        public delegate void EventDelegate<T>(T e) where T : AGameEvent;

        readonly Dictionary<Type, Delegate> listenerDelegates = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Adds the listener's EventHandler method to the list listening for AGameEvent T.
        /// Usage on MonoBehaviours:
        ///     void OnEnable() {
        ///        GameEventManager.instance.AddListener&lt;GameEventSubclass&gt;(EventHandlerMethodName);
        ///     }
        ///     
        ///     void EventHandlerMethodName(GameEventSubclass event) {
        ///         handle event...
        ///     }
        /// </summary>
        /// <typeparam name="T">The Type of AGameEvent.</typeparam>
        /// <param name="listener">The EventDelegate encapsulating the listener's EventHandler method.</param>
        public void AddListener<T>(System.Object source, EventDelegate<T> listener) where T : AGameEvent {
            Delegate delegateWithInvocationList;
            if (listenerDelegates.TryGetValue(typeof(T), out delegateWithInvocationList)) {
                listenerDelegates[typeof(T)] = Delegate.Combine(delegateWithInvocationList, listener);
            }
            else {
                listenerDelegates[typeof(T)] = listener;
            }
            //D.Log("{0} Listener added by {1}.", typeof(T).Name, source.GetType().Name);
            //WriteCompositionToLog<T>(source, listenerDelegates[typeof(T)] as EventDelegate<T>, "Added");
        }

        private void WriteCompositionToLog<T>(System.Object source, EventDelegate<T> currentListeners, string action) where T : AGameEvent {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("EventListener for {0} ", typeof(T).Name);
            sb.AppendFormat("{0} by {1} ", action, source.GetType().Name);
            if (source is IInstanceIdentity) { sb.Append((source as IInstanceIdentity).InstanceID.ToString()); }
            sb.Append(", Current Listeners now include:");
            sb.AppendLine();
            if (currentListeners != null) {
                foreach (var d in currentListeners.GetInvocationList()) {
                    sb.AppendFormat("{0} ", d.Target.GetType().Name);
                    if (d.Target is IInstanceIdentity) { sb.Append((d.Target as IInstanceIdentity).InstanceID.ToString()); }
                    sb.Append(", ");
                }
            }
            sb.AppendLine();
            string result = sb.ToString();
            D.Log(result);
        }


        /// <summary>
        /// Removes the listener's EventHandler method from the list listening for AGameEvent T.
        /// Usage on MonoBehaviours:
        ///     void OnDisable() {
        ///        GameEventManager.instance.RemoveListener&lt;GameEventSubclass&gt;(EventHandlerMethodName);
        ///     }
        ///    
        ///     void EventHandlerMethodName(GameEventSubclass event) {
        ///         handle event...
        ///     }
        /// </summary>
        /// <typeparam name="T">The Type of AGameEvent.</typeparam>
        /// <param name="listener">TThe EventDelegate encapsulating the listener's EventHandler method.</param>
        public void RemoveListener<T>(System.Object source, EventDelegate<T> listener) where T : AGameEvent {
            Delegate delegateWithInvocationList;
            if (listenerDelegates.TryGetValue(typeof(T), out delegateWithInvocationList)) {
                Delegate delegateAfterRemoval = Delegate.Remove(delegateWithInvocationList, listener);

                if (delegateAfterRemoval == null) {
                    // no more currentListeners for this AGameEvent type, so remove the key
                    listenerDelegates.Remove(typeof(T));
                }
                else {
                    listenerDelegates[typeof(T)] = delegateAfterRemoval;
                }
                //D.Log("{0} Listener removed by {1}.", typeof(T).Name, source.GetType().Name);
                Delegate currentListeners;
                if (!listenerDelegates.TryGetValue(typeof(T), out currentListeners)) {
                    currentListeners = null;
                }
                //WriteCompositionToLog<T>(source, currentListeners as EventDelegate<T>, "Removed");
            }
            else {
                D.Log("Attempt to RemoveListener of Type {0} that is not present.", typeof(T));
            }
        }

        public static void RaiseEvent<T>(T gameEvent) where T : AGameEvent {
            Instance.Raise<T>(gameEvent);
        }

        // <summary>
        /// Raises (broadcasts) the provided AGameEvent instance to all currentListeners.
        /// Usage:
        ///     GameEventManager.instance.Raise(new GameEventSubclass());
        /// </summary>
        /// <typeparam name="T">The Type of AGameEvent.</typeparam>
        /// <param name="gameEvent">The instance of AGameEvent to raise.</param>
        public void Raise<T>(T gameEvent) where T : AGameEvent {
            Arguments.ValidateNotNull(gameEvent);

            Delegate delegateWithInvocationList;
            if (listenerDelegates.TryGetValue(typeof(T), out delegateWithInvocationList)) {
                // this assignment to currentListeners is the defensive pattern to guard against multi-threaded race conditions
                EventDelegate<T> callback = delegateWithInvocationList as EventDelegate<T>;
                if (callback != null) {
                    WriteCompositionToLog<T>(gameEvent, callback);
                    callback(gameEvent);
                }
                else {
                    D.Error("Listener for Event of Type {0} is not an EventDelegate!", typeof(T));
                }
            }
            else {
                //D.Warn("Attempt to Raise Event of Type {0} without any Listeners.", typeof(T));
            }
        }

        private void WriteCompositionToLog<T>(T gameEvent, EventDelegate<T> callback) where T : AGameEvent {
            StringBuilder sb = new StringBuilder();
            sb.Append(gameEvent.GetType().Name);
            object source = gameEvent.Source;
            sb.AppendFormat(" Raised from {0} ", source.GetType().Name);
            if (source is IInstanceIdentity) { sb.Append((source as IInstanceIdentity).InstanceID.ToString()); }
            sb.Append(", Targets include:");
            sb.AppendLine();
            foreach (var d in callback.GetInvocationList()) {
                sb.AppendFormat("{0} ", d.Target.GetType().Name);
                if (d.Target is IInstanceIdentity) { sb.Append((d.Target as IInstanceIdentity).InstanceID.ToString()); }
                sb.Append(", ");
            }
            sb.AppendLine();
            sb.Append("Event Contents: ");
            sb.AppendLine();
            sb.Append(gameEvent.ToString());
            string result = sb.ToString();
            D.Log(result);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}


