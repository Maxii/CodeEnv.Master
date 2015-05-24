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

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// The primary class that keeps track of time during the game.
    /// Note: Seconds here always refers to seconds in the real world. There is no
    /// concept called GameTimeSeconds.
    /// </summary>
    [SerializeAll]
    public class GameTime : AGenericSingleton<GameTime>, IDisposable {

        #region Static Constants

        public static int HoursPerDay = GeneralSettings.Instance.HoursPerDay;
        public static int DaysPerYear = GeneralSettings.Instance.DaysPerYear;
        /// <summary>
        /// The number of GameHours in a Second at a GameSpeedMultiplier of 1 (aka GameSpeed.Normal).
        /// </summary>
        public static float HoursPerSecond = GeneralSettings.Instance.HoursPerSecond;
        public static int GameStartYear = GeneralSettings.Instance.GameStartYear;
        public static int GameEndYear = GeneralSettings.Instance.GameEndYear;

        #endregion

        public event Action<GameDate> onDateChanged;

        /// <summary>
        /// The number of Hours passing per second, adjusted for GameSpeed.
        /// </summary>
        public float GameSpeedAdjustedHoursPerSecond { get { return HoursPerSecond * _gameSpeedMultiplier; } }

        private GameSpeed _gameSpeed;
        public GameSpeed GameSpeed {
            get { return _gameSpeed; }
            set { SetProperty<GameSpeed>(ref _gameSpeed, value, "GameSpeed", OnGameSpeedChanged, OnGameSpeedChanging); }
        }

        /// <summary>
        /// The number of seconds elapsed, adjusted for GameSpeed, since the last Frame 
        /// was rendered or zero if the game is paused or not running. Useful for animations
        /// or other work that should reflect GameSpeed and Stop while paused.
        /// </summary>
        public float GameSpeedAdjustedDeltaTimeOrPaused {
            get {
                D.Assert(_isClockEnabled, "{0} clock is not enabled.".Inject(typeof(GameTime).Name));
                if (_gameMgr.IsPaused) {
                    return Constants.ZeroF;
                }
                return GameSpeedAdjustedDeltaTime;
            }
        }

        /// <summary>
        /// The number of seconds elapsed since the last Frame 
        /// was rendered or zero if the game is paused or not running. Useful for animations
        /// or other work that should not reflect GameSpeed but Stop while paused.
        /// </summary>
        public float DeltaTimeOrPaused {
            get {
                D.Assert(_isClockEnabled);
                if (_gameMgr.IsPaused) {
                    return Constants.ZeroF;
                }
                return DeltaTime;
            }
        }

        /// <summary>
        /// The number of seconds elapsed, adjusted for GameSpeed, since the last Frame 
        /// was rendered whether the game is paused or not. Useful for animations or other
        /// work that should reflect GameSpeed and continue even when the game is paused.
        /// </summary>
        public float GameSpeedAdjustedDeltaTime {
            get { return DeltaTime * _gameSpeedMultiplier; }
        }


        /// <summary>
        /// The number of seconds elapsed since the last Frame was rendered whether the 
        /// game is paused or not. Useful for animations or other work that should not reflect 
        /// GameSpeed and continue even when the game is paused.
        /// </summary>
        public float DeltaTime {
            get { return UnityEngine.Time.deltaTime; }
        }

        /// <summary>
        /// The number of seconds elapsed since this session with Unity started. In a standalone
        /// player, this is the time since the player was started. In the editor, this is the 
        /// time since the Editor Play button was pushed. GameClockSpeed has no effect.
        /// </summary>
        public float TimeInCurrentSession {
            get {
                float result = Time.time;
                //D.Log("TimeInCurrentSession = {0:0.00}.", result);
                return result;
            }
        }

        /// <summary>
        /// The number of seconds since a game instance was originally begun. Any time spent paused 
        /// during the game is included in this value. GameSpeed is not factored in.
        /// </summary>
        public float RealTime_Game {
            get {
                D.Assert(_isClockEnabled);
                float result = _cumTimeInPriorSessions + TimeInCurrentSession - _timeGameBeganInCurrentSession;
                //D.Log("RealTime_Game = {0:0.00}.", result);
                return result;
            }
        }

        /// <summary>
        /// The number of seconds since a new or saved game was begun.
        /// Time on hold (paused or not running) is not counted. GameSpeed is not factored in.
        /// </summary>
        public float RealTime_GamePlay {
            get {
                D.Assert(_isClockEnabled);
                float timeInCurrentPause = Constants.ZeroF;
                if (_gameMgr.IsPaused) {
                    timeInCurrentPause = RealTime_Game - _timeCurrentPauseBegan;
                }
                return RealTime_Game - _cumTimePaused - timeInCurrentPause;
            }
        }

        private GameDate _currentDate;
        /// <summary>
        /// The current GameDate in the game. This value takes into account when the game was begun,
        /// game speed changes and pauses.
        /// </summary>
        public GameDate CurrentDate {
            get {
                D.Assert(_isClockEnabled);
                CheckForDateChange();
                return _currentDate;
            }
            private set { _currentDate = value; }
        }


        public void CheckForDateChange() {
            if (!_isClockEnabled || _gameMgr.IsPaused) { return; }
            SyncGameClock();
            var updatedDate = new GameDate(_currentDateTime);
            //D.Log("GameDate {0} generated for CurrentDate changed check.", updatedDate);
            if (updatedDate != _currentDate) {   // use of _currentDate rather than CurrentDate.get() avoids infinite loop 
                // updatedDate can be < _currentDate when a new game is started
                CurrentDate = updatedDate;
                if (onDateChanged != null) {
                    //string subscribers = onDateChanged.GetInvocationList().Select(d => d.Target.GetType().Name).Concatenate();
                    //D.Log("{0}.onDateChanged. List = {1}.", GetType().Name, subscribers);
                    onDateChanged(updatedDate);
                }
            }
        }

        private IList<IDisposable> _subscriptions;

        // the amount of RealTime_Game time accumulated by a saved game in sessions prior to this one
        private float _cumTimeInPriorSessions;

        // a marker indicating the point in time in this session that the current game was begun
        private float _timeGameBeganInCurrentSession;

        // fields for tracking the amount of time paused in RealTime_Game units
        private float _cumTimePaused;
        private float _timeCurrentPauseBegan;

        /// <summary>
        /// The time this instance of the game has been running in seconds. Accounts for changes in gameSpeed
        /// and Pauses. Used to calculate the CurrentDate.
        /// </summary>
        private float _currentDateTime;
        private float _savedCurrentDateTime;    // FIXME required to save currentDateTime and then restore it. A bug?

        // internal field used to calculate the incremental elapsed time between syncs
        private float _gameRealTimeAtLastSync;

        private float _gameSpeedMultiplier;

        private bool _isClockEnabled;

        private PlayerPrefsManager _playerPrefsMgr;
        private IGameManager _gameMgr;

        private GameTime() {
            Initialize();
        }

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        protected override void Initialize() {
            //D.Log("{0}.Initialize() called.", GetType().Name);
            UnityEngine.Time.timeScale = Constants.OneF;
            _gameMgr = References.GameManager;
            _playerPrefsMgr = PlayerPrefsManager.Instance;
            PrepareToBeginNewGame();
            _subscriptions = new List<IDisposable>(); // placed here because GameTime exists in IntroScene. _subscriptions is null when disposing there
        }

        private void Subscribe() {
            _subscriptions.Add(_gameMgr.SubscribeToPropertyChanging<IGameManager, bool>(gm => gm.IsPaused, OnIsPausedChanging));
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
            D.Log("{0}.PrepareToBeginNewGame() called.", GetType().Name);
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
            // no need to assign a new CurrentDate as the change to _currentDateTime results in a new, synched CurrentDate instance once Date is requested
            // onDateChanged = null;   // new subscribers tend to subscribe on Awake, but nulling the list here clears it. All previous subscribers need to unsubscribe!
        }

        public void PrepareToSaveGame() {
            // isClockEnabled is by definition true if a game is about to be saved 
            // timeGameBeganInCurrentSession is important now in synching these values. It will be set to a new value when the clock is started again
            // timeCurrentPauseBegan is important now in synching these values. It will be set to a new value when the clock is paused again
            _cumTimePaused += RealTime_Game - _timeCurrentPauseBegan; // cumTimePaused must be updated now so it is current when saved
            // _gameRealTimeAtLastSync is not important to save. It will be set to the current RealTime_Game when the clock is started again
            // currentDateTime is key! It should be accurate as it gets updated at every sync.            
            D.Log("{0}.currentDateTime value being saved is {1:0.00}.", GetType().Name, _currentDateTime);
            _savedCurrentDateTime = _currentDateTime; // FIXME bug? currentDateTime does not get properly restored
            _cumTimeInPriorSessions = RealTime_Game; // cumTimeInPriorSessions must be updated (last so it doesn't affect other values here) so it is current when saved
            D.Log("{0}.PrepareToSaveGame called. cumTimeInPriorSessions set to {1:0.##}.", GetType().Name, _cumTimeInPriorSessions);
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
            // the list of subscribers to onDateChanged should be fine as saved
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
            D.Log("Starting GameClock. _timeGameBegunInCurrentSession set to {0:0.00}.", _timeGameBeganInCurrentSession);
            _gameRealTimeAtLastSync = RealTime_Game;
            SyncGameClock();
        }

        private void OnGameSpeedChanging(GameSpeed proposedSpeed) {
            SyncGameClock();
        }

        private void OnGameSpeedChanged() {
            _gameSpeedMultiplier = GameSpeed.SpeedMultiplier();
        }


        //private float timeAtLastSync;
        //private float currentTime;
        //private float accumulatedTime;
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
            //currentTime = Time.time;
            //accumulatedTime += (currentTime - timeAtLastSync);
            //timeAtLastSync = currentTime;
            //D.Log("AccumulatedTime = {0}.", accumulatedTime);
            _currentDateTime += GameSpeed.SpeedMultiplier() * (RealTime_Game - _gameRealTimeAtLastSync);
            _gameRealTimeAtLastSync = RealTime_Game;
            D.Log("GameClock for Date synced to {0:0.00}.", _currentDateTime);
        }

        private void Cleanup() {
            Unsubscribe();
            OnDispose();
        }

        private void Unsubscribe() {
            _subscriptions.ForAll<IDisposable>(s => s.Dispose());
            _subscriptions.Clear();
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
                Cleanup();
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


