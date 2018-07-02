// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerQueuedResearchManager.cs
// A research manager for a player patterned after the ConstructionManager which uses a queue of ConstructionTasks.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// A research manager for a player patterned after the ConstructionManager which uses a 
    /// queue of ConstructionTasks.
    /// </summary>
    [Obsolete("Use PlayerResearchManager instead.")]
    public class PlayerQueuedResearchManager : IRecurringDateMinderClient, IDisposable {

        private const string DebugNameFormat = "{0}.{1}";

        public event EventHandler researchQueueChanged;

        public event EventHandler<ResearchCompletedEventArgs> researchCompleted;

        public string DebugName { get { return DebugNameFormat.Inject(_aiMgr.Owner.DebugName, GetType().Name); } }

        private float _playerTotalScienceYield;
        private DateMinderDuration _researchQueueUpdateDuration;
        private LinkedList<QueuedResearchTask> _researchQueue;
        private PlayerAIManager _aiMgr;
        private GameTime _gameTime;
        private IList<IDisposable> _subscriptions;

        public PlayerQueuedResearchManager(PlayerAIManager aiMgr) {
            _aiMgr = aiMgr;
            _gameTime = GameTime.Instance;
            _researchQueue = new LinkedList<QueuedResearchTask>();
        }

        public void InitiateProgressChecks() {
            // OPTIMIZE 9.24.17 Updating construction progress every hour is expensive. Could only apply production 
            // when an expectedCompletionDate is reached using DateMinder. However, would need to accumulate production since
            // last applied. Could be done by recording last date applied and calc accumulated production when needed to 
            // apply. Would also need to remove/add dates to DateMinder whenever CompletionDates changed.
            _researchQueueUpdateDuration = new DateMinderDuration(GameTimeDuration.OneHour, this);
            _gameTime.RecurringDateMinder.Add(_researchQueueUpdateDuration);
            RefreshTotalScienceYield();
            Subscribe();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_aiMgr.Knowledge.SubscribeToPropertyChanged<PlayerKnowledge, float>(pk => pk.TotalScienceYield, PlayerTotalScienceYieldPropChangedHandler));
        }

        /// <summary>
        /// Adds the provided item to the ConstructionQueue for refitting to <c>refitDesign</c>.
        /// </summary>
        /// <param name="refitDesign">The design to refit too.</param>
        /// <param name="item">The item to be refitted.</param>
        /// <param name="researchCost">The refit cost.</param>
        /// <returns></returns>
        public QueuedResearchTask AddToQueue(Technology technologyToResearch) {
            var researchTask = new QueuedResearchTask(technologyToResearch);

            var expectedStartDate = _researchQueue.Any() ? _researchQueue.Last.Value.ExpectedCompletionDate : _gameTime.CurrentDate;
            float researchCost = researchTask.CostToResearch;
            GameTimeDuration expectedResearchDuration = new GameTimeDuration(researchCost / _playerTotalScienceYield);
            GameDate expectedCompletionDate = new GameDate(expectedStartDate, expectedResearchDuration);
            researchTask.ExpectedCompletionDate = expectedCompletionDate;

            _researchQueue.AddLast(researchTask);
            RefreshCurrentResearchProperty();
            // Adding to the end of the queue does not change any expected completion dates
            OnResearchQueueChanged();
            return researchTask;
        }

        [Obsolete("Not currently used")]
        public void MoveQueuedResearchAfter(QueuedResearchTask researchBefore, QueuedResearchTask researchMoving) {
            D.AssertNotEqual(researchBefore, researchMoving);
            var beforeItemNode = _researchQueue.Find(researchBefore);
            bool isRemoved = _researchQueue.Remove(researchMoving);
            D.Assert(isRemoved);
            _researchQueue.AddAfter(beforeItemNode, researchMoving);
            RefreshCurrentResearchProperty();
            UpdateExpectedCompletionDates();
            OnResearchQueueChanged();
        }

        [Obsolete("Not currently used")]
        public void MoveQueuedResearchBefore(QueuedResearchTask researchAfter, QueuedResearchTask researchMoving) {
            D.AssertNotEqual(researchAfter, researchMoving);
            var afterItemNode = _researchQueue.Find(researchAfter);
            bool isRemoved = _researchQueue.Remove(researchMoving);
            D.Assert(isRemoved);
            _researchQueue.AddBefore(afterItemNode, researchMoving);
            RefreshCurrentResearchProperty();
            UpdateExpectedCompletionDates();
            OnResearchQueueChanged();
        }

        public void RegenerateQueue(IList<QueuedResearchTask> orderedQueue) {
            _researchQueue.Clear();
            foreach (var research in orderedQueue) {
                _researchQueue.AddLast(research);
            }
            RefreshCurrentResearchProperty();
            UpdateExpectedCompletionDates();
            OnResearchQueueChanged();
        }

        public void RemoveFromQueue(QueuedResearchTask researchTask) {
            bool isRemoved = _researchQueue.Remove(researchTask);
            D.Assert(isRemoved);
            UpdateExpectedCompletionDates();
            RefreshCurrentResearchProperty();
            if (!researchTask.IsCompleted) {
                //_aiMgr.HandleUncompletedResearchRemovedFromQueue(researchTask);
                if (researchTask.CompletionPercentage > Constants.ZeroPercent) {
                    __HandlePartiallyCompletedResearchBeingRemovedFromQueue(researchTask);
                }
            }
            OnResearchQueueChanged();
        }

        public IList<QueuedResearchTask> GetQueue() {
            return new List<QueuedResearchTask>(_researchQueue);
        }

        private void ProgressResearch(float availableScienceYield) {
            D.Assert(!GameReferences.GameManager.IsPaused);
            if (_researchQueue.Any()) {
                var firstResearchTask = _researchQueue.First.Value;
                D.Log("{0} is checking {1} for completion. ExpectedCompletionDate = {2}, CurrentDate = {3}.",
                    DebugName, firstResearchTask.DebugName, firstResearchTask.ExpectedCompletionDate, _gameTime.CurrentDate);
                float unconsumedScienceYield;
                if (firstResearchTask.TryComplete(availableScienceYield, out unconsumedScienceYield)) {
                    D.Log("{0} has completed research of {1}. ExpectedCompletionDate = {2}, CurrentDate = {3}.",
                        DebugName, firstResearchTask.DebugName, firstResearchTask.ExpectedCompletionDate, _gameTime.CurrentDate);
                    RemoveFromQueue(firstResearchTask);
                    //_aiMgr.HandleResearchCompleted(firstResearchTask);
                    OnResearchCompleted(firstResearchTask);
                    ProgressResearch(unconsumedScienceYield);
                }
            }
        }

        private void UpdateExpectedCompletionDates() {
            if (_researchQueue.Any()) {
                GameDate dateResearchResumes = _gameTime.CurrentDate;
                foreach (var researchTask in _researchQueue) {
                    float additionalScienceReqd = researchTask.CostToResearch - researchTask.CumScienceApplied;
                    float remainingHoursToCompletion = additionalScienceReqd / _playerTotalScienceYield;
                    GameTimeDuration remainingDurationToCompletion = new GameTimeDuration(remainingHoursToCompletion);
                    GameDate updatedCompletionDate = new GameDate(dateResearchResumes, remainingDurationToCompletion);
                    if (updatedCompletionDate != researchTask.ExpectedCompletionDate) {
                        D.Log("{0}: {1}'s CompletionDate changed from {2} to {3}.",
                            DebugName, researchTask.DebugName, researchTask.ExpectedCompletionDate, updatedCompletionDate);
                        researchTask.ExpectedCompletionDate = updatedCompletionDate;
                    }
                    dateResearchResumes = researchTask.ExpectedCompletionDate;
                }
            }
        }

        #region Event and Property Change Handlers

        private void PlayerTotalScienceYieldPropChangedHandler() {
            RefreshTotalScienceYield();
            UpdateExpectedCompletionDates();
        }

        private void OnResearchQueueChanged() {
            if (researchQueueChanged != null) {
                researchQueueChanged(this, EventArgs.Empty);
            }
        }

        private void OnResearchCompleted(QueuedResearchTask completedResearch) {
            if (researchCompleted != null) {
                researchCompleted(this, new ResearchCompletedEventArgs(completedResearch));
            }
        }

        #endregion

        public void HandlePlayerLost() {
            RemoveAllResearchInQueue();
            _gameTime.RecurringDateMinder.Remove(_researchQueueUpdateDuration);
        }


        private void RemoveAllResearchInQueue() {
            var cQueueCopy = new List<QueuedResearchTask>(_researchQueue);
            foreach (var c in cQueueCopy) {
                RemoveFromQueue(c);
            }
        }

        private void RefreshCurrentResearchProperty() {
            ////var currentResearch = _researchQueue.Any() ? _researchQueue.First.Value : TempGameValues.NoQueuedResearch;
            ////_aiMgr.CurrentResearchTask = currentResearch;
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

        private void __HandlePartiallyCompletedResearchBeingRemovedFromQueue(QueuedResearchTask partiallyCompletedResearchTask) {

        }

        #endregion

        #region Nested Classes

        public class ResearchCompletedEventArgs : EventArgs {

            public QueuedResearchTask CompletedResearch { get; private set; }

            public ResearchCompletedEventArgs(QueuedResearchTask completedResearch) {
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
            D.AssertEqual(_researchQueueUpdateDuration, recurringDuration);
            ProgressResearch(_playerTotalScienceYield);
        }

        #endregion


    }
}

