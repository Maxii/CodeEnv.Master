// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerResearchManager.cs
// Manages the progression of research on technologies for a Player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Manages the progression of research on technologies for a Player.
    /// <remarks>Access via PlayerAIManager.</remarks>
    /// </summary>
    public class PlayerResearchManager : IRecurringDateMinderClient, IDisposable {

        private const string DebugNameFormat = "{0}.{1}";

        public event EventHandler currentResearchChanged;

        public event EventHandler<ResearchCompletedEventArgs> researchCompleted;

        public string DebugName { get { return DebugNameFormat.Inject(_aiMgr.Player.DebugName, GetType().Name); } }

        private ResearchTask _currentResearchTask = TempGameValues.NoResearch;
        public ResearchTask CurrentResearchTask {
            get { return _currentResearchTask; }
            private set { _currentResearchTask = value; }
        }

        private IDictionary<Technology, ResearchTask> _uncompletedResearchTaskLookup;
        private HashSet<Technology> _completedTechnologies;
        private float _unconsumedScienceYield;
        private float _playerTotalScienceYield;

        private DateMinderDuration _researchUpdateDuration;
        private PlayerAIManager _aiMgr;
        private GameTime _gameTime;
        private IList<IDisposable> _subscriptions;

        public PlayerResearchManager(PlayerAIManager aiMgr) {
            _aiMgr = aiMgr;
            InitializeValuesAndReferences();
        }

        private void InitializeValuesAndReferences() {
            _gameTime = GameTime.Instance;
            _completedTechnologies = new HashSet<Technology>();
            _uncompletedResearchTaskLookup = new Dictionary<Technology, ResearchTask>();

            var allTechs = TechnologyFactory.Instance.GetAllTechs(_aiMgr.Player);
            foreach (var tech in allTechs) {
                _uncompletedResearchTaskLookup.Add(tech, new ResearchTask(tech));
            }
            var startingTech = TechnologyFactory.Instance.__GetStartingTech(_aiMgr.Player);
            CurrentResearchTask = _uncompletedResearchTaskLookup[startingTech];
        }

        public void InitiateProgressChecks() {
            // OPTIMIZE 2.27.18 Updating progress every hour is expensive
            _researchUpdateDuration = new DateMinderDuration(GameTimeDuration.OneHour, this);
            _gameTime.RecurringDateMinder.Add(_researchUpdateDuration);
            RefreshTotalScienceYield();
            Subscribe();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_aiMgr.Knowledge.SubscribeToPropertyChanged<PlayerKnowledge, float>(pk => pk.TotalScienceYield, PlayerTotalScienceYieldPropChangedHandler));
        }

        public bool IsCompleted(Technology tech) {
            return _completedTechnologies.Contains(tech);
        }

        public ResearchTask GetUncompletedResearchTaskFor(Technology tech) {
            return _uncompletedResearchTaskLookup[tech];
        }

        public void ChangeCurrentResearchTo(Technology tech) {
            __ValidatePausedState();
            D.Assert(!IsCompleted(tech));

            CurrentResearchTask = _uncompletedResearchTaskLookup[tech];
            OnCurrentResearchChanged();
        }

        private void ProgressResearch(float availableScienceYield) {
            D.Assert(!GameReferences.GameManager.IsPaused);
            D.AssertNotEqual(TempGameValues.NoResearch, CurrentResearchTask);
            Technology tech = CurrentResearchTask.TechBeingResearched;
            //D.Log("{0} is checking {1} for completion on {2}.", DebugName, CurrentResearchTask.DebugName, _gameTime.CurrentDate);
            if (CurrentResearchTask.TryComplete(availableScienceYield, out _unconsumedScienceYield)) {
                D.Log("{0} has completed research of {1} on {2}. Science/hour = {3:0.}.",
                    DebugName, CurrentResearchTask.DebugName, _gameTime.CurrentDate, _playerTotalScienceYield);
                D.Assert(CurrentResearchTask.IsCompleted);
                var completedResearch = CurrentResearchTask;
                CurrentResearchTask = TempGameValues.NoResearch;
                bool isRemoved = _uncompletedResearchTaskLookup.Remove(tech);
                D.Assert(isRemoved);
                bool isAdded = _completedTechnologies.Add(tech);
                D.Assert(isAdded);

                OnResearchCompleted(completedResearch);
                //__ValidatePausedState();    // 2.27.18 This can be used here once I implement rqmt that user select tech from ResearchScreen
            }
        }

        private void UpdateTimeToCompletion() {
            foreach (var researchTask in _uncompletedResearchTaskLookup.Values) {
                float additionalScienceReqdToComplete = researchTask.CostToResearch - researchTask.CumScienceApplied;
                float remainingHoursToComplete;
                if (researchTask == CurrentResearchTask) {   // IMPROVE
                    if (additionalScienceReqdToComplete <= _playerTotalScienceYield + _unconsumedScienceYield) {
                        remainingHoursToComplete = Constants.OneF;
                    }
                    else {
                        remainingHoursToComplete = Constants.OneF + additionalScienceReqdToComplete / _playerTotalScienceYield;
                    }
                }
                else {
                    remainingHoursToComplete = additionalScienceReqdToComplete / _playerTotalScienceYield;
                }
                GameTimeDuration remainingDurationToComplete = new GameTimeDuration(remainingHoursToComplete);
                if (remainingDurationToComplete != researchTask.TimeToComplete) {
                    //D.Log("{0}: {1}'s TimeToComplete changed from {2} to {3}.", DebugName, researchTask.DebugName, researchTask.TimeToComplete,
                    //remainingDurationToComplete);
                    researchTask.TimeToComplete = remainingDurationToComplete;
                }
            }
        }

        #region Event and Property Change Handlers

        private void PlayerTotalScienceYieldPropChangedHandler() {
            RefreshTotalScienceYield();
            UpdateTimeToCompletion();
        }

        private void OnCurrentResearchChanged() {
            if (currentResearchChanged != null) {
                currentResearchChanged(this, EventArgs.Empty);
            }
        }

        private void OnResearchCompleted(ResearchTask completedResearch) {
            if (researchCompleted != null) {
                researchCompleted(this, new ResearchCompletedEventArgs(completedResearch));
            }
        }

        #endregion

        public void HandlePlayerLost() {
            _gameTime.RecurringDateMinder.Remove(_researchUpdateDuration);
        }

        private void RefreshTotalScienceYield() {
            var playerTotalScienceYield = _aiMgr.Knowledge.TotalScienceYield;
            D.AssertNotEqual(Constants.ZeroF, playerTotalScienceYield);
            _playerTotalScienceYield = playerTotalScienceYield;
        }

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            _subscriptions.ForAll(d => d.Dispose());
            _subscriptions.Clear();
        }

        public override string ToString() {
            return DebugName;
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void __ValidatePausedState() {
            if (_aiMgr.Player.IsUser) {
                D.Assert(GameReferences.GameManager.IsPaused);
            }
        }

        #endregion

        #region Nested Classes

        public class ResearchCompletedEventArgs : EventArgs {

            public ResearchTask CompletedResearch { get; private set; }

            public ResearchCompletedEventArgs(ResearchTask completedResearch) {
                CompletedResearch = completedResearch;
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
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion

        #region IRecurringDateMinderClient Members

        public void HandleDateReached(DateMinderDuration recurringDuration) {
            D.AssertEqual(_researchUpdateDuration, recurringDuration);
            ProgressResearch(_playerTotalScienceYield + _unconsumedScienceYield);
        }

        #endregion


    }
}

