// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AElementStats.cs
// Abstract base class containing values and settings for Elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class containing values and settings for Elements.
    /// </summary>
    public abstract class AElementStats {

        public IList<Weapon> Weapons { get; set; }
        public float MaxHitPoints { get; set; }
        public float CurrentHitPoints { get; set; }
        public CombatStrength Strength { get; set; }
        public float Mass { get; set; }
        public string Name { get; set; }
        // Parent names are assigned when the ElementModel is assigned to a Command
        // Owners are assigned when the ElementModel is assigned to a Command
        // Transforms are assigned when ElementData gets assigned to an ElementModel

    }
}

