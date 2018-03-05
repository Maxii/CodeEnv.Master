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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using CodeEnv.Master.Common;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Interface allowing access to the associated Unity-compiled script. 
    /// Typically, a static reference to the script is established by GameManager in References.cs, providing access to the script from classes located in pre-compiled assemblies.
    /// </summary>
    public interface IGameManager : INotifyPropertyChanged, INotifyPropertyChanging, IChangeTracking {

        /// <summary>
        /// Fires when GameState changes to Running and is ready to play, then clears all subscribers.
        /// WARNING: This event will fire each time the GameState changes to Running, 
        /// but as it clears its subscribers each time, clients will need to resubscribe if
        /// they want to receive the event again. Clients which persist across scene changes
        /// should pay particular attention as they won't automatically resubscribe since Awake
        /// (or a Constructor) is only called once in the life of the client.
        /// </summary>
        /// <remarks>
        /// Current clients AGuiEnumSliderBase, FPSReadout and GameTime as of 10.14.16.
        /// </remarks>
        event EventHandler isReadyForPlayOneShot;

        event EventHandler isPausedChanged;

        /// <summary>
        /// Occurs when GameState is about to change. 
        /// </summary>
        event EventHandler gameStateChanging;

        /// <summary>
        /// Occurs when the GameState has just changed. 
        /// </summary>
        event EventHandler gameStateChanged;

        /// <summary>
        /// Occurs just before a scene starts loading.
        /// Note: Event is not fired when the first scene is about to start loading as a result of the Application starting.
        /// </summary>
        event EventHandler sceneLoading;

        /// <summary>
        /// Occurs just after a scene finishes loading, aka immediately after OnLevelWasLoaded is received.
        /// Note: Event is not fired when the first scene is loaded as a result of the Application starting.
        /// </summary>
        event EventHandler sceneLoaded;

        event EventHandler newGameBuilding;

        bool IsApplicationQuiting { get; }

        bool IsSceneLoading { get; }

        bool IsPaused { get; }

        bool IsRunning { get; }

        /// <summary>
        /// The SceneID of the CurrentScene that is showing. 
        /// </summary>
        SceneID CurrentSceneID { get; }

        /// <summary>
        /// The last SceneID that was showing before CurrentSceneID. If this is the
        /// first scene after initialization, LastSceneID will == CurrentSceneID.
        /// </summary>
        SceneID LastSceneID { get; }

        IList<Player> AllPlayers { get; }

        Player UserPlayer { get; }

        IList<Player> AIPlayers { get; }

        /// <summary>
        /// The current GameState. 
        /// WARNING: Do not subscribe to this. 
        /// Use the events as I can find out who has subscribed to them.
        /// </summary>
        GameState CurrentState { get; }

        GameSettings GameSettings { get; }

        AllKnowledge GameKnowledge { get; }

        UserPlayerAIManager UserAIManager { get; }

        void InitiateNewGame(GameSettings gameSettings);

        PlayerAIManager GetAIManagerFor(Player player);

        void RecordGameStateProgressionReadiness(MonoBehaviour source, GameState maxGameStateUntilReady, bool isReady);

        /// <summary>
        /// Requests a pause state change. A request to resume [!toPause] from a pause without using
        /// override will not occur if the pause was set with an override. Also, a request to resume from
        /// a pause without using override will not occur if there are still outstanding requests to pause.
        /// <remarks>12.31.17 Use of override will always set PauseState to that requested.</remarks>
        /// <remarks>12.31.17 If not using override, _autoPauseCount must be zero to resume from AutoPaused.</remarks>
        /// </summary>
        /// <param name="toPause">if set to <c>true</c> [to pause].</param>
        /// <param name="toOverride">if set to <c>true</c> [to override].</param>
        void RequestPauseStateChange(bool toPause, bool toOverride = false);

    }
}

