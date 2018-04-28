// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResearchTask.cs
// Tracks progress of a Technology being researched by a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Tracks progress of a Technology being researched by a player.
    /// </summary>
    public class ResearchTask {

        private const string DebugNameFormat = "{0}[{1}]";

        public virtual string DebugName { get { return DebugNameFormat.Inject(GetType().Name, Tech.DebugName); } }

        private bool _isCompleted = false;
        public bool IsCompleted {
            get { return _isCompleted; }
            private set {
                if (_isCompleted != value) {
                    D.Assert(value);       // not allowed to be set false
                    CumScienceApplied = CostToResearch;
                    _isCompleted = value;
                }
                // No SetProperty to avoid subscribing to IsCompleted. Use PlayerResearchManager.isResearchCompleted instead.
            }
        }

        private GameTimeDuration _timeToComplete;
        /// <summary>
        /// The remaining time required to complete the research at the current rate of science accumulation.
        /// <remarks>2.27.18 This value can be used to estimate a completion date, but only if it is the current research task,
        /// aka science accumulation is currently being applied to this task to complete it.</remarks>
        /// </summary>
        public virtual GameTimeDuration TimeToComplete {
            get { return _timeToComplete; }
            set {
                if (_timeToComplete != value) {
                    _timeToComplete = value;
                    __TimeToCompletePropChangedHandler();
                }
            }
        }

        public virtual float CompletionPercentage { get { return Mathf.Clamp01(CumScienceApplied / CostToResearch); } }

        /// <summary>
        /// Returns the cost in units of science to research the tech being researched by this task.
        /// </summary>
        public virtual float CostToResearch { get { return Tech.ResearchCost; } }

        public virtual AtlasID ImageAtlasID { get { return Tech.ImageAtlasID; } }

        public virtual string ImageFilename { get { return Tech.ImageFilename; } }

        public virtual Technology Tech { get; private set; }

        public virtual float CumScienceApplied { get; private set; }

        public virtual float RemainingScienceNeededToComplete {
            get {
                float remainingScienceNeeded = CostToResearch - CumScienceApplied;
                return Mathf.Clamp(remainingScienceNeeded, Constants.ZeroF, remainingScienceNeeded);
            }
        }

        public ResearchTask(Technology techToResearch) {
            Tech = techToResearch;
        }

        public virtual bool TryComplete(float scienceToApply, out float unconsumedScience) {
            D.Assert(!IsCompleted);
            CumScienceApplied += scienceToApply;
            if (CumScienceApplied >= CostToResearch) {
                unconsumedScience = CumScienceApplied - CostToResearch;
                IsCompleted = true;
                return true;
            }
            unconsumedScience = Constants.ZeroF;
            return false;
        }

        public virtual void CompleteResearch() {
            float unusedUnconsumedRsch;
            bool isCompleted = TryComplete(CostToResearch - CumScienceApplied, out unusedUnconsumedRsch);
            D.Assert(isCompleted);
        }

        #region Event and Property Change Handlers

        private void __TimeToCompletePropChangedHandler() {
            __HandleTimeToCompletePropChanged();
        }

        #endregion

        private void __HandleTimeToCompletePropChanged() {
            //D.Log("{0} has changed TimeToComplete to {1}.", DebugName, TimeToComplete);
        }

        public sealed override string ToString() {
            return DebugName;
        }

    }
}

