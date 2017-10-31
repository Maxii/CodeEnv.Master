// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AEquipmentStat.cs
// Immutable abstract base class for Equipment stats.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable abstract base class for Equipment stats.
    /// <remarks>Implements value-based Equality and HashCode.</remarks>
    /// </summary>
    public abstract class AEquipmentStat {

        private const string DebugNameFormat = "{0}.{1}";

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(AEquipmentStat left, AEquipmentStat right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(AEquipmentStat left, AEquipmentStat right) {
            return !(left == right);
        }

        #endregion

        protected string _debugName;
        public virtual string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(Name, GetType().Name);
                }
                return _debugName;
            }
        }

        /// <summary>
        /// Display name of the equipment.
        /// </summary>
        public string Name { get; private set; }

        public abstract EquipmentCategory Category { get; }

        public AtlasID ImageAtlasID { get; private set; }

        public string ImageFilename { get; private set; }

        public string Description { get; private set; }

        /// <summary>
        /// The physical space this equipment requires or, in the case of a hull,
        /// the physical space provided.
        /// </summary>
        public float Size { get; private set; }

        public float Mass { get; private set; }

        public float PowerRequirement { get; private set; }

        public float ConstructionCost { get; private set; }

        public float Expense { get; private set; }

        public int RefitBenefit { get; private set; }

        public bool IsDamageable { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AEquipmentStat" /> class.
        /// </summary>
        /// <param name="name">The display name of the Equipment.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The physical size of the equipment.</param>
        /// <param name="mass">The mass of the equipment.</param>
        /// <param name="pwrRqmt">The power required to operate the equipment.</param>
        /// <param name="constructionCost">The cost to produce this equipment.</param>
        /// <param name="expense">The expense required to operate this equipment.</param>
        /// <param name="refitBenefit">The refit benefit.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public AEquipmentStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass,
            float pwrRqmt, float constructionCost, float expense, int refitBenefit, bool isDamageable) {
            Name = name;
            ImageAtlasID = imageAtlasID;
            ImageFilename = imageFilename;
            Description = description;
            Size = size;
            Mass = mass;
            PowerRequirement = pwrRqmt;
            ConstructionCost = constructionCost;
            Expense = expense;
            RefitBenefit = refitBenefit;
            IsDamageable = isDamageable;
        }

        #region Object.Equals and GetHashCode Override

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
                hash = hash * 31 + Name.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + Category.GetHashCode();
                hash = hash * 31 + ImageAtlasID.GetHashCode();
                hash = hash * 31 + ImageFilename.GetHashCode();
                hash = hash * 31 + Description.GetHashCode();
                hash = hash * 31 + Size.GetHashCode();
                hash = hash * 31 + Mass.GetHashCode();
                hash = hash * 31 + PowerRequirement.GetHashCode();
                hash = hash * 31 + ConstructionCost.GetHashCode();
                hash = hash * 31 + Expense.GetHashCode();
                hash = hash * 31 + RefitBenefit.GetHashCode();
                hash = hash * 31 + IsDamageable.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) {
            if (obj == null) { return false; }
            if (ReferenceEquals(obj, this)) { return true; }
            if (obj.GetType() != GetType()) { return false; }

            AEquipmentStat oStat = (AEquipmentStat)obj;
            return oStat.Name == Name && oStat.Category == Category && oStat.ImageAtlasID == ImageAtlasID
                && oStat.ImageFilename == ImageFilename && oStat.Description == Description && oStat.Size == Size && oStat.Mass == Mass
                && oStat.PowerRequirement == PowerRequirement && oStat.ConstructionCost == ConstructionCost && oStat.Expense == Expense
                && oStat.RefitBenefit == RefitBenefit && oStat.IsDamageable == IsDamageable;
        }

        #endregion

        public sealed override string ToString() {
            return DebugName;
        }

    }
}

