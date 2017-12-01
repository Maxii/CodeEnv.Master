﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Construction.cs
// Tracks progress of an element design under initial construction.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Tracks progress of an element design under initial construction.
    /// </summary>
    public class Construction : APropertyChangeTracking {

        private const string DebugNameFormat = "{0}[{1}]";

        public string DebugName { get { return DebugNameFormat.Inject(GetType().Name, Name); } }

        public virtual string Name { get { return Design.DesignName; } }

        public virtual bool IsRefitConstruction { get { return false; } }

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

        /// <summary>
        /// The remaining time to completion.
        /// <remarks>11.27.17 As TryCompleteConstruction is only attempted once per game hour, ExpectedCompletionDate can
        /// be earlier than CurrentDate which will throw an error when GameDate.operator- is used. Accordingly, if earlier than
        /// CurrentDate, this method returns default(GameTimeDuration), aka no time left to completion.</remarks>
        /// </summary>
        public virtual GameTimeDuration TimeToCompletion {
            get {
                GameDate currentDate = GameTime.Instance.CurrentDate;
                if (ExpectedCompletionDate < currentDate) {
                    //D.Log("{0}.ExpectedCompletionDate {1} < CurrentDate {2}, so returning 0 remaining TimeToCompletion.",
                    //    DebugName, ExpectedCompletionDate, currentDate);
                    return default(GameTimeDuration);
                }
                return ExpectedCompletionDate - GameTime.Instance.CurrentDate;
            }
        }

        public virtual float CompletionPercentage { get { return Mathf.Clamp01(CumProductionApplied / CostToConstruct); } }

        public virtual decimal BuyoutCost {
            get { return (decimal)((CostToConstruct - CumProductionApplied) * TempGameValues.ProductionCostBuyoutMultiplier); }
        }

        /// <summary>
        /// Returns the cost in units of production to construct what is being constructed.
        /// Can construct an Element from scratch or refit an existing Element to a new Design.
        /// <remarks>Default implementation returns the 'from scratch' Design.ConstructionCost. RefitConstruction overrides 
        /// this and returns the cost to 'refit' to this Design from a prior design.</remarks>
        /// </summary>
        public virtual float CostToConstruct { get { return Design.ConstructionCost; } }

        public virtual AtlasID ImageAtlasID { get { return Design.ImageAtlasID; } }

        public virtual string ImageFilename { get { return Design.ImageFilename; } }

        public AUnitElementDesign Design { get; private set; }

        public float CumProductionApplied { get; private set; }

        /// <summary>
        /// The element being constructed or refit.
        /// </summary>
        public IUnitElement Element { get; private set; }

        public Construction(AUnitElementDesign design, IUnitElement element) {
            Design = design;
            Element = element;
        }

        public virtual bool TryCompleteConstruction(float productionToApply, out float unconsumedProduction) {
            CumProductionApplied += productionToApply;
            if (CumProductionApplied >= CostToConstruct) {
                unconsumedProduction = CumProductionApplied - CostToConstruct;
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

        private void __HandleExpectedCompletionDateChanged() {
            //D.Log("{0} has changed ExpectedCompletionDate to {1}. CurrentDate = {2}.",
            //    DebugName, ExpectedCompletionDate, GameTime.Instance.CurrentDate);
        }

        public sealed override string ToString() {
            return DebugName;
        }

        #region Debug

        #endregion


    }
}

