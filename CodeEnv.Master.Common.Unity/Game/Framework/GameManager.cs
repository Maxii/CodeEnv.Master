// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameManager.cs
// SingletonPattern.  Primary Game Manager 'God' class for the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common.LocalResources;

    using UnityEngine;

    /// <summary>
    /// SingletonPattern. Primary Game Manager 'God' class for the game.
    /// </summary>
    public sealed class GameManager {

        private static bool isGamePaused;
        public static bool IsGamePaused {
            get { return isGamePaused; }
            set {
                if ((value && !isGamePaused) || (!value && isGamePaused)) {
                    isGamePaused = value;
                    eventMgr.Raise<GamePauseEvent>(new GamePauseEvent(value));
                }
                // Time.timeScale = (value) ? Constants.ZeroF : Constants.OneF;
            }
        }

        public static UniverseSize UniverseSize { get; set; }

        private static GameEventManager eventMgr;
        private GameTime gameTime;
        private PlayerPrefsManager playerPrefsMgr;

        #region SingletonPattern
        private static readonly GameManager instance;

        /// <summary>
        /// Explicit static constructor that enables lazy instantiation by telling C# compiler
        /// not to mark type as beforefieldinit.
        /// </summary>
        static GameManager() {
            // try, catch and resolve any possible exceptions here
            instance = new GameManager();
        }

        /// <summary>
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="GameManager"/>.
        /// </summary>
        private GameManager() {
            Initialize();
        }

        /// <summary>Returns the singleton instance of this class.</summary>
        public static GameManager Instance {
            get { return instance; }
        }
        #endregion

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        private void Initialize() {
            // Add initialization code here if any
            eventMgr = GameEventManager.Instance;
            gameTime = GameTime.Instance;
            playerPrefsMgr = PlayerPrefsManager.Instance;
            IsGamePaused = playerPrefsMgr.IsPauseOnLoadPref;
            // I will probably need to be able to force the GamePauseEvent to occur at initialization
            UniverseSize = playerPrefsMgr.UniverseSizePref;
        }

        // UNDONE not yet called
        public void Shutdown() {
            playerPrefsMgr.Store();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}


