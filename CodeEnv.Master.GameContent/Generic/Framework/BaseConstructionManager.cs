// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BaseConstructionManager.cs
// Manages the progression and completion of element ConstructionTasks via a queue for a Base Unit.
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
    /// Manages the progression and completion of element ConstructionTasks via a queue for a Base Unit.
    /// </summary>
    public class BaseConstructionManager : IRecurringDateMinderClient, IDisposable {

        private const string DebugNameFormat = "{0}.{1}";

        public event EventHandler constructionQueueChanged;

        public event EventHandler<ConstructionCompletedEventArgs> constructionCompleted;

        public string DebugName { get { return DebugNameFormat.Inject(_baseData.UnitName, GetType().Name); } }

        private float _unitProduction;
        private DateMinderDuration _constructionQueueUpdateDuration;
        private LinkedList<ConstructionTask> _constructionQueue;
        private IConstructionManagerClient _baseClient;
        private AUnitBaseCmdData _baseData;
        private GameTime _gameTime;
        private IList<IDisposable> _subscriptions;

        public BaseConstructionManager(AUnitBaseCmdData data, IConstructionManagerClient baseClient) {
            _baseData = data;
            _baseClient = baseClient;
            _gameTime = GameTime.Instance;
            _constructionQueue = new LinkedList<ConstructionTask>();
        }

        public void InitiateProgressChecks() {
            // OPTIMIZE 9.24.17 Updating construction progress every hour is expensive. Could only apply production 
            // when an expectedCompletionDate is reached using DateMinder. However, would need to accumulate production since
            // last applied. Could be done by recording last date applied and calc accumulated production when needed to 
            // apply. Would also need to remove/add dates to DateMinder whenever CompletionDates changed.
            _constructionQueueUpdateDuration = new DateMinderDuration(GameTimeDuration.OneHour, this);
            _gameTime.RecurringDateMinder.Add(_constructionQueueUpdateDuration);
            RefreshUnitProduction();
            Subscribe();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_baseData.SubscribeToPropertyChanged<AUnitBaseCmdData, OutputsYield>(data => data.UnitOutputs, UnitOutputsPropChangedHandler));
        }

        /// <summary>
        /// Adds the provided item to the ConstructionQueue for refitting to <c>refitDesign</c>.
        /// </summary>
        /// <param name="refitDesign">The design to refit too.</param>
        /// <param name="item">The item to be refitted.</param>
        /// <param name="refitCost">The refit cost.</param>
        /// <returns></returns>
        public RefitConstructionTask AddToRefitQueue(AUnitElementDesign refitDesign, IUnitElement item, float refitCost) {
            var refitConstruction = new RefitConstructionTask(refitDesign, item, refitCost);

            var expectedStartDate = _constructionQueue.Any() ? _constructionQueue.Last.Value.ExpectedCompletionDate : _gameTime.CurrentDate;
            float refitConstructionCost = refitConstruction.CostToConstruct;
            GameTimeDuration expectedConstructionDuration = new GameTimeDuration(refitConstructionCost / _unitProduction);
            GameDate expectedCompletionDate = new GameDate(expectedStartDate, expectedConstructionDuration);
            refitConstruction.ExpectedCompletionDate = expectedCompletionDate;

            _constructionQueue.AddLast(refitConstruction);
            RefreshCurrentConstructionProperty();
            // Adding to the end of the queue does not change any expected completion dates
            OnConstructionQueueChanged();
            return refitConstruction;
        }

        /// <summary>
        /// Adds the provided item to the ConstructionQueue for disbanding.
        /// </summary>
        /// <param name="designToDisband">The design we are disbanding.</param>
        /// <param name="item">The item to be disbanded.</param>
        /// <param name="disbandCost">The disband cost.</param>
        /// <returns></returns>
        public DisbandConstructionTask AddToDisbandQueue(AUnitElementDesign designToDisband, IUnitElement item, float disbandCost) {
            var disbandConstruction = new DisbandConstructionTask(designToDisband, item, disbandCost);

            var expectedStartDate = _constructionQueue.Any() ? _constructionQueue.Last.Value.ExpectedCompletionDate : _gameTime.CurrentDate;
            float disbandConstructionCost = disbandConstruction.CostToConstruct;
            GameTimeDuration expectedConstructionDuration = new GameTimeDuration(disbandConstructionCost / _unitProduction);
            GameDate expectedCompletionDate = new GameDate(expectedStartDate, expectedConstructionDuration);
            disbandConstruction.ExpectedCompletionDate = expectedCompletionDate;

            _constructionQueue.AddLast(disbandConstruction);
            RefreshCurrentConstructionProperty();
            // Adding to the end of the queue does not change any expected completion dates
            OnConstructionQueueChanged();
            return disbandConstruction;
        }

        /// <summary>
        /// Adds the provided item to the ConstructionQueue for its initial construction of <c>designToConstruct</c>.
        /// <remarks>The provided item must of course be instantiated beforehand using designToConstruct. However, it is 
        /// Unavailable for use until construction is completed.</remarks>
        /// </summary>
        /// <param name="designToConstruct">The design to construct.</param>
        /// <param name="item">The item to be 'constructed'.</param>
        /// <returns></returns>
        public ConstructionTask AddToQueue(AUnitElementDesign designToConstruct, IUnitElement item) {
            var construction = new ConstructionTask(designToConstruct, item);
            var expectedStartDate = _constructionQueue.Any() ? _constructionQueue.Last.Value.ExpectedCompletionDate : _gameTime.CurrentDate;
            float constructionCost = designToConstruct.ConstructionCost;
            GameTimeDuration expectedConstructionDuration = new GameTimeDuration(constructionCost / _unitProduction);
            GameDate expectedCompletionDate = new GameDate(expectedStartDate, expectedConstructionDuration);
            construction.ExpectedCompletionDate = expectedCompletionDate;

            _constructionQueue.AddLast(construction);
            RefreshCurrentConstructionProperty();
            // Adding to the end of the queue does not change any expected completion dates
            OnConstructionQueueChanged();
            return construction;
        }

        [Obsolete("Not currently used")]
        public void MoveQueuedConstructionAfter(ConstructionTask constructionBefore, ConstructionTask constructionMoving) {
            D.AssertNotEqual(constructionBefore, constructionMoving);
            var beforeItemNode = _constructionQueue.Find(constructionBefore);
            bool isRemoved = _constructionQueue.Remove(constructionMoving);
            D.Assert(isRemoved);
            _constructionQueue.AddAfter(beforeItemNode, constructionMoving);
            RefreshCurrentConstructionProperty();
            UpdateExpectedCompletionDates();
            OnConstructionQueueChanged();
        }

        [Obsolete("Not currently used")]
        public void MoveQueuedConstructionBefore(ConstructionTask constructionAfter, ConstructionTask constructionMoving) {
            D.AssertNotEqual(constructionAfter, constructionMoving);
            var afterItemNode = _constructionQueue.Find(constructionAfter);
            bool isRemoved = _constructionQueue.Remove(constructionMoving);
            D.Assert(isRemoved);
            _constructionQueue.AddBefore(afterItemNode, constructionMoving);
            RefreshCurrentConstructionProperty();
            UpdateExpectedCompletionDates();
            OnConstructionQueueChanged();
        }

        public void RegenerateQueue(IList<ConstructionTask> orderedQueue) {
            _constructionQueue.Clear();
            foreach (var construction in orderedQueue) {
                _constructionQueue.AddLast(construction);
            }
            RefreshCurrentConstructionProperty();
            UpdateExpectedCompletionDates();
            OnConstructionQueueChanged();
        }

        public void RemoveFromQueue(ConstructionTask construction) {
            bool isRemoved = _constructionQueue.Remove(construction);
            D.Assert(isRemoved);
            UpdateExpectedCompletionDates();
            RefreshCurrentConstructionProperty();
            if (!construction.IsCompleted) {
                _baseClient.HandleUncompletedConstructionRemovedFromQueue(construction);
                if (construction.CompletionPercentage > Constants.ZeroPercent) {
                    __HandlePartiallyCompletedConstructionBeingRemovedFromQueue(construction);
                }
            }
            OnConstructionQueueChanged();
        }

        public IList<ConstructionTask> GetQueue() {
            return new List<ConstructionTask>(_constructionQueue);
        }

        public bool IsConstructionQueuedFor(IUnitElement element) {
            var elementConstruction = _constructionQueue.SingleOrDefault(c => c.Element == element);
            return elementConstruction != default(ConstructionTask);
        }

        public ConstructionTask GetConstructionFor(IUnitElement element) {
            return _constructionQueue.Single(c => c.Element == element);
        }

        private void ProgressConstruction(float availableProduction) {
            // 3.29.18 Can't Assert not paused as UserResearchManager can complete research on the same date. If the User manually
            // picks the techs to research, the UserActionButton will auto pause. Can fail Assert depending on DateMinder's call order
            if (_constructionQueue.Any()) {
                var firstConstruction = _constructionQueue.First.Value;
                //D.Log("{0} is checking {1} for completion. ExpectedCompletionDate = {2}, CurrentDate = {3}.",
                //    DebugName, firstConstruction.DebugName, firstConstruction.ExpectedCompletionDate, _gameTime.CurrentDate);
                float unconsumedProduction;
                if (firstConstruction.TryCompleteConstruction(availableProduction, out unconsumedProduction)) {
                    //D.Log("{0} has completed construction of {1}. ExpectedCompletionDate = {2}, CurrentDate = {3}.",
                    //    DebugName, firstConstruction.DebugName, firstConstruction.ExpectedCompletionDate, _gameTime.CurrentDate);
                    RemoveFromQueue(firstConstruction);
                    _baseClient.HandleConstructionCompleted(firstConstruction);
                    OnConstructionCompleted(firstConstruction);
                    ProgressConstruction(unconsumedProduction);
                }
            }
        }

        private void UpdateExpectedCompletionDates() {
            if (_constructionQueue.Any()) {
                GameDate dateConstructionResumes = _gameTime.CurrentDate;
                foreach (var construction in _constructionQueue) {
                    float additionalProdnReqd = construction.Design.ConstructionCost - construction.CumProductionApplied;
                    float remainingHoursToCompletion = additionalProdnReqd / _unitProduction;
                    GameTimeDuration remainingDurationToCompletion = new GameTimeDuration(remainingHoursToCompletion);
                    GameDate updatedCompletionDate = new GameDate(dateConstructionResumes, remainingDurationToCompletion);
                    if (updatedCompletionDate != construction.ExpectedCompletionDate) {
                        //D.Log("{0}: {1}'s CompletionDate changed from {2} to {3}.",
                        //    DebugName, construction.DebugName, construction.ExpectedCompletionDate, updatedCompletionDate);
                        construction.ExpectedCompletionDate = updatedCompletionDate;
                    }
                    dateConstructionResumes = construction.ExpectedCompletionDate;
                }
            }
        }

        public void PurchaseQueuedConstruction(ConstructionTask construction) {
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

        private void UnitOutputsPropChangedHandler() {
            RefreshUnitProduction();
            UpdateExpectedCompletionDates();
        }

        private void OnConstructionQueueChanged() {
            if (constructionQueueChanged != null) {
                constructionQueueChanged(this, EventArgs.Empty);
            }
        }

        private void OnConstructionCompleted(ConstructionTask completedConstruction) {
            if (constructionCompleted != null) {
                constructionCompleted(this, new ConstructionCompletedEventArgs(completedConstruction));
            }
        }

        #endregion

        public void HandleDeath() {
            RemoveAllConstructionInQueue();
            _gameTime.RecurringDateMinder.Remove(_constructionQueueUpdateDuration);
        }

        public void HandleLosingOwnership() {
            RemoveAllConstructionInQueue();
        }

        private void RemoveAllConstructionInQueue() {
            var cQueueCopy = new List<ConstructionTask>(_constructionQueue);
            foreach (var c in cQueueCopy) {
                RemoveFromQueue(c);
            }
        }

        private void RefreshCurrentConstructionProperty() {
            var currentConstructionItem = _constructionQueue.Any() ? _constructionQueue.First.Value : TempGameValues.NoConstruction;
            _baseData.CurrentConstruction = currentConstructionItem;
        }

        private void RefreshUnitProduction() {
            var unitOutputs = _baseData.UnitOutputs;
            if (_baseData.ElementsData.Any()) {
                // unitOutputs can be default without Production present when 
                // Base's last element is removed which occurs before IsOperational is set to false
                D.AssertNotDefault(unitOutputs);
                D.Assert(unitOutputs.IsPresent(OutputID.Production));
            }
            _unitProduction = unitOutputs.GetYield(OutputID.Production).Value;
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
        private void __HandlePartiallyCompletedConstructionBeingRemovedFromQueue(ConstructionTask construction) {
            D.Log("{0} is removing {1} from Queue that is partially completed.", DebugName, construction.DebugName);
            // TODO This question should be raised by the Gui click handler as a popup before sending to the BaseConstructionManager
        }

        private void __ReduceBankBalanceByBuyoutCost(decimal buyoutCost) {
            D.Warn("{0}.__ReduceBankBalanceByBuyoutCost({1:0.}) not yet implemented.", DebugName, buyoutCost);
        }

        #endregion

        #region Nested Classes

        public class ConstructionCompletedEventArgs : EventArgs {

            public ConstructionTask CompletedConstruction { get; private set; }

            public ConstructionCompletedEventArgs(ConstructionTask completedConstruction) {
                CompletedConstruction = completedConstruction;
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
            ProgressConstruction(_unitProduction);
        }

        #endregion


    }
}

