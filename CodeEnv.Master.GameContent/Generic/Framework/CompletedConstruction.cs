// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CompletedConstruction.cs
// ConstructionInfo for use with Elements that are instantiated when the game begins.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// ConstructionInfo for use with Elements that are instantiated when the game begins.
    /// </summary>
    [Obsolete]
    public class CompletedConstruction : ConstructionInfo {

        public override string Name { get { return GetType().Name; } }

        public override bool IsCompleted { get { return true; } }

        public override GameTimeDuration TimeToCompletion { get { return default(GameTimeDuration); } }

        public override GameDate ExpectedCompletionDate {
            get { throw new NotImplementedException("ExpectedCompletionDate.set is not implemented in {0}.".Inject(DebugName)); }
            set { throw new NotImplementedException("ExpectedCompletionDate.set is not implemented in {0}.".Inject(DebugName)); }
        }

        public override float CompletionPercentage { get { return Constants.OneHundredPercent; } }

        public override bool CanBuyout { get { return false; } }

        public override decimal BuyoutCost { get { return Constants.ZeroCurrency; } }

        public override AtlasID ImageAtlasID { get { return AtlasID.MyGui; } }

        public override string ImageFilename { get { return TempGameValues.EmptyImageFilename; } }

        public CompletedConstruction(AUnitElementDesign design) : base(design) { }

        public override bool TryCompleteConstruction(float productionToApply, out float unconsumedProduction) {
            throw new NotImplementedException("TryCompleteConstruction() is not implemented in {0}.".Inject(DebugName));
        }

        public override void CompleteConstruction() {
            throw new NotImplementedException("CompleteConstruction() is not implemented in {0}.".Inject(DebugName));
        }

    }
}

