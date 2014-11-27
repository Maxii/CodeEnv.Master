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

        public IUnitAttackableTarget Target { get; private set; }

        public BaseDirective Directive { get; private set; }

        public BaseOrder(BaseDirective directive, IUnitAttackableTarget target = null) {
            Directive = directive;
            Target = target;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

