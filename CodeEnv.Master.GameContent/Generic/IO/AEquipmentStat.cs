// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AEquipmentStat.cs
// Immutable abstract base class for AImprovableStats for Equipment.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable abstract base class for AImprovableStats for Equipment.
    /// </summary>
    public abstract class AEquipmentStat : AImprovableStat {

        ////public abstract EquipmentCategory Category { get; }

        public EquipmentCategory Category { get { return ID.Category; } }

        public Level Level { get { return ID.Level; } }

        public EquipStatID ID { get; private set; }

        /// <summary>
        /// The physical space this equipment requires or, in the case of a hull, the physical space provided.
        /// </summary>
        public float Size { get; private set; }

        public float Mass { get; private set; }

        public float PowerRequirement { get; private set; }

        /// <summary>
        /// The hit points contributed to the survivability of the item.
        /// </summary>
        public float HitPoints { get; private set; }

        /// <summary>
        /// The cost in production units to produce this equipment.
        /// </summary>
        public float ConstructionCost { get; private set; }

        /// <summary>
        /// The expense required to operate this equipment.
        /// <remarks>IMPROVE UOM?</remarks>
        /// </summary>
        public float Expense { get; private set; }

        public bool IsDamageable { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AEquipmentStat" /> class.
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
        /// <param name="constructionCost">The cost in production units to produce this equipment.</param>
        /// <param name="expense">The expense required to operate this equipment.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public AEquipmentStat(string name, AtlasID imageAtlasID, string imageFilename, string description, EquipStatID id,
            float size, float mass, float pwrRqmt, float hitPts, float constructionCost, float expense, bool isDamageable)
            : base(name, imageAtlasID, imageFilename, description) {
            ID = id;
            Size = size;
            Mass = mass;
            PowerRequirement = pwrRqmt;
            HitPoints = hitPts;
            ConstructionCost = constructionCost;
            Expense = expense;
            IsDamageable = isDamageable;
        }


        #region Nested Classes

        public struct EquipStatID : IEquatable<EquipStatID> {

            #region Equality Operators Override

            // see C# 4.0 In a Nutshell, page 254

            public static bool operator ==(EquipStatID left, EquipStatID right) {
                return left.Equals(right);
            }

            public static bool operator !=(EquipStatID left, EquipStatID right) {
                return !left.Equals(right);
            }

            #endregion

            public Level Level { get; private set; }

            public EquipmentCategory Category { get; private set; }

            public EquipStatID(EquipmentCategory eCat, Level level) {
                Category = eCat;
                Level = level;
            }

            public EquipStatID(string eCatName, string levelName)
                : this(Enums<EquipmentCategory>.Parse(eCatName), Enums<Level>.Parse(levelName)) { }


            #region Object.Equals and GetHashCode Override

            public override bool Equals(object obj) {
                if (!(obj is EquipStatID)) { return false; }
                return Equals((EquipStatID)obj);
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// See "Page 254, C# 4.0 in a Nutshell."
            /// </summary>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public override int GetHashCode() {
                unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                    int hash = 17;  // 17 = some prime number
                    hash = hash * 31 + Category.GetHashCode(); // 31 = another prime number
                    hash = hash * 31 + Level.GetHashCode();
                    return hash;
                }
            }

            #endregion

            #region IEquatable<EquipStatID> Members

            public bool Equals(EquipStatID other) {
                return Category == other.Category && Level == other.Level;
            }

        }

        #endregion

        #endregion

    }

    #region Value-based Equality Archive
    // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

    ////public static bool operator ==(AEquipmentStat left, AEquipmentStat right) {
    ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
    ////    if (ReferenceEquals(left, right)) { return true; }
    ////    if (((object)left == null) || ((object)right == null)) { return false; }
    ////    return left.Equals(right);
    ////}

    ////public static bool operator !=(AEquipmentStat left, AEquipmentStat right) {
    ////    return !(left == right);
    ////}

    ////public override int GetHashCode() {
    ////    unchecked {
    ////        int hash = base.GetHashCode();
    ////        hash = hash * 31 + Category.GetHashCode(); // 31 = another prime number
    ////        return hash;
    ////    }
    ////}

    ////public override bool Equals(object obj) {
    ////    if (base.Equals(obj)) {
    ////        AEquipmentStat oStat = (AEquipmentStat)obj;
    ////        return oStat.Category == Category;
    ////    }
    ////    return false;
    ////}

    #endregion

}

