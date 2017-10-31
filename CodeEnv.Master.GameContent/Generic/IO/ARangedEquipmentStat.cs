// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ARangedEquipmentStat.cs
// Immutable abstract base class for ranged Equipment stats.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Immutable abstract base class for ranged Equipment stats.
    /// <remarks>Implements value-based Equality and HashCode.</remarks>
    /// </summary>
    public abstract class ARangedEquipmentStat : AEquipmentStat {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(ARangedEquipmentStat left, ARangedEquipmentStat right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(ARangedEquipmentStat left, ARangedEquipmentStat right) {
            return !(left == right);
        }

        #endregion

        public RangeCategory RangeCategory { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ARangedEquipmentStat" /> class.
        /// </summary>
        /// <param name="name">The name of the RangedEquipment.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The size.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The power needed to operate this equipment.</param>
        /// <param name="constructionCost">The cost to produce.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range category.</param>
        /// <param name="refitBenefit">The refit benefit.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public ARangedEquipmentStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass,
            float pwrRqmt, float constructionCost, float expense, RangeCategory rangeCat, int refitBenefit, bool isDamageable)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, constructionCost, expense, refitBenefit, isDamageable) {
            RangeCategory = rangeCat;
        }

        #region Object.Equals and GetHashCode Override

        public override int GetHashCode() {
            unchecked {
                int hash = base.GetHashCode();
                hash = hash * 31 + RangeCategory.GetHashCode(); // 31 = another prime number
                return hash;
            }
        }

        public override bool Equals(object obj) {
            if (base.Equals(obj)) {
                ARangedEquipmentStat oStat = (ARangedEquipmentStat)obj;
                return oStat.RangeCategory == RangeCategory;
            }
            return false;
        }

        #endregion
    }
}

