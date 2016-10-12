// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MoonDesign.cs
// A Moon Design.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// A Moon Design.
    /// </summary>
    public class MoonDesign : APlanetoidDesign {

        public PlanetoidStat Stat { get; private set; }

        public MoonDesign(string designName, PlanetoidStat stat, IEnumerable<PassiveCountermeasureStat> passiveCmStats)
            : base(designName, passiveCmStats) {
            Stat = stat;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

