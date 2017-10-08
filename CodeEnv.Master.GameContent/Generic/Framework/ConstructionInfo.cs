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
    public class ConstructionInfo {

        private const string DebugNameFormat = "{0}[{1}]";

        public string DebugName { get { return DebugNameFormat.Inject(GetType().Name, Name); } }

        public virtual string Name { get { return Design.DesignName; } }

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
                decimal playerBankBalance = GameReferences.GameManager.GetAIManagerFor(Design.Player).__PlayerBankBalance;
                return BuyoutCost <= playerBankBalance;
            }
        }

        public virtual GameTimeDuration TimeToCompletion { get { return ExpectedCompletionDate - GameTime.Instance.CurrentDate; } }

        public virtual float CompletionPercentage { get { return Mathf.Clamp01(CumProductionApplied / Design.ConstructionCost); } }

        public virtual decimal BuyoutCost {
            get { return (decimal)((Design.ConstructionCost - CumProductionApplied) * TempGameValues.ProductionCostBuyoutMultiplier); }
        }

        public virtual AtlasID ImageAtlasID { get { return Design.ImageAtlasID; } }

        public virtual string ImageFilename { get { return Design.ImageFilename; } }

        public AUnitElementDesign Design { get; private set; }

        public float CumProductionApplied { get; private set; }

        public ConstructionInfo(AUnitElementDesign design, GameDate expectedCompletionDate) {
            Design = design;
            _expectedCompletionDate = expectedCompletionDate;
        }

        public virtual bool TryCompleteConstruction(float productionToApply, out float unconsumedProduction) {
            CumProductionApplied += productionToApply;
            if (CumProductionApplied >= Design.ConstructionCost) {
                unconsumedProduction = CumProductionApplied - Design.ConstructionCost;
                return true;
            }
            unconsumedProduction = Constants.ZeroF;
            return false;
        }

        public virtual void CompleteConstruction() {
            float unusedUnconsumedProduction;
            bool isCompleted = TryCompleteConstruction(Design.ConstructionCost - CumProductionApplied, out unusedUnconsumedProduction);
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

