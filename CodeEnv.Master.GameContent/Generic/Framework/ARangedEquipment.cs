// --------------------------------------------------------------------------------------------------------------------
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

        public float RangeDistance { get { return Stat.BaseRangeDistance * RangeMultiplier; } }

        protected abstract float RangeMultiplier { get; }

        protected new ARangedEquipmentStat Stat { get { return base.Stat as ARangedEquipmentStat; } }

        public ARangedEquipment(ARangedEquipmentStat stat) : base(stat) { }

    }
}

