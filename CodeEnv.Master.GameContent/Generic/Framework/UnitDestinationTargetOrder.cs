// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitDestinationTargetOrder.cs
// Order that requires IDestinationTarget info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Order that requires IDestinationTarget info.
    /// </summary>
    public class UnitDestinationTargetOrder<T> : UnitOrder<T> where T : struct {

        public IDestination Target { get; private set; }

        public UnitDestinationTargetOrder(T order, IDestination target)
            : base(order) {
            Target = target;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

