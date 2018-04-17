// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdModuleStat.cs
// Immutable AEquipmentStat for Fleet Command Module equipment.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

using CodeEnv.Master.Common;

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Immutable AEquipmentStat for Fleet Command Module equipment.
    /// </summary>
    public class FleetCmdModuleStat : ACmdModuleStat {

        ////public override EquipmentCategory Category { get { return EquipmentCategory.FleetCmdModule; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCmdModuleStat" /> class.
        /// </summary>
        /// <param name="name">The display name of the Equipment.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="size">The physical size of the equipment.</param>
        /// <param name="mass">The mass of the equipment.</param>
        /// <param name="pwrRqmt">The power required to operate the equipment.</param>
        /// <param name="hitPts">The hit points contributed to the survivability of the item.</param>
        /// <param name="constructCost">The cost in production units to produce this equipment.</param>
        /// <param name="expense">The expense required to operate this equipment.</param>
        /// <param name="maxCmdStaffEffectiveness">The maximum effectiveness of the command staff.</param>
        public FleetCmdModuleStat(string name, AtlasID imageAtlasID, string imageFilename, string description, EquipStatID id,
            float size, float mass, float pwrRqmt, float hitPts, float constructCost, float expense, float maxCmdStaffEffectiveness)
            : base(name, imageAtlasID, imageFilename, description, id, size, mass, pwrRqmt, hitPts, constructCost, expense, maxCmdStaffEffectiveness) {
        }


        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(FleetCmdModuleStat left, FleetCmdModuleStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(FleetCmdModuleStat left, FleetCmdModuleStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
        ////        return base.GetHashCode();
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    return base.Equals(obj);
        ////}

        #endregion


    }
}

