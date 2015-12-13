// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACombatItemData.cs
// Abstract base class that holds data for Items that can engage in combat and cause weapons to fire.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract base class that holds data for Items that can engage in combat and cause weapons to fire.
    /// </summary>
    public abstract class ACombatItemData /*: AMortalItemData*/{

        private float _maxWeaponsRange;
        /// <summary>
        /// The maximum range of this item's weapons.
        /// </summary>
        public virtual float MaxWeaponsRange {
            get { return _maxWeaponsRange; }
            set { SetProperty<float>(ref _maxWeaponsRange, value, "MaxWeaponsRange"); }
        }

        public ACombatItemData(string name, float mass, float maxHitPts)
            : base(name, mass, maxHitPts) { }


    }
}

