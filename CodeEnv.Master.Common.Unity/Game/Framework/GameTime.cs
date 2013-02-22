// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameTime.cs
//  The primary class that keeps track of game time.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// The primary class that keeps track of game time.
    /// </summary>
    public class GameTime {


        public static GameClockSpeed GameSpeed { get; private set; }

        /// <value>
        /// The amount of time in seconds elapsed since the last Frame 
        /// was rendered or zero if the game is paused or not running. Useful for animations
        /// or other work that should stop while paused.
        /// </value>
        public static float DeltaTimeOrPaused {
            get {
                if (isPaused || !isGameRunning) {
                    return Constants.ZeroF;
                }
                else {
                    return DeltaTime;
                }
            }
        }

        /// <value>
        /// The amount of time in seconds elapsed since the last Frame 
        /// was rendered whether the game is paused or not. Useful for 
        /// animations or other work I want to continue even while the game is paused.
        /// </value>
        public static float DeltaTime {
            get { return UnityEngine.Time.deltaTime; }
        }

        /// <value>
        /// The real time in seconds since the Unity Application was launched.
        /// </value>
        public static float RealTime_Unity {
            get { return UnityEngine.Time.realtimeSinceStartup; }
        }

        /// <value>
        /// The real time in seconds since a new or saved game was begun. GameClockSpeed
        /// does not effect this.
        /// </value>
        public static float RealTime_Game {
            get {
                if (Mathfx.Approx(timeGameBegun, Constants.ZeroF, .01F)) {
                    return Constants.ZeroF;
                }
                return RealTime_Unity - timeGameBegun;
            }
        }

        /// <value>
        /// The real time in seconds since a new or saved game was begun.
        /// Time on hold (paused or not running) is not counted. GameClockSpeed does not effect this.
        /// </value>
        public static float RealTime_GamePlay {
            get {
                float timeInCurrentHold = Constants.ZeroF;
                if (isPaused || !isGameRunning) {
                    timeInCurrentHold = RealTime_Game - timeCurrentHoldBegan;
                }
                return RealTime_Game - cumTimeOnHold - timeInCurrentHold;
            }
        }

        /// <summary>
        /// The GameDate in the game. This value takes into account when the game was begun,
        /// game speed changes and holds.
        /// </summary>
        private static GameDate date;
        public static IGameDate Date {
            get {
                if (!isPaused && isGameRunning) {
                    SyncGameClock();
                }
                // the only time the date needs to be synced is when it is about to be used
                date.SyncDateToGameClock(gameDateTimeAtLastSync);
                return date;
            }
        }

        // time the game was begun in RealTime_Unity units
        private static float timeGameBegun;

        // fields for tracking the amount of time paused or not running in RealTime_Game units
        private static float cumTimeOnHold;
        private static float timeCurrentHoldBegan;
        // time in seconds used to calculate the Date. Accounts for speed and holds
        private static float gameDateTimeAtLastSync;
        // the real time in seconds used to calculate the DateTime above when syncing
        private static float gameRealTimeAtLastSync;

        private static bool isGameRunning;
        private static bool isPaused;

        private GameEventManager eventMgr;
        private PlayerPrefsManager playerPrefsMgr;

        #region SingletonPattern
        private static readonly GameTime instance;

        /// <summary>
        /// Explicit static constructor that enables lazy instantiation by telling C# compiler
        /// not to mark type as beforefieldinit.
        /// </summary>
        static GameTime() {
            // try, catch and resolve any possible exceptions here
            instance = new GameTime();
        }

        /// <summary>
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="GameTime"/>.
        /// </summary>
        private GameTime() {
            Initialize();
        }

        /// <summary>Returns the singleton instance of this class.</summary>
        public static GameTime Instance {
            get { return instance; }
        }
        #endregion

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        private void Initialize() {
            eventMgr = GameEventManager.Instance;
            AddEventListeners();
            UnityEngine.Time.timeScale = Constants.OneF;
            playerPrefsMgr = PlayerPrefsManager.Instance;
            date = new GameDate { DayOfYear = 1, Year = TempGameValues.StartingGameYear };
            GameSpeed = playerPrefsMgr.GameSpeedOnLoad;
            InitializeForStartScene();
        }

        private void InitializeForStartScene() {
            SceneLevel scene = (SceneLevel)Application.loadedLevel;
            switch (scene) {
                case SceneLevel.IntroScene:
                    isGameRunning = false; // a GameStateChange to Running will change this
                    break;
                case SceneLevel.GameScene:
                    isGameRunning = true;
                    break;
                case SceneLevel.AllGuiOnly:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(scene));
            }
        }

        private void AddEventListeners() {
            eventMgr.AddListener<GameSpeedChangeEvent>(OnGameSpeedChange);
            eventMgr.AddListener<PauseGameEvent>(OnPauseGameCommand);
            eventMgr.AddListener<GameStateChangeEvent>(OnGameStateChange);
            //eventMgr.AddListener<GameLoadedEvent>(OnGameLoaded);
        }

        //private void OnGameLoaded(GameLoadedEvent e) {
        //    GameSpeed = playerPrefsMgr.GameSpeedOnLoad;
        //}

        private void OnGameStateChange(GameStateChangeEvent e) {
            if (e.NewState == GameState.Running) {
                isGameRunning = true;
                if (!isPaused) {
                    // establish the game clock's start time
                    timeGameBegun = RealTime_Unity;
                    //Debug.Log("OnGameStateChange TimeGameBegun (UnityTime) set to {0}.".Inject(timeGameBegun));
                }
                // ifPaused, resume will establish the start time
            }
        }

        // the first call to this will come from GameManager setting IsGamePaused to the playerPref onload
        private void OnPauseGameCommand(PauseGameEvent e) {
            switch (e.PauseCmd) {
                case PauseGameCommand.Pause:
                    if (isGameRunning) {
                        SyncGameClock();    // update the game clock before pausing
                        timeCurrentHoldBegan = RealTime_Game;
                    }
                    isPaused = true;
                    break;
                case PauseGameCommand.Resume:
                    if (isGameRunning) {
                        if (Mathfx.Approx(timeGameBegun, Constants.ZeroF, .01F)) {
                            // If player pref is to start the game paused on load, then the time the game
                            // clock starts counting is when the player hits the resume button
                            timeGameBegun = RealTime_Unity;
                            //Debug.Log("At Resume TimeGameBegun (UnityTime) set to {0}.".Inject(timeGameBegun));

                        }
                        float timeInCurrentHold = RealTime_Game - timeCurrentHoldBegan;
                        cumTimeOnHold += timeInCurrentHold;
                        // Debug.Log("TimeGameBegun (UnityTime) = {0}, TimeCurrentHoldBegan (GameTime) = {1}".Inject(timeGameBegun, timeCurrentHoldBegan));
                        //Debug.Log("TimeInCurrentHold (GameTime) = {0}, RealTime_Game = {1}.".Inject(timeInCurrentHold, RealTime_Game));

                        timeCurrentHoldBegan = Constants.ZeroF;

                        // ignore the accumulated time during pause when next GameClockSync is requested
                        gameRealTimeAtLastSync = RealTime_Game;
                        //Debug.Log("CumTimeOnHold (GameTime) = {0}, GameRealTimeAtLastSync = {1}".Inject(cumTimeOnHold, gameRealTimeAtLastSync));
                    }
                    isPaused = false;

                    break;
                case PauseGameCommand.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(e.PauseCmd));
            }
        }

        private void OnGameSpeedChange(GameSpeedChangeEvent e) {
            if (GameSpeed != e.GameSpeed) {
                if (!isPaused && isGameRunning) {
                    SyncGameClock();
                }
                GameSpeed = e.GameSpeed;
            }
        }

        /// <summary>
        /// Brings the game clock up to date. While the Date only needs to be synced when it is about to be used,
        /// this clock must keep track of accumulated pauses and game speed changes. It is not necessary to 
        /// sync the date when a pause or speed change occurs as they don't have any use for the date.
        /// </summary>
        private static void SyncGameClock() {
            gameDateTimeAtLastSync += GameSpeed.GetSpeedMultiplier() * (RealTime_Game - gameRealTimeAtLastSync);
            gameRealTimeAtLastSync = RealTime_Game;
            //Debug.Log("GameClock synced to: " + gameDateTimeAtLastSync);
        }

        void OnDestroy() {
            Dispose();
        }

        #region IDisposable
        private bool alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (alreadyDisposed) {
                return;
            }

            if (isDisposing) {
                // free managed resources here including unhooking events
                eventMgr.RemoveListener<GameSpeedChangeEvent>(OnGameSpeedChange);
                //eventMgr.RemoveListener<GameClockEvent>(OnGameClockCommand);
                eventMgr.RemoveListener<PauseGameEvent>(OnPauseGameCommand);
            }
            // free unmanaged resources here
            alreadyDisposed = true;
        }

        // Example method showing check for whether the object has been disposed
        //public void ExampleMethod() {
        //    // throw Exception if called on object that is already disposed
        //    if(alreadyDisposed) {
        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    }

        //    // method content here
        //}
        #endregion


        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}


