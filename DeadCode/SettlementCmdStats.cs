// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdStats.cs
// Class containing values and settings for building Settlement Commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Class containing values and settings for building Settlement Commands.
    /// </summary>
    [Obsolete]
    public class SettlementCmdStats : ACommandStats {

        public int Population { get; set; }
        public int CapacityUsed { get; set; }
        public OpeResourceYield ResourcesUsed { get; set; }
        public RareResourceYield SpecialResourcesUsed { get; set; }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

