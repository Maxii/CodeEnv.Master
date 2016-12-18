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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;
    using UnityEngine.Assertions;

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

        /// <summary>
        /// The maximum precision used by Hours in the game.
        /// <remarks>0.1 hour tolerance is about the best I can expect at an FPS as low as 25 
        /// (0.04 secs between updates to GameTime, and Time.time is defined as the time at the beginning
        /// of the frame and will not change until the next frame starts) and GameSettings.HoursPerSecond of 2.0
        /// => 0.04 * 2.0 = 0.08 hours tolerance. Granularity will be better at higher FPS, 
        /// but I can't count on it.</remarks>
        /// </summary>
        public const float HoursPrecision = 0.1F;

        /// <summary>
        /// The fixed time step setting of the game in seconds per fixed update.
        /// <remarks>Equivalent to 50 Physics calculation periods per second.</remarks>
        /// </summary>
        public const float FixedTimestep = 0.02F;

        /// <summary>
        /// The multiplier and divider used to convert hours as a float to GameTime Hours 
        /// with the proper precision. The inverse of HoursPrecision.
        /// </summary>
        public const float HoursConversionFactor = 1F / HoursPrecision;

        public static readonly int HoursPerDay = GeneralSettings.Instance.HoursPerDay;
        public static readonly int DaysPerYear = GeneralSettings.Instance.DaysPerYear;
        /// <summary>
        /// The number of GameHours in a Second at a GameSpeedMultiplier of 1 (aka GameSpeed.Normal).
        /// <remarks>12.12.16 ~ 2 hours per second.</remarks>
        /// </summary>
        public static readonly float HoursPerSecond = GeneralSettings.Instance.HoursPerSecond;
        public static readonly int GameStartYear = GeneralSettings.Instance.GameStartYear;
        public static readonly int GameEndYear = GeneralSettings.Instance.GameEndYear;

        public static readonly GameDate GameStartDate = new GameDate(Constants.ZeroF, Constants.Zero, GameStartYear);    // 2700.000.00.0
        public static readonly GameDate GameEndDate = new GameDate(HoursPerDay - HoursPrecision, DaysPerYear - 1, GameEndYear);    // 8999.099.19.9

        #endregion

        #region Static Methods

        /// <summary>
        /// Validates the provided hours value is no more precise than GameConstants.HoursPrecision.
        /// </summary>
        /// <param name="hours">The hours.</param>
        public static void ValidateHoursValue(float hours) {
            float convertedHours = ConvertHoursValue(hours);
            //D.Log("{0:0.000} validating against {1:0.000}.", hours, convertedHours);
            if (!Mathfx.Approx(convertedHours, hours, UnityConstants.FloatEqualityPrecision)) {
                D.Error("Hours: {0:0.000000} != ConvertedHours: {1:0.000000}.", hours, convertedHours);
            }
        }

        /// <summary>
        /// Converts the provided hours value to the precision used by hours in dates and durations.
        /// </summary>
        /// <param name="hours">The hours value to convert.</param>
        /// <returns></returns>
        public static float ConvertHoursValue(float hours) {
            float convertedHours = Mathf.Round(hours * HoursConversionFactor) / HoursConversionFactor;
            //D.Log("{0:0.000} hours converted to {1:0.000}", hours, convertedHours);
            return convertedHours;
        }

        #endregion

        /// <summary>
        /// Occurs when the date changes that a calender display would care about.
        /// <remarks>3.25.16 Fires when the hour digit changes.</remarks>
        /// <remarks>11.15.16 Memory allocation (~1K) from string and Inject() occurs when this fires.</remarks>
        /// </summary>
        public event EventHandler calenderDateChanged;

        /// <summary>
        /// The number of Hours passing per second, adjusted for GameSpeed.
        /// </summary>
        public float GameSpeedAdjustedHoursPerSecond { get { return HoursPerSecond * GameSpeedMultiplier; } }

        private float _gameSpeedMultiplier;
        public float GameSpeedMultiplier {
            get {
                D.Assert(_gameSpeedMultiplier > Constants.ZeroF);
                return _gameSpeedMultiplier;
            }
            set { SetProperty<float>(ref _gameSpeedMultiplier, value, "GameSpeedMultiplier"); }
        }

        private GameSpeed _gameSpeed;
        public GameSpeed GameSpeed {
            get {
                D.AssertNotDefault((int)_gameSpeed);
                return _gameSpeed;
            }
            set { SetProperty<GameSpeed>(ref _gameSpeed, value, "GameSpeed", GameSpeedPropChangedHandler, GameSpeedPropChangingHandler); }
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
        [Obsolete]
        public float DeltaTimeOrPaused {
            get {
                __WarnIfGameNotRunning();
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
        /// <remarks>Derived from Time.time which is the seconds value at the beginning of a frame. This value 
        /// DOES NOT CHANGE within a frame, which means with an FPS rate of 25, accuracy is no better than 0.04 seconds.</remarks>
        /// IMPROVE Use .Net System.DateTime.UTCNow?
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
                __WarnIfGameNotRunning();
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
                __WarnIfGameNotRunning();
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
        /// <remarks>Not subscribable.</remarks>
        /// </summary>
        public GameDate CurrentDate {
            get {
                __WarnIfGameNotRunning();
                return _currentDate;
            }
            private set { _currentDate = value; }
        }

        /// <summary>
        /// The number of seconds accumulated by a saved GameInstance in UnitySessions prior to this one.
        /// This value is saved when a GameInstance is saved.
        /// </summary>
        private float _cumGameInstanceTimeInPriorUnitySessions;

        /// <summary>
        /// A marker indicating the point in time in this UnitySession that the current GameInstance began.
        /// </summary>
        private float _currentUnitySessionTimeWhenGameInstanceBegan;

        /// <summary>
        /// The accumulated number of seconds this new or saved GameInstance has spent paused 
        /// since it originally began in this or prior UnitySessions. This value is saved when a GameInstance is saved.
        /// </summary>
        private float _cumGameInstanceTimePaused;

        /// <summary>
        /// A marker indicating the GameInstanceTime in the current UnitySession the current paused state began. 
        /// Allows _cumGameInstanceTimePaused to be calculated when exiting a paused state in the current UnitySession.
        /// </summary>
        private float _gameInstanceTimeCurrentPauseBegan;

        /// <summary>
        /// The number of "game" seconds this GameInstance has been running since it was started. 
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
        private IList<IDisposable> _subscriptions;

        private GameTime() {
            Initialize();
        }

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        protected sealed override void Initialize() {
            UnityEngine.Time.timeScale = Constants.OneF;
            if (HoursPerSecond * 1F / TempGameValues.MinimumFramerate > HoursPrecision) {
                D.Warn("See {0}.HoursPrecision notes above.", DebugName);
            }
            if (!FixedTimestep.ApproxEquals(Time.fixedDeltaTime)) {
                D.Warn("{0}: Time.fixedDeltaTime of {1} unexpected.", typeof(GameTime).Name, Time.fixedDeltaTime);
            }
            _gameMgr = References.GameManager;
            _playerPrefsMgr = PlayerPrefsManager.Instance;
            Subscribe();
            ////PrepareToBeginNewGame();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_gameMgr.SubscribeToPropertyChanging<IGameManager, bool>(gm => gm.IsPaused, IsPausedPropChangingHandler));
            _gameMgr.isReadyForPlayOneShot += IsReadyForPlayEventHandler;
        }

        public void PrepareToBeginNewGame() {
            _cumGameInstanceTimeInPriorUnitySessions = Constants.ZeroF;
            _currentUnitySessionTimeWhenGameInstanceBegan = Constants.ZeroF;
            _cumGameInstanceTimePaused = Constants.ZeroF;
            _gameInstanceTimeCurrentPauseBegan = Constants.ZeroF;
            _gameInstancePlayTimeAtLastCurrentDateTimeRefresh = Constants.ZeroF;
            _currentDateTime = Constants.ZeroF;
            __savedCurrentDateTime = Constants.ZeroF;
            // don't wait for the Gui to set GameSpeed. Use the backing field as the Property calls GameSpeedPropChangedHandler()
            _gameSpeed = _playerPrefsMgr.GameSpeedOnLoad;
            GameSpeedMultiplier = _gameSpeed.SpeedMultiplier();
            //// no need to assign a new CurrentDate as the change to _currentDateTime results in a new, synced CurrentDate instance once Date is requested
            //// onDateChanged = null;   // new subscribers tend to subscribe on Awake, but nulling the list here clears it. All previous subscribers need to unsubscribe!
            _currentDate = GameStartDate;   // 8.13.16 added as otherwise at the mercy of GameMgr calling CheckForDateChange() once IsRunning
            //D.Log("{0}.PrepareToBeginNewGame() finished. Frame {1}, UnityTime {2:0.0}, SystemTimeStamp {3}.", DebugName, Time.frameCount, Time.time, Utility.TimeStamp);
        }

        public void PrepareToSaveGame() {
            // _currentUnitySessionTimeWhenGameInstanceBegan will be set to a new value the next time a GameInstance begins running
            _cumGameInstanceTimePaused += GameInstanceTime - _gameInstanceTimeCurrentPauseBegan; // _cumGameInstanceTimePaused must be updated now so it is current when saved
            // _gameInstanceTimeCurrentPauseBegan will be set to a new value the next time a pause begins
            // _gameInstancePlayTimeAtLastCurrentDateTimeRefresh is not important to save as it is constantly kept current
            // currentDateTime is key! It should be accurate as it gets constantly refreshed       
            //D.Log("{0}.currentDateTime value being saved is {1:0.00}.", DebugName, _currentDateTime);
            __savedCurrentDateTime = _currentDateTime; // FIXME bug? currentDateTime does not get properly restored
            _cumGameInstanceTimeInPriorUnitySessions = GameInstanceTime; // _cumGameInstanceTimeInPriorUnitySessions must be updated (last so it doesn't affect other values here) so it is current when saved
            //D.Log("{0}.PrepareToSaveGame called. CumGameInstanceTimeInPriorUnitySessions set to {1:0.##}.", DebugName, _cumGameInstanceTimeInPriorUnitySessions);
        }

        public void PrepareToResumeSavedGame() {
            // _cumGameInstanceTimeInPriorUnitySessions was updated before saving, so it should be restored to the right value
            //D.Log("{0}.PrepareToResumeSavedGame() called. CumGameInstanceTimeInPriorUnitySessions restored to {1:0.0)}.", DebugName, _cumGameInstanceTimeInPriorUnitySessions);
            // _currentUnitySessionTimeWhenGameInstanceBegan that was saved is irrelevant. It will be updated when the resumed GameInstance begins running
            // _cumGameInstanceTimePaused was updated before saving, so it should be restored to the right value
            // _gameInstanceTimeCurrentPauseBegan will be set to a new value on the next pause
            // _gameInstancePlayTimeAtLastCurrentDateTimeRefresh will be reset when the resumed GameInstance begins running

            // currentDateTime is key! It value when restored should be accurate as it is kept current up to the point it is saved
            _currentDateTime = __savedCurrentDateTime; // FIXME bug? currentDateTime does not get properly restored
            //D.Log("{0} CurrentDateTime restored to {1:0.00}.", DebugName, _currentDateTime);
            // don't wait for the Gui to set GameSpeed. Use the backing field as the Property calls GameSpeedPropChangedHandler()
            _gameSpeed = _playerPrefsMgr.GameSpeedOnLoad; // the GameSpeed when saved is not relevant to the resumed GameInstance
            GameSpeedMultiplier = _gameSpeed.SpeedMultiplier();
            // date that is saved is fine and should be accurate. It gets recalculated from currentDateTime every time it is used
            // the list of subscribers to onDateChanged should be fine as saved
        }

        /// <summary>
        /// Checks for a change to the current date. Called by GameManager's Update
        /// method to keep the date accurate. Does nothing if the game is paused.
        /// </summary>
        public void CheckForDateChange() {
            if (_gameMgr.IsPaused) {
                return;
            }

            RefreshCurrentDateTime();

            bool toFireCalenderDateChangedEvent = false;
            bool toUpdateCurrentDate = false;

            var updatedDate = new GameDate(_currentDateTime);
            if (updatedDate > _currentDate) {
                // they are not within equivalence tolerance so update
                toUpdateCurrentDate = true;
            }

            if (!updatedDate.CalenderEquals(_currentDate)) {
                // Hours digit has changed so calender needs to hear about it
                toFireCalenderDateChangedEvent = true;
                // 3.26.16: CurrentDate needs to be updated as GuiDateReadout will acquire it to post.
                // A small change in hours from hourDigit.99 to hourDigitPlusOne.01 needs to fire a
                // calender date change event, but the two dates are within equivalence tolerance
                // and therefore won't update CurrentDate so we tell it to update just in case.
                toUpdateCurrentDate = true;
            }
            if (toUpdateCurrentDate) {
                //D.Log("{0}: Changing CurrentDate to {1}.", DebugName, updatedDate);
                CurrentDate = updatedDate;  // must be done before event fired
            }
            if (toFireCalenderDateChangedEvent) {
                OnCalenderDateChanged();    // 11.15.16 Appropriately causes 1K memory allocation
            }
        }

        #region Event and Property Change Handlers

        private void IsReadyForPlayEventHandler(object sender, EventArgs e) {
            D.Assert(_gameMgr.IsRunning);
            D.Assert(!_gameMgr.IsPaused);   // my practice - set IsRunning to true, fire isReadyForPlay, THEN pause when isPauseOnLoad option enabled
            _currentUnitySessionTimeWhenGameInstanceBegan = CurrentUnitySessionTime;
            _gameInstancePlayTimeAtLastCurrentDateTimeRefresh = GameInstancePlayTime;
            // no reason to call RefreshCurrentDateTime here as it will get called as soon as CurrentDate is requested
        }

        private void IsPausedPropChangingHandler(bool isPausing) {
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
                    //D.Log("{0} CurrentUnitySessionTimeGameInstanceBegan = {1:0.00}, GameInstanceTimeCurrentPauseBegan = {2:0.00}", DebugName, _currentUnitySessionTimeWhenGameInstanceBegan, _gameInstanceTimeCurrentPauseBegan);
                    //D.Log("{0} GameInstanceTimeInCurrentPause = {1:0.00}, GameInstanceTime = {2:0.00}.", DebugName, gameInstanceTimeInCurrentPause, GameInstanceTime);
                    _gameInstanceTimeCurrentPauseBegan = Constants.ZeroF;
                }
            }
        }

        private void GameSpeedPropChangingHandler(GameSpeed proposedSpeed) {
            if (!_gameMgr.IsPaused) {
                RefreshCurrentDateTime();
            }
        }

        private void GameSpeedPropChangedHandler() {
            GameSpeedMultiplier = GameSpeed.SpeedMultiplier();
        }

        private void OnCalenderDateChanged() {
            if (calenderDateChanged != null) {
                //string subscribers = calenderDateChanged.GetInvocationList().Select(d => d.Target.GetType().Name).Concatenate();
                //D.Log("{0}.calenderDateChanged. CalenderDate: {1}, Subscribers: {2}.", DebugName, _currentDate.CalenderFormattedDate, subscribers);
                calenderDateChanged(this, EventArgs.Empty);
            }
        }

        #endregion

        /// <summary>
        /// Generates a random date in the future. If called before the game is
        /// running, the date will be on or after the GameStartDate, otherwise it
        /// will be on or after the current date.
        /// </summary>
        /// <param name="maxDelay">The maximum delay after the start date.</param>
        /// <returns></returns>
        public GameDate GenerateRandomFutureDate(GameTimeDuration maxDelay) {
            GameDate startDate = _gameMgr.IsRunning ? CurrentDate : GameStartDate;
            float maxHoursDelayed = maxDelay.TotalInHours;
            float hoursDelayed = UnityEngine.Random.Range(Constants.ZeroF, maxHoursDelayed);
            return new GameDate(startDate, new GameTimeDuration(hoursDelayed));
        }


        /// <summary>
        /// Brings _currentDateTime up to date. While the Date only needs to be refreshed when it is about to be used,
        /// _currentDateTime must keep track of accumulated pauses and game speed changes. It is not necessary to 
        /// refresh the date when a pause or speed change occurs as they don't have any use for the date.
        /// </summary>
        private void RefreshCurrentDateTime() {
            D.Assert(_gameMgr.IsRunning);
            D.Assert(!_gameMgr.IsPaused);   // it keeps adding to currentDateTime
            float playTime = GameInstancePlayTime;
            float deltaGameInstancePlayTime = playTime - _gameInstancePlayTimeAtLastCurrentDateTimeRefresh;
            // 10.16.16 significant deltaTimes > 0.1 secs still occurring after the game time clock starts
            //D.Warn(deltaGameInstancePlayTime > HoursPrecision, "{0}.deltaGameInstancePlayTime increased by {1}.", DebugName, deltaGameInstancePlayTime);
            _currentDateTime += GameSpeedMultiplier * deltaGameInstancePlayTime;
            _gameInstancePlayTimeAtLastCurrentDateTimeRefresh = playTime;
            //D.Log("{0}.CurrentDateTime refreshed to {1:0.00}.", DebugName, _currentDateTime);
        }

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            _subscriptions.ForAll<IDisposable>(s => s.Dispose());
            _subscriptions.Clear();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug

        private void __WarnIfGameNotRunning() {
            if (!_gameMgr.IsRunning) {
                D.Warn("{0}: {1} should be running. Frame = {2}.", GetType().Name, typeof(IGameManager).Name, Time.frameCount);
            }
        }

        #endregion

        #region IDisposable

        private bool _alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {

            Dispose(true);

            // This object is being cleaned up by you explicitly calling Dispose() so take this object off
            // the finalization queue and prevent finalization code from 'disposing' a second time
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isExplicitlyDisposing) {
            if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
                D.Warn("{0} has already been disposed.", GetType().Name);
                return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
            }

            if (isExplicitlyDisposing) {
                // Dispose of managed resources here as you have called Dispose() explicitly
                Cleanup();
                CallOnDispose();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion

    }
}


