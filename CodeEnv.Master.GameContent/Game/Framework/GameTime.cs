// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameTime.cs
// The primary class that keeps track of time during a GameInstance and/or UnitySession.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// The primary class that keeps track of time during a GameInstance and/or UnitySession.
    /// Note: Seconds here always refers to seconds in the real world. There is no concept called GameTimeSeconds.
    /// </summary>
    /// <remarks>
    /// DEFINITIONS
    /// GameInstance - an instance of a new or saved game. A saved game that is later resumed is the
    /// same GameInstance although it can be running in a different UnitySession. Multiple GameInstances can be
    /// started, run, saved or resumed in a single UnitySession. 
    /// UnitySession - In a standalone player, a session starts when the player starts. 
    /// In the editor, a session starts when the Editor Play button is pushed.
    /// </remarks>
    //[SerializeAll]
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
        public float GameSpeedAdjustedHoursPerSecond { get { return HoursPerSecond * GameSpeedMultiplier; } }

        public float GameSpeedMultiplier { get; private set; }

        private GameSpeed _gameSpeed;
        public GameSpeed GameSpeed {
            get { return _gameSpeed; }
            set { SetProperty<GameSpeed>(ref _gameSpeed, value, "GameSpeed", OnGameSpeedChanged, OnGameSpeedChanging); }
        }

        #region GameSpeed Adjusted DeltaTime Archive

        /********************************************************************************************************************************
                    * Removed as I was erroneously using both GameSpeedAdjustedHoursPerSecond and these deltaTimes in same coroutines
                    ********************************************************************************************************************************/

        /// <summary>
        /// The number of seconds elapsed, adjusted for GameSpeed, since the last Frame 
        /// was rendered or zero if the game is paused. Useful for animations
        /// or other work that should reflect GameSpeed and stop while paused.
        /// </summary>
        //public float GameSpeedAdjustedDeltaTimeOrPaused {
        //    get {
        //        //WarnIfGameInstanceNotRunning();   // Jobs can call this even when IsRunning = false when launching new game within old game
        //        if (_gameMgr.IsPaused) {
        //            return Constants.ZeroF;
        //        }
        //        return GameSpeedAdjustedDeltaTime;
        //    }
        //}

        ///// <summary>
        ///// The number of seconds elapsed, adjusted for GameSpeed, since the last Frame 
        ///// was rendered whether the game is paused or not. Useful for animations or other
        ///// work that should reflect GameSpeed and continue even when the game is paused.
        ///// </summary>
        //public float GameSpeedAdjustedDeltaTime {
        //    get {
        //        WarnIfGameInstanceNotRunning();
        //        return DeltaTime * GameSpeedMultiplier;
        //    }
        //}

        #endregion

        /// <summary>
        /// The number of seconds elapsed since the last Frame 
        /// was rendered or zero if the game is paused. Useful for animations
        /// or other work that should stop while paused.
        /// </summary>
        public float DeltaTimeOrPaused {
            get {
                WarnIfGameInstanceNotRunning();
                if (_gameMgr.IsPaused) {
                    return Constants.ZeroF;
                }
                return DeltaTime;
            }
        }

        /// <summary>
        /// The number of seconds elapsed since the last Frame was rendered whether the 
        /// game is paused or not. Useful for animations or other work that should continue even when the game is paused.
        /// </summary>
        public float DeltaTime { get { return Time.deltaTime; } }

        /// <summary>
        /// The number of seconds since this current UnitySession started, aka UnityEngine.Time.time. 
        /// In a standalone player, this is the time since the player was started. In the editor, this is the 
        /// time since the Editor Play button was pushed. GameSpeed has no effect.
        /// </summary>
        public float CurrentUnitySessionTime { get { return Time.time; } }

        /// <summary>
        /// The number of seconds since a GameInstance originally began in this or prior UnitySessions. 
        /// Any time spent paused while playing the GameInstance is included. GameSpeed is not factored in.
        /// A typical use would be to track and report the total amount of real world time a user has spent 
        /// playing this GameInstance, even when paused.
        /// </summary>
        public float GameInstanceTime {
            get {
                WarnIfGameInstanceNotRunning();
                return _cumGameInstanceTimeInPriorUnitySessions + CurrentUnitySessionTime - _currentUnitySessionTimeWhenGameInstanceBegan;
            }
        }

        /// <summary>
        /// The number of seconds since a GameInstance originally began in this or prior UnitySessions. 
        /// Any time spent paused while playing the GameInstance is NOT included. GameSpeed is not factored in.
        /// A typical use would be to track and report the total amount of real world time a user has spent 
        /// ACTIVELY (not paused) playing this GameInstance.
        /// </summary>
        public float GameInstancePlayTime {
            get {
                WarnIfGameInstanceNotRunning();
                float gameInstanceTimeSpentInCurrentPause = Constants.ZeroF;
                if (_gameMgr.IsPaused) {
                    gameInstanceTimeSpentInCurrentPause = GameInstanceTime - _gameInstanceTimeCurrentPauseBegan;
                }
                return GameInstanceTime - _cumGameInstanceTimePaused - gameInstanceTimeSpentInCurrentPause;
            }
        }

        private GameDate _currentDate;
        /// <summary>
        /// The current GameDate in this game instance. Takes into account both game speed changes and pauses.
        /// </summary>
        public GameDate CurrentDate {
            get {
                WarnIfGameInstanceNotRunning();
                CheckForDateChange();
                return _currentDate;
            }
            private set { _currentDate = value; }
        }

        public void CheckForDateChange() {
            if (_gameMgr.IsRunning && !_gameMgr.IsPaused) {
                RefreshCurrentDateTime();

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
        }

        private IList<IDisposable> _subscriptions;

        /// <summary>
        /// The number of seconds accummulated by a saved GameInstance in UnitySessions prior to this one.
        /// This value is saved when a GameInstance is saved.
        /// </summary>
        private float _cumGameInstanceTimeInPriorUnitySessions;

        /// <summary>
        /// A marker indicating the point in time in this UnitySession that the current GameInstance began.
        /// </summary>
        private float _currentUnitySessionTimeWhenGameInstanceBegan;

        /// <summary>
        /// The accummulated number of seconds this new or saved GameInstance has spent paused 
        /// since it originally began in this or prior UnitySessions. This value is saved when a GameInstance is saved.
        /// </summary>
        private float _cumGameInstanceTimePaused;

        /// <summary>
        /// A marker indicating the GameInstanceTime in the current UnitySession the current paused state began. 
        /// Allows _cumGameInstanceTimePaused to be calculated when exiting a paused state in the current UnitySession.
        /// </summary>
        private float _gameInstanceTimeCurrentPauseBegan;

        /// <summary>
        /// The number of seconds this GameInstance has been running since it was started. 
        /// Accounts for changes in gameSpeed and pausing as it is used to calculate the CurrentDate.
        /// </summary>
        private float _currentDateTime;
        private float __savedCurrentDateTime;    // FIXME required to save currentDateTime and then restore it. A bug?

        /// <summary>
        ///A marker indicating the GameInstancePlayTime value when the last CurrentDateTimeRefresh was executed.
        /// </summary>
        private float _gameInstancePlayTimeAtLastCurrentDateTimeRefresh;
        private PlayerPrefsManager _playerPrefsMgr;
        private IGameManager _gameMgr;

        private GameTime() {
            Initialize();
        }

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        protected override void Initialize() {
            UnityEngine.Time.timeScale = Constants.OneF;
            _gameMgr = References.GameManager;
            _playerPrefsMgr = PlayerPrefsManager.Instance;
            PrepareToBeginNewGame();
            Subscribe();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_gameMgr.SubscribeToPropertyChanging<IGameManager, bool>(gm => gm.IsPaused, OnIsPausedChanging));
            _subscriptions.Add(_gameMgr.SubscribeToPropertyChanged<IGameManager, bool>(gm => gm.IsRunning, OnIsRunningChanged));
        }

        private void OnIsRunningChanged() {
            //D.Log("{0}.OnIsRunningChanged() called. IsRunning = {1}.", GetType().Name, _gameMgr.IsRunning);
            if (_gameMgr.IsRunning) {
                D.Assert(!_gameMgr.IsPaused);    // my practice - set IsRunning to true, THEN pause when isPauseOnLoad option enabled
                _currentUnitySessionTimeWhenGameInstanceBegan = CurrentUnitySessionTime;
                _gameInstancePlayTimeAtLastCurrentDateTimeRefresh = GameInstancePlayTime;
                // no reason to call RefreshCurrentDateTime here as it will get called as soon as CurrentDate is requested
            }
        }

        private void OnIsPausedChanging(bool isPausing) {
            if (_gameMgr.IsRunning) {
                if (isPausing) {
                    // we are about to pause
                    RefreshCurrentDateTime();   // refresh CurrentDateTime before pausing
                    _gameInstanceTimeCurrentPauseBegan = GameInstanceTime;
                }
                else {
                    // we are about to resume play
                    float gameInstanceTimeInCurrentPause = GameInstanceTime - _gameInstanceTimeCurrentPauseBegan;
                    _cumGameInstanceTimePaused += gameInstanceTimeInCurrentPause;
                    //D.Log("CurrentUnitySessionTimeGameInstanceBegan = {0:0.00}, GameInstanceTimeCurrentPauseBegan = {1:0.00}", _currentUnitySessionTimeWhenGameInstanceBegan, _gameInstanceTimeCurrentPauseBegan);
                    //D.Log("GameInstanceTimeInCurrentPause = {0:0.00}, GameInstanceTime = {1:0.00}.", gameInstanceTimeInCurrentPause, GameInstanceTime);
                    _gameInstanceTimeCurrentPauseBegan = Constants.ZeroF;
                }
            }
        }

        public void PrepareToBeginNewGame() {
            D.Log("{0}.PrepareToBeginNewGame() called.", GetType().Name);
            _cumGameInstanceTimeInPriorUnitySessions = Constants.ZeroF;
            _currentUnitySessionTimeWhenGameInstanceBegan = Constants.ZeroF;
            _cumGameInstanceTimePaused = Constants.ZeroF;
            _gameInstanceTimeCurrentPauseBegan = Constants.ZeroF;
            _gameInstancePlayTimeAtLastCurrentDateTimeRefresh = Constants.ZeroF;
            _currentDateTime = Constants.ZeroF;
            __savedCurrentDateTime = Constants.ZeroF;
            // don't wait for the Gui to set GameSpeed. Use the backing field as the Property calls OnGameSpeedChanged()
            _gameSpeed = _playerPrefsMgr.GameSpeedOnLoad;
            GameSpeedMultiplier = _gameSpeed.SpeedMultiplier();
            // no need to assign a new CurrentDate as the change to _currentDateTime results in a new, synched CurrentDate instance once Date is requested
            // onDateChanged = null;   // new subscribers tend to subscribe on Awake, but nulling the list here clears it. All previous subscribers need to unsubscribe!
        }

        public void PrepareToSaveGame() {
            // _currentUnitySessionTimeWhenGameInstanceBegan will be set to a new value the next time a GameInstance begins running
            _cumGameInstanceTimePaused += GameInstanceTime - _gameInstanceTimeCurrentPauseBegan; // _cumGameInstanceTimePaused must be updated now so it is current when saved
            // _gameInstanceTimeCurrentPauseBegan will be set to a new value the next time a pause begins
            // _gameInstancePlayTimeAtLastCurrentDateTimeRefresh is not important to save as it is constantly kept current
            // currentDateTime is key! It should be accurate as it gets constantly refreshed       
            D.Log("{0}.currentDateTime value being saved is {1:0.00}.", GetType().Name, _currentDateTime);
            __savedCurrentDateTime = _currentDateTime; // FIXME bug? currentDateTime does not get properly restored
            _cumGameInstanceTimeInPriorUnitySessions = GameInstanceTime; // _cumGameInstanceTimeInPriorUnitySessions must be updated (last so it doesn't affect other values here) so it is current when saved
            D.Log("{0}.PrepareToSaveGame called. CumGameInstanceTimeInPriorUnitySessions set to {1:0.##}.", GetType().Name, _cumGameInstanceTimeInPriorUnitySessions);
        }

        public void PrepareToResumeSavedGame() {
            // _cumGameInstanceTimeInPriorUnitySessions was updated before saving, so it should be restored to the right value
            D.Log("GameTime.PrepareToResumeSavedGame() called. CumGameInstanceTimeInPriorUnitySessions restored to {0:0.0)}.", _cumGameInstanceTimeInPriorUnitySessions);
            // _currentUnitySessionTimeWhenGameInstanceBegan that was saved is irrelevant. It will be updated when the resumed GameInstance begins running
            // _cumGameInstanceTimePaused was updated before saving, so it should be restored to the right value
            // _gameInstanceTimeCurrentPauseBegan will be set to a new value on the next pause
            // _gameInstancePlayTimeAtLastCurrentDateTimeRefresh will be reset when the resumed GameInstance begins running

            // currentDateTime is key! It value when restored should be accurate as it is kept current up to the point it is saved
            _currentDateTime = __savedCurrentDateTime; // FIXME bug? currentDateTime does not get properly restored
            D.Log("CurrentDateTime restored to {0:0.00}.", _currentDateTime);
            // don't wait for the Gui to set GameSpeed. Use the backing field as the Property calls OnGameSpeedChanged()
            _gameSpeed = _playerPrefsMgr.GameSpeedOnLoad; // the GameSpeed when saved is not relevant to the resumed GameInstance
            GameSpeedMultiplier = _gameSpeed.SpeedMultiplier();
            // date that is saved is fine and should be accurate. It gets recalculated from currentDateTime everytime it is used
            // the list of subscribers to onDateChanged should be fine as saved
        }

        private void OnGameSpeedChanging(GameSpeed proposedSpeed) {
            RefreshCurrentDateTime();
        }

        private void OnGameSpeedChanged() {
            GameSpeedMultiplier = GameSpeed.SpeedMultiplier();
        }

        /// <summary>
        /// Brings _currentDateTime up to date. While the Date only needs to be refreshed when it is about to be used,
        ///_currentDateTime must keep track of accummulated pauses and game speed changes. It is not necessary to 
        /// refresh the date when a pause or speed change occurs as they don't have any use for the date.
        /// </summary>
        private void RefreshCurrentDateTime() {
            D.Assert(_gameMgr.IsRunning);
            D.Assert(!_gameMgr.IsPaused);   // it keeps adding to currentDateTime
            _currentDateTime += GameSpeedMultiplier * (GameInstancePlayTime - _gameInstancePlayTimeAtLastCurrentDateTimeRefresh);
            _gameInstancePlayTimeAtLastCurrentDateTimeRefresh = GameInstancePlayTime;
            //D.Log("{0}.CurrentDateTime refreshed to {1:0.00}.", GetType().Name, _currentDateTime);
        }

        private void WarnIfGameInstanceNotRunning() {
            D.Warn(!_gameMgr.IsRunning, "{0} reports {1} should have a GameInstance running.", GetType().Name, typeof(IGameManager).Name);
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


