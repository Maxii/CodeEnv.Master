// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetDesign.cs
// A Planet Design.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// A Planet Design.
    /// </summary>
    public class PlanetDesign : APlanetoidDesign {

        public PlanetStat Stat { get; private set; }

        public PlanetDesign(string designName, PlanetStat stat, IEnumerable<PassiveCountermeasureStat> passiveCmStats)
            : base(designName, passiveCmStats) {
            Stat = stat;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

