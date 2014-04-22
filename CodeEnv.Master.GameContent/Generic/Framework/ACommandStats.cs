// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACommandStats.cs
// Abstract base class containing values and settings for building Commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class containing values and settings for building Commands.
    /// </summary>
    public abstract class ACommandStats : AItemStats {

        public int MaxCmdEffectiveness { get; set; }
        public Formation UnitFormation { get; set; }

    }
}

