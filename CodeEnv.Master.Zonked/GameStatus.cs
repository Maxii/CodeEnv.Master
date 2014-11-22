// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameStatus.cs
//  Singleton. Non-MonoBehaviour Class holding commonly used game status values
// that need to be accessible from within the GameContent assembly. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Singleton. Non-MonoBehaviour Class holding commonly used game status values
    /// that need to be accessible from within the GameContent assembly. 
    /// IMPROVE GameManager sets these values directly so they must be public, but no other 
    /// client should be able to set them.
    /// </summary>
    [Obsolete]
    public class GameStatus : AGenericSingleton<GameStatus> {

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
        public event Action onIsRunningOneShot;

        private bool _isRunning;
        /// <summary>
        /// Treat as Readonly except by GameManager. Indicates whether the game
        /// is in GameState.Running or not.
        /// </summary>
        public bool IsRunning {
            get { return _isRunning; }
            set { SetProperty<bool>(ref _isRunning, value, "IsRunning", OnIsRunningChanged); }
        }

        private bool _isPaused;
        /// <summary>
        /// Treat as Readonly except by GameManager. Indicates whether the game is paused. Automatically set
        /// as a result of GameManager.OnPauseStateChanged. Warning: This means OnIsPausedChanging actually
        /// occurs AFTER the PauseState changes, but always before OnIsPausedChanged.
        /// </summary>
        public bool IsPaused {
            get { return _isPaused; }
            set { SetProperty<bool>(ref _isPaused, value, "IsPaused"); }
        }

        private GameStatus() {
            Initialize();
        }

        protected override void Initialize() { }

        private void OnIsRunningChanged() {
            D.Log("{0}.IsRunning changed to {1}.", GetType().Name, IsRunning);
            if (IsRunning) {
                if (onIsRunningOneShot != null) {
                    onIsRunningOneShot();
                    onIsRunningOneShot = null;
                }
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}


