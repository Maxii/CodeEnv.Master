// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameTime.cs
// GameTime class wrapper around Unity's Time class.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;
    using UnityEditor;
    using System.Text;

    /**
     * Notes on Unity Time class:
     *  1. Time.timeScale is normally set to 1.0F, realtime. However, when set to 0.0F:
     *      - Pauses anything that is framerate independant.
     *          - These are primarily all physics and time-dependant functions, rigidbody forces and velocities.
     *          - Additionally the execution of FixedUpdate() is halted.
     *          - Animations and anything that uses Time.deltaTime since Time.deltaTime is now 0.0F
     *              - Time.deltaTime is actually time since last frame rendered * timeScale!
     *      - Update functions are still called every frame and realtimeSinceStartup still accumulates.
     *      - Rendering still occurs so there are frames.
     *      - Physics reactions (collisions, etc.) still work. Not clear what affect occurs?
     *      - Unity GUI elements are still active.
     * 
     * 
     * Thoughts on GameTime:
     *      1. I want animations to run at normal pace no matter how fast GameTime is moving or if paused.      
     *      2. I want the camera to always move independant of gameSpeed and while the Game is Paused.
     *           - Upshot of above = these animations need to use GameTime.DeltaTime that doesn't pause.
     */

    /// <summary>
    /// This GameTime class wraps UnityEngine.RealTime. All Game time related values should come from
    /// this class. This class also requires that Unity's Time.TimeScale is always = 1.0F.
    /// </summary>
    [Serializable]
    public sealed class GameTime {

        public static GameClockSpeed GameSpeed { get; private set; }

        /// <value>
        /// The amount of time in seconds elapsed since the last Frame 
        /// was rendered or zero if the game is paused. Useful for animations
        /// or other work that should stop while paused.
        /// </value>
        public static float DeltaTimeOrPaused {
            get {
                if (isPaused) {
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
        /// The real time in seconds since the start of the game.
        /// </value>
        public static float RealTime {
            get { return UnityEngine.Time.realtimeSinceStartup; }
        }

        /// <value>
        /// The real time in seconds since the start of the game less time paused.
        /// </value>
        public static float RealTimeLessTimePaused {
            get {
                float realTimeInCurrentPause = Constants.ZeroF;
                if (isPaused) {
                    realTimeInCurrentPause = RealTime - realTimeCurrentPauseBegan;
                }
                return RealTime - cumRealTimePreviouslyPaused - realTimeInCurrentPause;
            }
        }


        private static GameDate date;
        public static IGameDate Date {
            get {
                if (!isPaused) {
                    SyncGameClock();
                }
                // the only time the date needs to be synced is when it is about to be used
                date.SyncDateToGameClock(gameClockAtLastSync);
                return date;
            }
        }

        private static float cumRealTimePreviouslyPaused = Constants.ZeroF;
        private static float realTimeCurrentPauseBegan = Constants.ZeroF;
        private static float gameClockAtLastSync = Constants.ZeroF;
        private static float realTimeAtLastSync = Constants.ZeroF;

        private static bool isPaused;

        private GameEventManager eventMgr;

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
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="GameManager"/>.
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
            // Add initialization code here if any
            eventMgr = GameEventManager.Instance;
            SetupEventListeners();
            UnityEngine.Time.timeScale = Constants.OneF;
            GameSpeed = PlayerPrefsManager.Instance.GameSpeedOnLoadPref;
            date = new GameDate { DayOfYear = 1, Year = GameValues.StartingGameYear };
        }

        private void SetupEventListeners() {
            eventMgr.AddListener<GamePauseEvent>(OnPause);
            eventMgr.AddListener<GameSpeedChangeEvent>(OnGameSpeedChange);
        }

        private void OnGameSpeedChange(GameSpeedChangeEvent e) {
            if (GameSpeed != e.GameSpeed) {
                if (!isPaused) {
                    SyncGameClock();
                }
                GameSpeed = e.GameSpeed;
            }
        }

        private void OnPause(GamePauseEvent e) {
            bool toPause = e.Paused;
            Debug.Log("Pause event received. toPause = " + toPause);
            if (toPause) {
                SyncGameClock();    // update the game clock before pausing
                realTimeCurrentPauseBegan = RealTime;
            }
            else {  // resume
                float realTimeInCurrentPause = RealTime - realTimeCurrentPauseBegan;
                cumRealTimePreviouslyPaused += realTimeInCurrentPause;
                realTimeCurrentPauseBegan = Constants.ZeroF;

                // ignore the accumulated time during pause when next GameClockSync is requested
                realTimeAtLastSync = RealTime;
            }
            isPaused = toPause;
        }

        /// <summary>
        /// Brings the game clock up to date. While the Date only needs to be synced when it is about to be used,
        /// this clock must keep track of accumulated pauses and game speed changes. It is not necessary to 
        /// sync the date when a pause or speed change occurs as they don't have any use for the date.
        /// </summary>
        private static void SyncGameClock() {
            gameClockAtLastSync += GameSpeed.GetSpeedMultiplier() * (RealTime - realTimeAtLastSync);
            realTimeAtLastSync = RealTime;
            //Debug.Log("GameClock synced to: " + gameClockAtLastSync);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

