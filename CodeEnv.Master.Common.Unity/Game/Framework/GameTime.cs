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

#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR
//#define DEBUG_LOG


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
    [SerializeAll]
    public class GameTime {

        public static GameClockSpeed GameSpeed { get; private set; }

        /// <value>
        /// The amount of time in seconds elapsed since the last Frame 
        /// was rendered or zero if the game is paused or not running. Useful for animations
        /// or other work that should stop while paused.
        /// </value>
        public static float DeltaTimeOrPaused {
            get {
                D.Assert(Instance.isClockEnabled);
                if (GameManager.IsGamePaused) {
                    return Constants.ZeroF;
                }
                return DeltaTime;
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
        /// The realtime elapsed since this session with Unity started. In a standalone
        /// player, this is the time since the player was started. In the editor, this is the 
        /// time since the Editor Play button was pushed.
        /// </value>
        public static float TimeInCurrentSession {
            get {
                float result = Time.time;
                D.Log("TimeInCurrentSession = {0:0.00}.", result);
                return result;
            }
        }

        /// <value>
        /// The real time in seconds since the Unity Application was launched.
        /// </value>
        [Obsolete]
        // very strange behaviour. it appears to count the time since the last play push, then shortly after the current push, resets to zero to start again
        public static float RealTime_Unity {
            get {
                D.Log("Time.realtimeSinceStartup = {0:0.00}, Time.time = {1:0.00}.", UnityEngine.Time.realtimeSinceStartup, UnityEngine.Time.time);
                return Time.realtimeSinceStartup;
            }
        }

        /// <value>
        /// The real time in seconds since a game instance was originally begun. Any time spent paused 
        /// during the game is included in this value. GameClockSpeed does not effect this.
        /// </value>
        public static float RealTime_Game {
            get {
                D.Assert(Instance.isClockEnabled);
                float result = Instance.cumTimeInPriorSessions + TimeInCurrentSession - Instance.timeGameBeganInCurrentSession;
                D.Log("RealTime_Game = {0:0.00}.", result);
                return result;
            }
        }

        /// <value>
        /// The real time in seconds since a new or saved game was begun.
        /// Time on hold (paused or not running) is not counted. GameClockSpeed does not effect this.
        /// </value>
        public static float RealTime_GamePlay {
            get {
                D.Assert(Instance.isClockEnabled);
                float timeInCurrentPause = Constants.ZeroF;
                if (GameManager.IsGamePaused) {
                    timeInCurrentPause = RealTime_Game - Instance.timeCurrentPauseBegan;
                }
                return RealTime_Game - Instance.cumTimePaused - timeInCurrentPause;
            }
        }

        /// <summary>
        /// The GameDate in the game. This value takes into account when the game was begun,
        /// game speed changes and pauses.
        /// </summary>
        private static GameDate date;
        public static IGameDate Date {
            get {
                D.Assert(Instance.isClockEnabled);
                Instance.SyncGameClock();
                // the only time the date needs to be synced is when it is about to be used
                date.SyncDateToGameClock(Instance.currentDateTime);
                return date;
            }
        }

        // the amount of RealTime_Game time accumulated by a saved game in sessions prior to this one
        private float cumTimeInPriorSessions;

        // a marker indicating the point in time in this session that the current game was begun
        private float timeGameBeganInCurrentSession;

        // fields for tracking the amount of time paused in RealTime_Game units
        private float cumTimePaused;
        private float timeCurrentPauseBegan;

        // time in seconds used to calculate the Date. Accounts for speed and pausing
        private float currentDateTime;
        private float _savedCurrentDateTime;    // FIXME required to save currentDateTime and then restore it. A bug?

        // internal field used to calculate the incremental elapsed time between syncs
        private float _gameRealTimeAtLastSync;

        private bool isClockEnabled;
        //private bool isPaused;

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
            UnityEngine.Time.timeScale = Constants.OneF;
            playerPrefsMgr = PlayerPrefsManager.Instance;
            PrepareToBeginNewGame();
        }

        private void AddListeners() {
            eventMgr.AddListener<GameSpeedChangeEvent>(this, OnGameSpeedChange);
            eventMgr.AddListener<GamePauseStateChangingEvent>(this, OnPauseStateChanging);
        }

        private void OnPauseStateChanging(GamePauseStateChangingEvent e) {
            D.Assert(isClockEnabled);
            GamePauseState pauseCmd = e.PauseState;
            switch (pauseCmd) {
                case GamePauseState.Paused:
                    SyncGameClock();    // update the game clock before pausing
                    timeCurrentPauseBegan = RealTime_Game;
                    break;
                case GamePauseState.Resumed:
                    float timeInCurrentPause = RealTime_Game - timeCurrentPauseBegan;
                    cumTimePaused += timeInCurrentPause;
                    D.Log("TimeGameBegunInCurrentSession = {0:0.00}, TimeCurrentPauseBegan (GameTime) = {1:0.00}", timeGameBeganInCurrentSession, timeCurrentPauseBegan);
                    D.Log("TimeInCurrentPause (GameTime) = {0:0.00}, RealTime_Game = {1:0.00}.", timeInCurrentPause, RealTime_Game);

                    timeCurrentPauseBegan = Constants.ZeroF;

                    // ignore the accumulated time during pause when next GameClockSync is requested
                    _gameRealTimeAtLastSync = RealTime_Game;
                    D.Log("CumTimePaused = {0:0.00}, _gameRealTimeAtLastSync = {1:0.00}.", cumTimePaused, _gameRealTimeAtLastSync);
                    break;
                case GamePauseState.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(pauseCmd));
            }
        }

        public void PrepareToBeginNewGame() {
            D.Log("GameTime.PrepareForWaiting() called.");
            EnableClock(false);
            cumTimeInPriorSessions = Constants.ZeroF;
            timeGameBeganInCurrentSession = Constants.ZeroF;
            cumTimePaused = Constants.ZeroF;
            timeCurrentPauseBegan = Constants.ZeroF;
            _gameRealTimeAtLastSync = Constants.ZeroF;
            currentDateTime = Constants.ZeroF;
            _savedCurrentDateTime = Constants.ZeroF;
            // don't rely on initialization events from the gui
            GameSpeed = playerPrefsMgr.GameSpeedOnLoad;
            date = new GameDate { DayOfYear = 1, Year = TempGameValues.StartingGameYear };
        }

        public void PrepareToSaveGame() {
            // isClockEnabled is by definition true if a game is about to be saved 
            // timeGameBeganInCurrentSession is important now in synching these values. It will be set to a new value when the clock is started again
            // timeCurrentPauseBegan is important now in synching these values. It will be set to a new value when the clock is paused again
            cumTimePaused += RealTime_Game - timeCurrentPauseBegan; // cumTimePaused must be updated now so it is current when saved
            // _gameRealTimeAtLastSync is not important to save. It will be set to the current RealTime_Game when the clock is started again
            // currentDateTime is key! It should be accurate as it gets updated at every sync.            
            D.Log("currentDateTime value being saved is {0:0.00}.", currentDateTime);
            _savedCurrentDateTime = currentDateTime; // FIXME bug? currentDateTime does not get properly restored
            cumTimeInPriorSessions = RealTime_Game; // cumTimeInPriorSessions must be updated (last so it doesn't affect other values here) so it is current when saved
            D.Log("PrepareToSaveGame called. cumTimeInPriorSessions set to {0:0.00}.", cumTimeInPriorSessions);
        }

        public void PrepareToResumeSavedGame() {
            EnableClock(false); // when saved it was enabled. Disable now pending re-enable on Running
            // cumTimeInPriorSessions was updated before saving, so it should be restored to the right value
            D.Log("GameTime.PrepareToResumeSavedGame() called. cumTimeInPriorSessions restored to {0:0.0)}.", cumTimeInPriorSessions);
            // timeGameBeganInCurrentSession that was saved is irrelevant. It will be updated when the clock is enabled on Running
            // cumTimePaused was updated before saving, so it should be restored to the right value
            // timeCurrentPauseBegan will be set to a new value when the clock is paused again
            // _gameRealTimeAtLastSync will be set to the current RealTime_Game when the clock is started again

            // currentDateTime is key! It value when restored should be accurate as it was updated at every sync prior to being saved
            currentDateTime = _savedCurrentDateTime; // FIXME bug? currentDateTime does not get properly restored
            D.Log("currentDateTime restored to {0:0.00}.", currentDateTime);

            GameSpeed = playerPrefsMgr.GameSpeedOnLoad; // the speed the clock was running at when saved is not relevant in the following session
            // date that is saved is fine and should be accurate. It gets recalculated from currentDateTime everytime it is used
        }

        public void EnableClock(bool toEnable) {
            D.Assert(!GameManager.IsGamePaused);    // my practice - enable clock, then pause it
            isClockEnabled = toEnable;
            if (toEnable) {
                AddListeners();
                StartClock();
            }
            else {
                RemoveListeners();
            }
        }

        private void StartClock() {
            timeGameBeganInCurrentSession = TimeInCurrentSession;
            D.Log("TimeGameBegunInCurrentSession set to {0:0.00}.", timeGameBeganInCurrentSession);
            _gameRealTimeAtLastSync = RealTime_Game;
            SyncGameClock();
        }

        private void OnGameSpeedChange(GameSpeedChangeEvent e) {
            D.Assert(isClockEnabled);
            if (GameSpeed != e.GameSpeed) {
                SyncGameClock();
                GameSpeed = e.GameSpeed;
            }
        }

        /// <summary>
        /// Brings the game clock up to date. While the Date only needs to be synced when it is about to be used,
        /// this clock must keep track of accumulated pauses and game speed changes. It is not necessary to 
        /// sync the date when a pause or speed change occurs as they don't have any use for the date.
        /// </summary>
        private void SyncGameClock() {
            D.Assert(isClockEnabled);
            if (GameManager.IsGamePaused) {
                D.Warn("SyncGameClock called while Paused!");   // it keeps adding to currentDateTime
                return;
            }
            currentDateTime += GameSpeed.GetSpeedMultiplier() * (RealTime_Game - _gameRealTimeAtLastSync);
            _gameRealTimeAtLastSync = RealTime_Game;
            D.Log("GameClock synced to {0:0.00}.", currentDateTime);
        }

        private void RemoveListeners() {
            eventMgr.RemoveListener<GameSpeedChangeEvent>(this, OnGameSpeedChange);
            eventMgr.RemoveListener<GamePauseStateChangingEvent>(this, OnPauseStateChanging);
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
                RemoveListeners();
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


