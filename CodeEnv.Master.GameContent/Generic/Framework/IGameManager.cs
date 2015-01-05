// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGameManager.cs
// Interface allowing access to the associated Unity-compiled script. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface allowing access to the associated Unity-compiled script. 
    /// Typically, a static reference to the script is established by GameManager in References.cs, providing access to the script from classes located in pre-compiled assemblies.
    /// </summary>
    public interface IGameManager : INotifyPropertyChanged, INotifyPropertyChanging, IChangeTracking {

        /// <summary>
        /// Fires when GameState changes to Running, then clears all subscribers.
        /// WARNING: This event will fire each time the GameState changes to Running, 
        /// but as it clears its subscribers each time, clients will need to resubscribe if
        /// they want to receive the event again. Clients which persist across scene changes
        /// should pay particular attention as they won't automatically resubscribe since Awake
        /// (or a Constructor) is only called once in the life of the client.
        /// </summary>
        /// <remarks>
        /// Current clients SelectionManager, AGuiEnumSliderBase and DebugHud have been checked.
        /// </remarks>
        event Action onIsRunningOneShot;

        event Action<GameState> onCurrentStateChanging;

        event Action onCurrentStateChanged;

        /// <summary>
        /// Occurs just before a scene starts loading.
        /// Note: Event is not fired when the first scene is about to start loading as a result of the Application starting.
        /// </summary>
        event Action<SceneLevel> onSceneLoading;

        /// <summary>
        /// Occurs just after a scene finishes loading, aka immediately after OnLevelWasLoaded is received.
        /// Note: Event is not fired when the first scene is loaded as a result of the Application starting.
        /// </summary>
        event Action onSceneLoaded;

        event Action onNewGameBuilding;

        bool IsPaused { get; }

        Player HumanPlayer { get; }

        IList<Player> AIPlayers { get; }

        GameState CurrentState { get; }

        GameSettings GameSettings { get; }

        void RecordGameStateProgressionReadiness(MonoBehaviour source, GameState maxGameStateUntilReady, bool isReady);
    }
}

