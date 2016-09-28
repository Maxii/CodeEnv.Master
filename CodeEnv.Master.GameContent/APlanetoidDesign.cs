// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APlanetoidDesign.cs
// Abstract base class for a Planetoid (Planet and Moon) Design.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// Abstract base class for a Planetoid (Planet and Moon) Design.
    /// </summary>
    public abstract class APlanetoidDesign {

        public string DesignName { get; private set; }

        public IEnumerable<PassiveCountermeasureStat> PassiveCmStats { get; private set; }

        public APlanetoidDesign(string designName, IEnumerable<PassiveCountermeasureStat> passiveCmStats) {
            DesignName = designName;
            PassiveCmStats = passiveCmStats;
        }


    }
}

