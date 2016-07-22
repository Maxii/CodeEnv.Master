// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BaseOrder.cs
// Order for a Base - Settlement or Starbase.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Order for a Base - Settlement or Starbase.
    /// </summary>
    public class BaseOrder {

        public IUnitAttackable Target { get; private set; }

        /// <summary>
        /// The source of this order.
        /// </summary>
        public OrderSource Source { get; private set; }

        public BaseDirective Directive { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseOrder" /> class.
        /// </summary>
        /// <param name="directive">The order directive.</param>
        /// <param name="source">The source of this order.</param>
        /// <param name="target">The target of this order. Default is null.</param>
        public BaseOrder(BaseDirective directive, OrderSource source, IUnitAttackable target = null) {
            D.Assert(source != OrderSource.Captain);
            Directive = directive;
            Source = source;
            Target = target;
        }

        public override string ToString() { // IMPROVE
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

