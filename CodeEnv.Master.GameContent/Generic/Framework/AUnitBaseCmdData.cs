// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitBaseCmdData.cs
// Abstract class for Data associated with an AUnitBaseCmdItem.
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
    using UnityEngine;

    /// <summary>
    /// Abstract class for Data associated with an AUnitBaseCmdItem.
    /// </summary>
    public abstract class AUnitBaseCmdData : AUnitCmdData, IRecurringDateMinderClient {

        public event EventHandler constructionQueueChanged;

        public new FacilityData HQElementData {
            protected get { return base.HQElementData as FacilityData; }
            set { base.HQElementData = value; }
        }

        private BaseComposition _unitComposition;
        public BaseComposition UnitComposition {
            get { return _unitComposition; }
            private set { SetProperty<BaseComposition>(ref _unitComposition, value, "UnitComposition"); }
        }

        public new IEnumerable<FacilityData> ElementsData { get { return base.ElementsData.Cast<FacilityData>(); } }

        private float _unitProduction;
        public float UnitProduction {
            get { return _unitProduction; }
            private set { SetProperty<float>(ref _unitProduction, value, "UnitProduction", UnitProductionChangedHandler); }
        }

        private IntVector3 _sectorID;
        public override IntVector3 SectorID {
            get {
                if (_sectorID == default(IntVector3)) {
                    _sectorID = InitializeSectorID();
                }
                return _sectorID;
            }
        }

        private DateMinderDuration _constructionQueueUpdateDuration;
        private LinkedList<ConstructionTracker> _constructionQueue;

        #region Initialization 

        public AUnitBaseCmdData(IUnitCmd cmd, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, IEnumerable<CmdSensor> sensors,
            FtlDampener ftlDampener, ACmdModuleStat cmdStat, string designName)
            : base(cmd, owner, passiveCMs, sensors, ftlDampener, cmdStat, designName) {
            _constructionQueue = new LinkedList<ConstructionTracker>();
        }

        protected override AIntel MakeIntelInstance() {
            return new RegressibleIntel(lowestRegressedCoverage: IntelCoverage.Basic);
        }

        public override void FinalInitialize() {
            base.FinalInitialize();
            // Deployment has already occurred
        }

        private IntVector3 InitializeSectorID() {
            IntVector3 sectorID = GameReferences.SectorGrid.GetSectorIDThatContains(Position);
            D.AssertNotDefault(sectorID);
            MarkAsChanged();
            return sectorID;
        }

        #endregion

        public override void CommenceOperations() {
            base.CommenceOperations();
            __InitiateConstructionQueueProcessing();
        }

        protected override void Subscribe(AUnitElementData elementData) {
            base.Subscribe(elementData);
            var anElementsSubscriptions = _elementSubscriptionsLookup[elementData];
            FacilityData facilityData = elementData as FacilityData;
            anElementsSubscriptions.Add(facilityData.SubscribeToPropertyChanged<FacilityData, float>(ed => ed.Production, ElementProductionPropChangedHandler));
        }

        protected override void RecalcPropertiesDerivedFromCombinedElements() {
            base.RecalcPropertiesDerivedFromCombinedElements();
            RecalcUnitProduction();
        }

        #region Event and Property Change Handlers

        private void ElementProductionPropChangedHandler() {
            RecalcUnitProduction();
        }

        private void UnitProductionChangedHandler() {
            HandleUnitProductionChanged();
        }

        private void OnConstructionQueueChanged() {
            if (constructionQueueChanged != null) {
                constructionQueueChanged(this, EventArgs.Empty);
            }
        }

        #endregion

        private void HandleUnitProductionChanged() {
            if (IsOperational) {
                UpdateConstructionQueueExpectedCompletionDates();
            }
        }

        protected override void HandleDeath() {
            base.HandleDeath();
            GameTime.Instance.RecurringDateMinder.Remove(_constructionQueueUpdateDuration);
        }

        protected override void HandleUnitWeaponsRangeChanged() {
            if (UnitWeaponsRange.Max > TempGameValues.__MaxBaseWeaponsRangeDistance) {
                D.Warn("{0} max UnitWeaponsRange {1:0.#} > {2:0.#}.", DebugName, UnitWeaponsRange.Max, TempGameValues.__MaxBaseWeaponsRangeDistance);
            }
        }

        private void RecalcUnitProduction() {
            UnitProduction = _elementsData.Cast<FacilityData>().Sum(ed => ed.Production);
        }

        protected override void RefreshComposition() {
            var elementCategories = _elementsData.Cast<FacilityData>().Select(fd => fd.HullCategory);
            UnitComposition = new BaseComposition(elementCategories);
        }

        #region Construction Queue Management

        private void __InitiateConstructionQueueProcessing() {
            // OPTIMIZE 9.24.17 Updating construction progress every hour is expensive. Could only apply production 
            // when an expectedCompletionDate is reached using DateMinder. However, would need to accumulate production since
            // last applied. Could be done by recording last date applied and calc accumulated production when needed to 
            // apply. Would also need to remove/add dates to DateMinder whenever CompletionDates changed.
            _constructionQueueUpdateDuration = new DateMinderDuration(GameTimeDuration.OneHour, this);
            GameTime.Instance.RecurringDateMinder.Add(_constructionQueueUpdateDuration);
        }

        public void AddToConstructionQueue(AElementDesign designToConstruct) {
            var expectedStartDate = _constructionQueue.Any() ? _constructionQueue.Last.Value.ExpectedCompletionDate : GameTime.Instance.CurrentDate;
            GameTimeDuration expectedConstructionDuration = new GameTimeDuration(designToConstruct.ConstructionCost / UnitProduction);
            GameDate expectedCompletionDate = new GameDate(expectedStartDate, expectedConstructionDuration);
            var constructionItem = new ConstructionTracker(designToConstruct, expectedCompletionDate);
            _constructionQueue.AddLast(constructionItem);
            // Adding to the end of the queue does not change any expected completion dates
            OnConstructionQueueChanged();
        }

        [Obsolete("Not currently used")]
        public void MoveConstructionQueueItemAfter(ConstructionTracker beforeItem, ConstructionTracker movingItem) {
            D.AssertNotEqual(beforeItem, movingItem);
            var beforeItemNode = _constructionQueue.Find(beforeItem);
            bool isRemoved = _constructionQueue.Remove(movingItem);
            D.Assert(isRemoved);
            _constructionQueue.AddAfter(beforeItemNode, movingItem);
            UpdateConstructionQueueExpectedCompletionDates();
            OnConstructionQueueChanged();
        }

        [Obsolete("Not currently used")]
        public void MoveConstructionQueueItemBefore(ConstructionTracker afterItem, ConstructionTracker movingItem) {
            D.AssertNotEqual(afterItem, movingItem);
            var afterItemNode = _constructionQueue.Find(afterItem);
            bool isRemoved = _constructionQueue.Remove(movingItem);
            D.Assert(isRemoved);
            _constructionQueue.AddBefore(afterItemNode, movingItem);
            UpdateConstructionQueueExpectedCompletionDates();
            OnConstructionQueueChanged();
        }

        public void RegenerateConstructionQueue(IList<ConstructionTracker> orderedQueue) {
            _constructionQueue.Clear();
            foreach (var constructionItem in orderedQueue) {
                _constructionQueue.AddLast(constructionItem);
            }
            UpdateConstructionQueueExpectedCompletionDates();
            OnConstructionQueueChanged();
        }

        public void RemoveFromConstructionQueue(ConstructionTracker itemToRemove) {
            bool isRemoved = _constructionQueue.Remove(itemToRemove);
            D.Assert(isRemoved);
            UpdateConstructionQueueExpectedCompletionDates();
            OnConstructionQueueChanged();
        }

        public IList<ConstructionTracker> GetConstructionQueue() {
            return new List<ConstructionTracker>(_constructionQueue);
        }

        private void ProgressConstruction(float availableProduction) {
            D.Assert(!_gameMgr.IsPaused);
            if (_constructionQueue.Any()) {
                var firstConstructionItem = _constructionQueue.First.Value;
                float unconsumedProduction;
                if (firstConstructionItem.TryCompleteConstruction(availableProduction, out unconsumedProduction)) {
                    __HandleConstructionCompleted(firstConstructionItem);
                    RemoveFromConstructionQueue(firstConstructionItem);
                    ProgressConstruction(unconsumedProduction);
                }
            }
        }

        private void UpdateConstructionQueueExpectedCompletionDates() {
            if (_constructionQueue.Any()) {
                GameDate dateItemConstructionResumes = GameTime.Instance.CurrentDate;
                foreach (var constructionItem in _constructionQueue) {
                    float additionalProdnReqd = constructionItem.Design.ConstructionCost - constructionItem.CumProductionApplied;
                    float remainingHoursToCompletion = additionalProdnReqd / UnitProduction;
                    GameTimeDuration remainingDurationToCompletion = new GameTimeDuration(remainingHoursToCompletion);
                    GameDate updatedCompletionDate = new GameDate(dateItemConstructionResumes, remainingDurationToCompletion);
                    if (updatedCompletionDate != constructionItem.ExpectedCompletionDate) {
                        D.Log("{0}: {1}'s CompletionDate changed from {2} to {3}.",
                            DebugName, constructionItem.DebugName, constructionItem.ExpectedCompletionDate, updatedCompletionDate);
                        constructionItem.ExpectedCompletionDate = updatedCompletionDate;
                    }
                    dateItemConstructionResumes = constructionItem.ExpectedCompletionDate;
                }
            }
        }

        public void PurchaseQueuedConstructionItem(ConstructionTracker itemUnderConstruction) {
            D.Assert(itemUnderConstruction.CanBuyout);
            itemUnderConstruction.CompleteConstruction();
            bool isRemoved = _constructionQueue.Remove(itemUnderConstruction);
            D.Assert(isRemoved);
            _constructionQueue.AddFirst(itemUnderConstruction);
            __ReduceBankBalanceByBuyoutCost(itemUnderConstruction.BuyoutCost);
            OnConstructionQueueChanged();
        }

        #endregion

        #region Debug

        private void __HandleConstructionCompleted(ConstructionTracker firstConstructionItem) {
            D.Log("{0} has completed construction of {1} without instantiation.", DebugName, firstConstructionItem.DebugName);
            //D.Log("{0}: {1}.ConstructionCost = {2:0.#}, Prod/Hr = {3:0.#}.",
            //    DebugName, firstConstructionItem.DebugName, firstConstructionItem.Design.ConstructionCost, UnitProduction);
        }

        private void __ReduceBankBalanceByBuyoutCost(decimal buyoutCost) {
            D.Log("{0}.__ReduceBankBalanceByBuyoutCost({1:0.}) not yet implemented.", DebugName, buyoutCost);
        }

        #endregion

        #region IRecurringDateMinderClient Members

        public void HandleDateReached(DateMinderDuration recurringDuration) {
            D.AssertEqual(_constructionQueueUpdateDuration, recurringDuration);
            ProgressConstruction(UnitProduction);
        }

        #endregion
    }
}

