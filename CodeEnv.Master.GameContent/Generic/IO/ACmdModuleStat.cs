// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACmdModuleStat.cs
// Immutable abstract AEquipmentStat for Command Module equipment.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable abstract AEquipmentStat for Command Module equipment.
    /// </summary>
    public abstract class ACmdModuleStat : AEquipmentStat {

        /// <summary>
        /// The maximum effectiveness of the command staff.
        /// <remarks>Does not include contributions from Heroes.</remarks>
        /// </summary>
        public float MaxCmdStaffEffectiveness { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ACmdModuleStat" /> class.
        /// </summary>
        /// <param name="name">The display name of the Equipment.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="level">The improvement level of this stat.</param>
        /// <param name="size">The physical size of the equipment.</param>
        /// <param name="mass">The mass of the equipment.</param>
        /// <param name="pwrRqmt">The power required to operate the equipment.</param>
        /// <param name="hitPts">The hit points contributed to the survivability of the item.</param>
        /// <param name="expense">The expense required to operate this equipment.</param>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="maxCmdStaffEffectiveness">The maximum effectiveness of the command staff.</param>
        public ACmdModuleStat(string name, AtlasID imageAtlasID, string imageFilename, string description, Level level, float size, float mass,
            float pwrRqmt, float hitPts, float expense, float maxCmdStaffEffectiveness)
            : base(name, imageAtlasID, imageFilename, description, level, size, mass, pwrRqmt, hitPts, Constants.ZeroF, expense, isDamageable: false) {
            MaxCmdStaffEffectiveness = maxCmdStaffEffectiveness;
        }

        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(ACmdModuleStat left, ACmdModuleStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(ACmdModuleStat left, ACmdModuleStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + MaxHitPoints.GetHashCode(); // 31 = another prime number
        ////        hash = hash * 31 + MaxCmdStaffEffectiveness.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        ACmdModuleStat oStat = (ACmdModuleStat)obj;
        ////        return oStat.MaxHitPoints == MaxHitPoints && oStat.MaxCmdStaffEffectiveness == MaxCmdStaffEffectiveness;
        ////    }
        ////    return false;
        ////}

        #endregion

    }
}

