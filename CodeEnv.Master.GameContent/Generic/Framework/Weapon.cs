// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Weapon.cs
// Data container class holding the characteristics of an Element's Weapon.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Data container class holding the characteristics of an Element's Weapon.
    /// </summary>
    public class Weapon {

        public RangeTrackerID TrackerID { get; set; }

        public WeaponCategory Category { get; private set; }

        public int Model { get; private set; }

        public float Range { get; set; }

        public float ReloadPeriod { get; set; }

        public float Damage { get; set; }

        public float PhysicalSize { get; set; }

        public float PowerRequirements { get; set; }

        public Weapon(WeaponCategory category, int model) {
            Category = category;
            Model = model;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

