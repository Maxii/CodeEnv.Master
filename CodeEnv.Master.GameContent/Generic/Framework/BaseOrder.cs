// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BaseOrder.cs
// Generic order for a Base - Settlement or Starbase.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Generic order for a Base - Settlement or Starbase.
    /// </summary>
    public class BaseOrder<T> where T : struct {

        public IMortalTarget Target { get; private set; }

        public T Directive { get; private set; }

        public BaseOrder(T directive, IMortalTarget target) {
            Directive = directive;
            Target = target;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

