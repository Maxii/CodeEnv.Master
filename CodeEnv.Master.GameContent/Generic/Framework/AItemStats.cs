// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemStats.cs
// Abstract base class containing values and settings for building Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class containing values and settings for building Items.
    /// </summary>
    public abstract class AItemStats {

        public string Name { get; set; }
        public float MaxHitPoints { get; set; }
        public CombatStrength Strength { get; set; }

    }
}

