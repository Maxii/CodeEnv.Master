// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponDesign.cs
// Wrapper holding the design attributes of a weapon including its WeaponStat
// along with the SlotID and Facing of its future WeaponMount.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Wrapper holding the design attributes of a weapon including its WeaponStat
    /// along with the SlotID and Facing of its future WeaponMount.
    /// </summary>
    public class WeaponDesign {

        public AWeaponStat WeaponStat { get; private set; }

        public MountSlotID MountSlotID { get; private set; }

        public WeaponDesign(AWeaponStat stat, MountSlotID mountSlotID) {
            WeaponStat = stat;
            MountSlotID = mountSlotID;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

