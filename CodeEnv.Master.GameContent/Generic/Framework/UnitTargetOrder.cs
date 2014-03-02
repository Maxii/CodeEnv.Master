// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitTargetOrder.cs
// Order that requires ITarget info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Order that requires ITarget info.
    /// </summary>
    public class UnitTargetOrder<T> : UnitDestinationTargetOrder<T> where T : struct {

        public new ITarget Target {
            get { return base.Target as ITarget; }
        }

        public UnitTargetOrder(T order, ITarget target)
            : base(order, target) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

