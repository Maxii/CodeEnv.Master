// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitDestinationOrder.cs
// Order that requires IDestination info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Order that requires IDestination info.
    /// </summary>
    public class UnitDestinationOrder<T> : UnitOrder<T> where T : struct {

        public IDestinationTarget Target { get; private set; }

        public UnitDestinationOrder(T order, IDestinationTarget target)
            : base(order) {
            Target = target;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

