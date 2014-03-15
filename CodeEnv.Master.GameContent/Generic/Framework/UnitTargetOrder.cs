// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitTargetOrder.cs
// Order that requires IMortalTarget info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Order that requires IMortalTarget info.
    /// </summary>
    public class UnitTargetOrder<T> : UnitDestinationOrder<T> where T : struct {

        public new IMortalTarget Target {
            get { return base.Target as IMortalTarget; }
        }

        public UnitTargetOrder(T order, IMortalTarget target)
            : base(order, target) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

