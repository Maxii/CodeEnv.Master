// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ConstructionManager.cs
// Manages element construction progress via a queue for a Base Unit.
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
    /// Manages element construction progress via a queue for a Base Unit.
    /// </summary>
    public class ConstructionManager : IRecurringDateMinderClient, IDisposable {

        private const string DebugNameFormat = "{0}.{1}";

        public event EventHandler constructionQueueChanged;

        public event EventHandler<ConstructionCompletedEventArgs> constructionCompleted;

        public string DebugName { get { return DebugNameFormat.Inject(_unitData.UnitName, GetType().Name); } }

        private DateMinderDuration _constructionQueueUpdateDuration;
        private LinkedList<ConstructionInfo> _constructionQueue;
        private AUnitBaseCmdData _unitData;
        private GameTime _gameTime;
        private IList<IDisposable> _subscriptions;

        public ConstructionManager(AUnitBaseCmdData data) {
            _unitData = data;
            _gameTime = GameTime.Instance;
            _constructionQueue = new LinkedList<ConstructionInfo>();
        }

        public void InitiateProgressChecks() {
            // OPTIMIZE 9.24.17 Updating construction progress every hour is expensive. Could only apply production 
            // when an expectedCompletionDate is reached using DateMinder. However, would need to accumulate production since
            // last applied. Could be done by recording last date applied and calc accumulated production when needed to 
            // apply. Would also need to remove/add dates to DateMinder whenever CompletionDates changed.
            _constructionQueueUpdateDuration = new DateMinderDuration(GameTimeDuration.OneHour, this);
            _gameTime.RecurringDateMinder.Add(_constructionQueueUpdateDuration);
            Subscribe();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_unitData.SubscribeToPropertyChanged<AUnitBaseCmdData, float>(data => data.UnitProduction, UnitProductionPropChangedHandler));
        }

        public void AddToQueue(AUnitElementDesign designToConstruct) {
            var expectedStartDate = _constructionQueue.Any() ? _constructionQueue.Last.Value.ExpectedCompletionDate : _gameTime.CurrentDate;
            GameTimeDuration expectedConstructionDuration = new GameTimeDuration(designToConstruct.ConstructionCost / _unitData.UnitProduction);
            GameDate expectedCompletionDate = new GameDate(expectedStartDate, expectedConstructionDuration);
            var constructionItem = new ConstructionInfo(designToConstruct, expectedCompletionDate);
            _constructionQueue.AddLast(constructionItem);
            RefreshCurrentConstructionProperty();
            // Adding to the end of the queue does not change any expected completion dates
            OnConstructionQueueChanged();
        }

        [Obsolete("Not currently used")]
        public void MoveQueueItemAfter(ConstructionInfo beforeItem, ConstructionInfo movingItem) {
            D.AssertNotEqual(beforeItem, movingItem);
            var beforeItemNode = _constructionQueue.Find(beforeItem);
            bool isRemoved = _constructionQueue.Remove(movingItem);
            D.Assert(isRemoved);
            _constructionQueue.AddAfter(beforeItemNode, movingItem);
            RefreshCurrentConstructionProperty();
            UpdateExpectedCompletionDates();
            OnConstructionQueueChanged();
        }

        [Obsolete("Not currently used")]
        public void MoveQueueItemBefore(ConstructionInfo afterItem, ConstructionInfo movingItem) {
            D.AssertNotEqual(afterItem, movingItem);
            var afterItemNode = _constructionQueue.Find(afterItem);
            bool isRemoved = _constructionQueue.Remove(movingItem);
            D.Assert(isRemoved);
            _constructionQueue.AddBefore(afterItemNode, movingItem);
            RefreshCurrentConstructionProperty();
            UpdateExpectedCompletionDates();
            OnConstructionQueueChanged();
        }

        public void RegenerateQueue(IList<ConstructionInfo> orderedQueue) {
            _constructionQueue.Clear();
            foreach (var constructionItem in orderedQueue) {
                _constructionQueue.AddLast(constructionItem);
            }
            RefreshCurrentConstructionProperty();
            UpdateExpectedCompletionDates();
            OnConstructionQueueChanged();
        }

        public void RemoveFromQueue(ConstructionInfo itemToRemove) {
            bool isRemoved = _constructionQueue.Remove(itemToRemove);
            D.Assert(isRemoved);
            UpdateExpectedCompletionDates();
            RefreshCurrentConstructionProperty();
            OnConstructionQueueChanged();
        }

        public IList<ConstructionInfo> GetQueue() {
            return new List<ConstructionInfo>(_constructionQueue);
        }

        private void ProgressConstruction(float availableProduction) {
            D.Assert(!GameReferences.GameManager.IsPaused);
            if (_constructionQueue.Any()) {
                var firstConstructionItem = _constructionQueue.First.Value;
                float unconsumedProduction;
                if (firstConstructionItem.TryCompleteConstruction(availableProduction, out unconsumedProduction)) {
                    RemoveFromQueue(firstConstructionItem);
                    OnConstructionCompleted(firstConstructionItem.Design);
                    ProgressConstruction(unconsumedProduction);
                }
            }
        }

        private void UpdateExpectedCompletionDates() {
            if (_constructionQueue.Any()) {
                GameDate dateConstructionResumes = _gameTime.CurrentDate;
                foreach (var construction in _constructionQueue) {
                    float additionalProdnReqd = construction.Design.ConstructionCost - construction.CumProductionApplied;
                    float remainingHoursToCompletion = additionalProdnReqd / _unitData.UnitProduction;
                    GameTimeDuration remainingDurationToCompletion = new GameTimeDuration(remainingHoursToCompletion);
                    GameDate updatedCompletionDate = new GameDate(dateConstructionResumes, remainingDurationToCompletion);
                    if (updatedCompletionDate != construction.ExpectedCompletionDate) {
                        D.Log("{0}: {1}'s CompletionDate changed from {2} to {3}.",
                            DebugName, construction.DebugName, construction.ExpectedCompletionDate, updatedCompletionDate);
                        construction.ExpectedCompletionDate = updatedCompletionDate;
                    }
                    dateConstructionResumes = construction.ExpectedCompletionDate;
                }
            }
        }

        public void PurchaseQueuedConstruction(ConstructionInfo construction) {
            D.Assert(construction.CanBuyout);
            construction.CompleteConstruction();
            bool isRemoved = _constructionQueue.Remove(construction);
            D.Assert(isRemoved);
            _constructionQueue.AddFirst(construction);
            __ReduceBankBalanceByBuyoutCost(construction.BuyoutCost);
            RefreshCurrentConstructionProperty();
            OnConstructionQueueChanged();
        }

        #region Event and Property Change Handlers

        private void UnitProductionPropChangedHandler() {
            UpdateExpectedCompletionDates();
        }

        private void OnConstructionQueueChanged() {
            if (constructionQueueChanged != null) {
                constructionQueueChanged(this, EventArgs.Empty);
            }
        }

        private void OnConstructionCompleted(AUnitElementDesign completedDesign) {
            if (constructionCompleted != null) {
                constructionCompleted(this, new ConstructionCompletedEventArgs(completedDesign));
            }
        }

        #endregion

        public void HandleDeath() {
            _gameTime.RecurringDateMinder.Remove(_constructionQueueUpdateDuration);
        }

        private void RefreshCurrentConstructionProperty() {
            var currentConstructionItem = _constructionQueue.Any() ? _constructionQueue.First() : TempGameValues.NoConstruction;
            if (_unitData.CurrentConstruction != currentConstructionItem) {
                _unitData.CurrentConstruction = currentConstructionItem;
            }
        }

        private void __ReduceBankBalanceByBuyoutCost(decimal buyoutCost) {
            D.Log("{0}.__ReduceBankBalanceByBuyoutCost({1:0.}) not yet implemented.", DebugName, buyoutCost);
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

        #endregion

        #region Nested Classes

        public class ConstructionCompletedEventArgs : EventArgs {

            public AUnitElementDesign CompletedDesign { get; private set; }

            public ConstructionCompletedEventArgs(AUnitElementDesign completedDesign) {
                CompletedDesign = completedDesign;
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
            D.AssertEqual(_constructionQueueUpdateDuration, recurringDuration);
            ProgressConstruction(_unitData.UnitProduction);
        }

        #endregion


    }
}

