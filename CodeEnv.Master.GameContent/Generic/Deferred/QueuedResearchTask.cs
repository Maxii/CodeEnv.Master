// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: QueuedResearchTask.cs
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
    [System.Obsolete]
    public class QueuedResearchTask : APropertyChangeTracking {

        private const string DebugNameFormat = "{0}[{1}]";

        public string DebugName { get { return DebugNameFormat.Inject(GetType().Name, Name); } }

        public virtual string Name { get { return TechBeingResearched.Name; } }

        private bool _isCompleted = false;
        public bool IsCompleted {
            get { return _isCompleted; }
            private set {
                if (_isCompleted != value) {
                    D.Assert(value);       // not allowed to be set false
                    _isCompleted = value;
                }
                // No SetProperty to avoid subscribing to IsCompleted. Use PlayerResearchManager.isResearchCompleted instead.
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

        /// <summary>
        /// The remaining time to completion.
        /// <remarks>2.26.18 As TryComplete is only attempted once per game hour, ExpectedCompletionDate can
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
                return ExpectedCompletionDate - currentDate;
            }
        }

        public virtual float CompletionPercentage { get { return Mathf.Clamp01(CumScienceApplied / CostToResearch); } }

        /// <summary>
        /// Returns the cost in units of science to research the tech being researched by this task.
        /// </summary>
        public virtual float CostToResearch { get { return TechBeingResearched.ResearchCost; } }

        public virtual AtlasID ImageAtlasID { get { return TechBeingResearched.ImageAtlasID; } }

        public virtual string ImageFilename { get { return TechBeingResearched.ImageFilename; } }

        public Technology TechBeingResearched { get; private set; }

        public float CumScienceApplied { get; private set; }

        public QueuedResearchTask(Technology techToResearch) {
            TechBeingResearched = techToResearch;
        }

        public virtual bool TryComplete(float scienceToApply, out float unconsumedScience) {
            CumScienceApplied += scienceToApply;
            if (CumScienceApplied >= CostToResearch) {
                unconsumedScience = CumScienceApplied - CostToResearch;
                IsCompleted = true;
                return true;
            }
            unconsumedScience = Constants.ZeroF;
            return false;
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

    }
}

