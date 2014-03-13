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

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Data container class holding the characteristics of an Element's Weapon.
    /// </summary>
    public class Weapon {

        public Guid ID { get; private set; }

        public Guid TrackerID { get; set; }

        public bool IsOperational { get; set; }

        public string Name { get { return Category.GetName() + Constants.Underscore + "Model" + Model; } }

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
            ID = Guid.NewGuid();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

