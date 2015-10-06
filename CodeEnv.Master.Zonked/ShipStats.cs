// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipStats.cs
// Class containing values and settings for building Ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    ///  Class containing values and settings for building Ships.
    /// </summary>
    [Obsolete]
    public class ShipStats : AElementStats {

        public ShipHullCategory Category { get; set; }
        public ShipCombatStance CombatStance { get; set; }
        public float MaxTurnRate { get; set; }
        public float Drag { get; set; }
        public float FullThrust { get; set; }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

