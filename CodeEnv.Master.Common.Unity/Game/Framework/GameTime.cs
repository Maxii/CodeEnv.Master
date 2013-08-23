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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
    public class GameTime : APropertyChangeTracking {

        private GameClockSpeed _gameSpeed;
        public GameClockSpeed GameSpeed {
            get { return _gameSpeed; }
            set {
                SetProperty<GameClockSpeed>(ref _gameSpeed, value, "GameSpeed", OnGameSpeedChanged, OnGameSpeedChanging);
            }
        }

        /// <summary>
        /// The amount of time in seconds elapsed since the last Frame 
        /// was rendered or zero if the game is paused or not running. Useful for animations
        /// or other work that should stop while paused. GameClockSpeed IS factored in.
        /// </summary>
        public static float DeltaTimeOrPausedWithGameSpeed {
            get {
                D.Assert(Instance._isClockEnabled);
                if (GameManager.Instance.IsPaused) {
                    return Constants.ZeroF;
                }
                return DeltaTimeWithGameSpeed;
            }
        }

        /// <summary>
        /// The amount of time in seconds elapsed since the last Frame 
        /// was rendered or zero if the game is paused or not running. Useful for animations
        /// or other work that should stop while paused.
        /// </summary>
        public static float DeltaTimeOrPaused {
            get {
                D.Assert(Instance._isClockEnabled);
                if (GameManager.Instance.IsPaused) {
                    return Constants.ZeroF;
                }
                return DeltaTime;
            }
        }

        /// <summary>
        /// The amount of time in seconds elapsed since the last Frame 
        /// was rendered whether the game is paused or not. Useful for 
        /// animations or other work I want to continue even while the game is paused.
        /// GameClockSpeed IS factored in.
        /// </summary>
        public static float DeltaTimeWithGameSpeed {
            get { return DeltaTime * Instance._gameSpeedMultiplier; }
        }


        /// <summary>
        /// The amount of time in seconds elapsed since the last Frame 
        /// was rendered whether the game is paused or not. Useful for 
        /// animations or other work I want to continue even while the game is paused.
        /// GameClockSpeed has no effect.
        /// </summary>
        public static float DeltaTime {
            get { return UnityEngine.Time.deltaTime; }
        }

        /// <summary>
        /// The realtime elapsed since this session with Unity started. In a standalone
        /// player, this is the time since the player was started. In the editor, this is the 
        /// time since the Editor Play button was pushed. GameClockSpeed has no effect.
        /// </summary>
        public static float TimeInCurrentSession {
            get {
                float result = Time.time;
                D.Log("TimeInCurrentSession = {0:0.00}.", result);
                return result;
            }
        }

        /// <summary>
        /// The real time in seconds since a game instance was originally begun. Any time spent paused 
        /// during the game is included in this value. GameClockSpeed has no effect.
        /// </summary>
        public static float RealTime_Game {
            get {
                D.Assert(Instance._isClockEnabled);
                float result = Instance._cumTimeInPriorSessions + TimeInCurrentSession - Instance._timeGameBeganInCurrentSession;
                D.Log("RealTime_Game = {0:0.00}.", result);
                return result;
            }
        }

        /// <summary>
        /// The real time in seconds since a new or saved game was begun.
        /// Time on hold (paused or not running) is not counted. GameClockSpeed has no effect.
        /// </summary>
        public static float RealTime_GamePlay {
            get {
                D.Assert(Instance._isClockEnabled);
                float timeInCurrentPause = Constants.ZeroF;
                if (GameManager.Instance.IsPaused) {
                    timeInCurrentPause = RealTime_Game - Instance._timeCurrentPauseBegan;
                }
                return RealTime_Game - Instance._cumTimePaused - timeInCurrentPause;
            }
        }


        private static GameDate _date;
        /// <summary>
        /// The GameDate in the game. This value takes into account when the game was begun,
        /// game speed changes and pauses.
        /// </summary>
        public static IGameDate Date {
            get {
                D.Assert(Instance._isClockEnabled);
                if (!GameManager.Instance.IsPaused) {
                    Instance.SyncGameClock();   // OK to ask for date while paused (ie. HUD needs), but Syncing clock won't do anything
                }
                // the only time the date needs to be synced is when it is about to be used
                _date.SyncDateToGameClock(Instance._currentDateTime);
                return _date;
            }
        }

        private IList<IDisposable> _subscribers;

        // the amount of RealTime_Game time accumulated by a saved game in sessions prior to this one
        private float _cumTimeInPriorSessions;

        // a marker indicating the point in time in this session that the current game was begun
        private float _timeGameBeganInCurrentSession;

        // fields for tracking the amount of time paused in RealTime_Game units
        private float _cumTimePaused;
        private float _timeCurrentPauseBegan;

        // time in seconds used to calculate the Date. Accounts for speed and pausing
        private float _currentDateTime;
        private float _savedCurrentDateTime;    // FIXME required to save currentDateTime and then restore it. A bug?

        // internal field used to calculate the incremental elapsed time between syncs
        private float _gameRealTimeAtLastSync;

        private float _gameSpeedMultiplier;

        private bool _isClockEnabled;

        private GameEventManager _eventMgr;
        private PlayerPrefsManager _playerPrefsMgr;
        private GameManager _gameMgr;

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
            _gameMgr = GameManager.Instance;
            _eventMgr = GameEventManager.Instance;
            UnityEngine.Time.timeScale = Constants.OneF;
            _playerPrefsMgr = PlayerPrefsManager.Instance;
            PrepareToBeginNewGame();
        }

        private void Subscribe() {
            if (_subscribers == null) {
                _subscribers = new List<IDisposable>();
            }
            _subscribers.Add(_gameMgr.SubscribeToPropertyChanging<GameManager, bool>(gm => gm.IsPaused, OnIsPausedChanging));
        }

        private void OnIsPausedChanging(bool isPausing) {
            D.Assert(_isClockEnabled);
            if (isPausing) {
                // we are about to pause
                SyncGameClock();    // update the game clock before pausing
                _timeCurrentPauseBegan = RealTime_Game;
            }
            else {
                // we are about to resume play
                float timeInCurrentPause = RealTime_Game - _timeCurrentPauseBegan;
                _cumTimePaused += timeInCurrentPause;
                D.Log("TimeGameBegunInCurrentSession = {0:0.00}, TimeCurrentPauseBegan (GameTime) = {1:0.00}", _timeGameBeganInCurrentSession, _timeCurrentPauseBegan);
                D.Log("TimeInCurrentPause (GameTime) = {0:0.00}, RealTime_Game = {1:0.00}.", timeInCurrentPause, RealTime_Game);

                _timeCurrentPauseBegan = Constants.ZeroF;

                // ignore the accumulated time during pause when next GameClockSync is requested
                _gameRealTimeAtLastSync = RealTime_Game;
                D.Log("CumTimePaused = {0:0.00}, _gameRealTimeAtLastSync = {1:0.00}.", _cumTimePaused, _gameRealTimeAtLastSync);
            }
        }

        public void PrepareToBeginNewGame() {
            D.Log("GameTime.PrepareToBeginNewGame() called.");
            EnableClock(false);
            _cumTimeInPriorSessions = Constants.ZeroF;
            _timeGameBeganInCurrentSession = Constants.ZeroF;
            _cumTimePaused = Constants.ZeroF;
            _timeCurrentPauseBegan = Constants.ZeroF;
            _gameRealTimeAtLastSync = Constants.ZeroF;
            _currentDateTime = Constants.ZeroF;
            _savedCurrentDateTime = Constants.ZeroF;
            // don't wait for the Gui to set GameSpeed. Use the backing field as the Property calls OnGameSpeedChanged()
            _gameSpeed = _playerPrefsMgr.GameSpeedOnLoad;
            _gameSpeedMultiplier = _gameSpeed.SpeedMultiplier();
            _date = new GameDate();
        }

        public void PrepareToSaveGame() {
            // isClockEnabled is by definition true if a game is about to be saved 
            // timeGameBeganInCurrentSession is important now in synching these values. It will be set to a new value when the clock is started again
            // timeCurrentPauseBegan is important now in synching these values. It will be set to a new value when the clock is paused again
            _cumTimePaused += RealTime_Game - _timeCurrentPauseBegan; // cumTimePaused must be updated now so it is current when saved
            // _gameRealTimeAtLastSync is not important to save. It will be set to the current RealTime_Game when the clock is started again
            // currentDateTime is key! It should be accurate as it gets updated at every sync.            
            D.Log("currentDateTime value being saved is {0:0.00}.", _currentDateTime);
            _savedCurrentDateTime = _currentDateTime; // FIXME bug? currentDateTime does not get properly restored
            _cumTimeInPriorSessions = RealTime_Game; // cumTimeInPriorSessions must be updated (last so it doesn't affect other values here) so it is current when saved
            D.Log("PrepareToSaveGame called. cumTimeInPriorSessions set to {0:0.00}.", _cumTimeInPriorSessions);
        }

        public void PrepareToResumeSavedGame() {
            EnableClock(false); // when saved it was enabled. Disable now pending re-enable on Running
            // cumTimeInPriorSessions was updated before saving, so it should be restored to the right value
            D.Log("GameTime.PrepareToResumeSavedGame() called. cumTimeInPriorSessions restored to {0:0.0)}.", _cumTimeInPriorSessions);
            // timeGameBeganInCurrentSession that was saved is irrelevant. It will be updated when the clock is enabled on Running
            // cumTimePaused was updated before saving, so it should be restored to the right value
            // timeCurrentPauseBegan will be set to a new value when the clock is paused again
            // _gameRealTimeAtLastSync will be set to the current RealTime_Game when the clock is started again

            // currentDateTime is key! It value when restored should be accurate as it was updated at every sync prior to being saved
            _currentDateTime = _savedCurrentDateTime; // FIXME bug? currentDateTime does not get properly restored
            D.Log("currentDateTime restored to {0:0.00}.", _currentDateTime);
            // don't wait for the Gui to set GameSpeed. Use the backing field as the Property calls OnGameSpeedChanged()
            _gameSpeed = _playerPrefsMgr.GameSpeedOnLoad; // the speed the clock was running at when saved is not relevant in the following session
            _gameSpeedMultiplier = _gameSpeed.SpeedMultiplier();
            // date that is saved is fine and should be accurate. It gets recalculated from currentDateTime everytime it is used
        }

        public void EnableClock(bool toEnable) {
            D.Assert(!_gameMgr.IsPaused);    // my practice - enable clock, then pause it
            if (_isClockEnabled != toEnable) {
                _isClockEnabled = toEnable;
                if (toEnable) {
                    Subscribe();
                    StartClock();
                }
                else {
                    Unsubscribe();
                }
            }
        }

        private void StartClock() {
            _timeGameBeganInCurrentSession = TimeInCurrentSession;
            D.Log("TimeGameBegunInCurrentSession set to {0:0.00}.", _timeGameBeganInCurrentSession);
            _gameRealTimeAtLastSync = RealTime_Game;
            SyncGameClock();
        }

        private void OnGameSpeedChanging(GameClockSpeed proposedSpeed) {
            SyncGameClock();
        }

        private void OnGameSpeedChanged() {
            _gameSpeedMultiplier = GameSpeed.SpeedMultiplier();
        }

        /// <summary>
        /// Brings the game clock up to date. While the Date only needs to be synced when it is about to be used,
        /// this clock must keep track of accumulated pauses and game speed changes. It is not necessary to 
        /// sync the date when a pause or speed change occurs as they don't have any use for the date.
        /// </summary>
        private void SyncGameClock() {
            D.Assert(_isClockEnabled);
            if (_gameMgr.IsPaused) {
                D.Warn("SyncGameClock called while Paused!");   // it keeps adding to currentDateTime
                return;
            }
            _currentDateTime += GameSpeed.SpeedMultiplier() * (RealTime_Game - _gameRealTimeAtLastSync);
            _gameRealTimeAtLastSync = RealTime_Game;
            D.Log("GameClock synced to {0:0.00}.", _currentDateTime);
        }

        private void Unsubscribe() {
            _subscribers.ForAll<IDisposable>(s => s.Dispose());
            _subscribers.Clear();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
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
                Unsubscribe();
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

    }
}


