// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ConstructionInfo.cs
// Tracks progress of an element design under construction.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Tracks progress of an element design under construction.
    /// </summary>
    public class ConstructionInfo : APropertyChangeTracking {

        private const string DebugNameFormat = "{0}[{1}]";

        public string DebugName { get { return DebugNameFormat.Inject(GetType().Name, Name); } }

        public virtual string Name { get { return Design.DesignName; } }

        public virtual bool IsRefitConstruction { get { return false; } }

        /// <summary>
        /// The element being constructed or refit.
        /// <remarks>Public set as elements being initially constructed cannot be assigned in the constructor
        /// as they haven't been instantiated yet.</remarks>
        /// </summary>
        public IUnitElement Element { get; set; }

        private bool _isCompleted = false;
        public virtual bool IsCompleted {
            get { return _isCompleted; }
            private set {
                D.Assert(!_isCompleted);    // not allowed to be set false
                SetProperty<bool>(ref _isCompleted, value, "IsCompleted");  // provides access to IsCompletedChanged
            }
        }

        private GameDate _expectedCompletionDate;
        public virtual GameDate ExpectedCompletionDate {
            get { return _expectedCompletionDate; }
            set {
                if (_expectedCompletionDate != value) {
                    _expectedCompletionDate = value;
                    __ExpectedCompletionDatePropChangedHandler();
                }
            }
        }

        public virtual bool CanBuyout {
            get {
                decimal playerBankBalance = GameReferences.GameManager.GetAIManagerFor(Design.Player).Knowledge.__BankBalance;
                return BuyoutCost <= playerBankBalance;
            }
        }

        public virtual GameTimeDuration TimeToCompletion { get { return ExpectedCompletionDate - GameTime.Instance.CurrentDate; } }

        public virtual float CompletionPercentage { get { return Mathf.Clamp01(CumProductionApplied / CostToConstruct); } }

        public virtual decimal BuyoutCost {
            get { return (decimal)((CostToConstruct - CumProductionApplied) * TempGameValues.ProductionCostBuyoutMultiplier); }
        }

        /// <summary>
        /// Returns the cost in units of production to construct what is being constructed.
        /// Can construct an Element from scratch or refit an existing Element to a new Design.
        /// </summary>
        public virtual float CostToConstruct { get { return Design.ConstructionCost; } }

        public virtual AtlasID ImageAtlasID { get { return Design.ImageAtlasID; } }

        public virtual string ImageFilename { get { return Design.ImageFilename; } }

        public AUnitElementDesign Design { get; private set; }

        public float CumProductionApplied { get; private set; }

        public ConstructionInfo(AUnitElementDesign design) {
            Design = design;
        }

        public virtual bool TryCompleteConstruction(float productionToApply, out float unconsumedProduction) {
            CumProductionApplied += productionToApply;
            if (CumProductionApplied >= Design.ConstructionCost) {
                unconsumedProduction = CumProductionApplied - Design.ConstructionCost;
                IsCompleted = true;
                return true;
            }
            unconsumedProduction = Constants.ZeroF;
            return false;
        }

        public virtual void CompleteConstruction() {
            float unusedUnconsumedProduction;
            bool isCompleted = TryCompleteConstruction(CostToConstruct - CumProductionApplied, out unusedUnconsumedProduction);
            D.Assert(isCompleted);
        }

        #region Event and Property Change Handlers

        private void __ExpectedCompletionDatePropChangedHandler() {
            __HandleExpectedCompletionDateChanged();
        }

        #endregion

        private void __HandleExpectedCompletionDateChanged() { }

        public sealed override string ToString() {
            return DebugName;
        }

        #region Debug

        #endregion

    }
}

