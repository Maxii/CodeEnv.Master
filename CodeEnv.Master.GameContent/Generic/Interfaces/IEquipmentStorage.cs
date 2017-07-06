// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IEquipmentStorage.cs
// Interface used by EquipmentStorageIcons to access their Storage, independent of the type of UnitDesign the equipment is being stored for.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface used by EquipmentStorageIcons to access their Storage, 
    /// independent of the type of UnitDesign the equipment is being stored for.
    /// </summary>
    public interface IEquipmentStorage {

        AEquipmentStat GetEquipmentStat(EquipmentSlotID slotID);

        /// <summary>
        /// Replace a stat in storage with the one provided, returning the one replaced.
        /// Either can be null.
        /// </summary>
        /// <param name="slotID">The slot number.</param>
        /// <param name="equipStat">The equip stat. Can be null.</param>
        /// <returns>
        /// The stat that was replaced. Can be null.
        /// </returns>
        AEquipmentStat Replace(EquipmentSlotID slotID, AEquipmentStat equipStat);

    }
}

