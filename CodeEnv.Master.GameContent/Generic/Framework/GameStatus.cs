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
    /// FIXME GameManager sets these values directly so they must be public, but no other 
    /// client should be able to set them.
    /// </summary>
    public class GameStatus : AGenericSingleton<GameStatus> {

        /// <summary>
        /// Occurs once when the game initially starts Running. The invocation list is cleared
        /// after use so it is a one-shot implementation requiring no client cleanup to unsubscribe.
        /// This event is NOT called when the game changes FROM Running.
        /// </summary>
        public event Action onIsRunning_OneShot;

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
                var temp = onIsRunning_OneShot;
                if (temp != null) {
                    temp();
                }
                onIsRunning_OneShot = null; // clears the invocation list thereby requiring no cleanup on the client end
            }
        }
    }
}


