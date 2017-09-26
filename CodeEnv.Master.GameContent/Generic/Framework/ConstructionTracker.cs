// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ConstructionTracker.cs
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
    public class ConstructionTracker {

        private const string DebugNameFormat = "{0}[{1}]";

        [Obsolete]
        public event EventHandler constructionCompleted;

        public string DebugName {
            get {
                return DebugNameFormat.Inject(GetType().Name, Design.DesignName);
            }
        }

        private GameDate _expectedCompletionDate;
        public GameDate ExpectedCompletionDate {
            get { return _expectedCompletionDate; }
            set {
                if (_expectedCompletionDate != value) {
                    _expectedCompletionDate = value;
                    __ExpectedCompletionDatePropChangedHandler();
                }
            }
        }

        public bool CanBuyout {
            get {
                decimal playerBankBalance = GameReferences.GameManager.GetAIManagerFor(Design.Player).__PlayerBankBalance;
                return BuyoutCost <= playerBankBalance;
            }
        }

        public GameTimeDuration TimeToCompletion { get { return ExpectedCompletionDate - GameTime.Instance.CurrentDate; } }

        public float CompletionPercentage { get { return Mathf.Clamp01(CumProductionApplied / Design.ConstructionCost); } }

        public decimal BuyoutCost {
            get { return (decimal)((Design.ConstructionCost - CumProductionApplied) * TempGameValues.ProductionCostBuyoutMultiplier); }
        }

        public AUnitElementDesign Design { get; private set; }

        public float CumProductionApplied { get; private set; }

        public ConstructionTracker(AUnitElementDesign design, GameDate expectedCompletionDate) {
            Design = design;
            _expectedCompletionDate = expectedCompletionDate;
        }

        public bool TryCompleteConstruction(float productionToApply, out float unconsumedProduction) {
            CumProductionApplied += productionToApply;
            if (CumProductionApplied >= Design.ConstructionCost) {
                unconsumedProduction = CumProductionApplied - Design.ConstructionCost;
                ////OnConstructionCompleted();
                return true;
            }
            unconsumedProduction = Constants.ZeroF;
            return false;
        }

        public void CompleteConstruction() {
            float unusedUnconsumedProduction;
            bool isCompleted = TryCompleteConstruction(Design.ConstructionCost - CumProductionApplied, out unusedUnconsumedProduction);
            D.Assert(isCompleted);
        }

        #region Event and Property Change Handlers

        private void __ExpectedCompletionDatePropChangedHandler() {
            __HandleExpectedCompletionDateChanged();
        }

        [Obsolete]
        private void OnConstructionCompleted() {
            if (constructionCompleted != null) {
                constructionCompleted(this, EventArgs.Empty);
            }
        }

        #endregion

        private void __HandleExpectedCompletionDateChanged() { }

        public override string ToString() {
            return DebugName;
        }

        #region Debug

        #endregion

    }
}

