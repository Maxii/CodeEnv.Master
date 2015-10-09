// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CountermeasureInterceptSolution.cs
// An firing solution for an ActiveCountermeasure against an IInterceptableOrdnance threat.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// A firing solution for an ActiveCountermeasure against an IInterceptableOrdnance threat.
    /// Not currently used.
    /// </summary>
    [System.Obsolete]
    public class CountermeasureInterceptSolution {

        public IInterceptableOrdnance Threat { get; private set; }

        public ActiveCountermeasure Countermeasure { get; private set; }

        public CountermeasureInterceptSolution(ActiveCountermeasure countermeasure, IInterceptableOrdnance threat) {
            Countermeasure = countermeasure;
            Threat = threat;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

