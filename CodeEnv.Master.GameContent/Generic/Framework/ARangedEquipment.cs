﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ARangedEquipment.cs
// Abstract base class for Ranged Equipment such as Sensors and Weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract base class for Ranged Equipment such as Sensors and Weapons.
    /// </summary>
    public abstract class ARangedEquipment : AEquipment {

        public RangeCategory RangeCategory { get { return Stat.RangeCategory; } }

        /// <summary>
        /// The equipment's range in units adjusted for any range modifiers from owners, etc.
        /// </summary>
        public float RangeDistance { get { return Stat.BaseRangeDistance * RangeMultiplier; } }

        protected abstract float RangeMultiplier { get; }

        protected new ARangedEquipmentStat Stat { get { return base.Stat as ARangedEquipmentStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ARangedEquipment"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public ARangedEquipment(ARangedEquipmentStat stat, string name = null) : base(stat, name) { }

    }
}

