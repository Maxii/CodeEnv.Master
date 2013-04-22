// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: __GameTime.cs
// __GameTime class wrapper around Unity's Time class.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common.Unity {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

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
     * Thoughts on __GameTime:
     *      1. I want animations to run at normal pace no matter how fast __GameTime is moving or if toPause.      
     *      2. I want the camera to always move independant of gameSpeed and while the Game is ToPause.
     *           - Upshot of above = these animations need to use __GameTime.DeltaTime that doesn't pause.
     */

    /// <summary>
    /// This __GameTime class wraps UnityEngine.RealTime_Unity. All Game time related tValues should come from
    /// this class. This class also requires that Unity's Time.TimeScale is always = 1.0F.
    /// </summary>
    [Serializable]
    public class __GameTime : IDisposable {

        public static GameClockSpeed GameSpeed { get; private set; }

        /// <tPrefsValue>
        /// The amount of time in seconds elapsed since the last Frame 
        /// was rendered or zero if the game is paused. Useful for animations
        /// or other work that should stop while paused.
        /// </tPrefsValue>
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

        /// <tPrefsValue>
        /// The amount of time in seconds elapsed since the last Frame 
        /// was rendered whether the game is paused or not. Useful for 
        /// animations or other work I want to continue even while the game is paused.
        /// </tPrefsValue>
        public static float DeltaTime {
            get { return UnityEngine.Time.deltaTime; }
        }

        /// <tPrefsValue>
        /// The real time in seconds since the start of the game.
        /// </tPrefsValue>
        public static float RealTime {
            get { return UnityEngine.Time.realtimeSinceStartup; }
        }

        /// <tPrefsValue>
        /// The real time in seconds since the start of the game less time paused.
        /// </tPrefsValue>
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
        private static readonly __GameTime instance;
        /// <summary>Returns the singleton instance of this class.</summary>
        public static __GameTime Instance {
            get { return instance; }
        }

        /// <summary>
        /// Explicit static constructor that enables lazy instantiation by telling C# compiler
        /// not to mark type as beforefieldinit.
        /// </summary>
        static __GameTime() {
            // try, catch and resolve any possible exceptions here
            instance = new __GameTime();
        }

        /// <summary>
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="GameManager"/>.
        /// </summary>
        private __GameTime() {
            Initialize();
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
            GameSpeed = PlayerPrefsManager.Instance.GameSpeedOnLoad;
            date = new GameDate { DayOfYear = 1, Year = TempGameValues.StartingGameYear };
        }

        private void SetupEventListeners() {
            // eventMgr.AddListener<GamePauseStateChangedEvent>(OnPause);
            // eventMgr.AddListener<GameSpeedChangeEvent>(OnGameSpeedChange);
        }

        private void OnGameSpeedChange(GameSpeedChangeEvent e) {
            if (GameSpeed != e.GameSpeed) {
                if (!isPaused) {
                    SyncGameClock();
                }
                GameSpeed = e.GameSpeed;
            }
        }

        private void OnPause(GamePauseStateChangedEvent e) {
            //Debug.Log("Paused event received. GamePauseState = " + e.PauseState);
            switch (e.PauseState) {
                case GamePauseState.Paused:
                    SyncGameClock();    // update the game clock before pausing
                    realTimeCurrentPauseBegan = RealTime;
                    isPaused = true;
                    break;
                case GamePauseState.Resumed:
                    float realTimeInCurrentPause = RealTime - realTimeCurrentPauseBegan;
                    cumRealTimePreviouslyPaused += realTimeInCurrentPause;
                    realTimeCurrentPauseBegan = Constants.ZeroF;

                    // _ignore the accumulated time during pause when next GameClockSync is requested
                    realTimeAtLastSync = RealTime;
                    isPaused = false;
                    break;
                case GamePauseState.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(e.PauseState));
            }
        }

        /// <summary>
        /// Brings the game clock up to date. While the Date only needs to be synced when it is about to be used,
        /// this clock must keep track of accumulated pauses and game speed changes. It is not necessary to 
        /// sync the date when a pause or speed change occurs as they don't have any use for the date.
        /// </summary>
        private static void SyncGameClock() {
            gameClockAtLastSync += GameSpeed.GetSpeedMultiplier() * (RealTime - realTimeAtLastSync);
            realTimeAtLastSync = RealTime;
            //Debug.Log("GameClock synced to: " + currentDateTime);
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
                eventMgr.RemoveListener<GamePauseStateChangedEvent>(OnPause);
                eventMgr.RemoveListener<GameSpeedChangeEvent>(OnGameSpeedChange);

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

