// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitOrder.cs
// Order that requires no other info besides the order itself.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Order that requires no other info besides the order itself.
    /// </summary>
    /// <typeparam name="T">The enum type of the order.</typeparam>
    public class UnitOrder<T> where T : struct {

        public UnitOrder<T> NextOrder { get; set; } // not currently used

        public T Order { get; private set; }

        public UnitOrder(T order) {
            Order = order;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

