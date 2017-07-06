// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FtlDampenerStat.cs
// Stat for FtlDampener Equipment.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Stat for FtlDampener Equipment.
    /// <remarks>Implements value-based Equality and HashCode.</remarks>
    /// </summary>
    public class FtlDampenerStat : ARangedEquipmentStat {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(FtlDampenerStat left, FtlDampenerStat right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(FtlDampenerStat left, FtlDampenerStat right) {
            return !(left == right);
        }

        #endregion

        private const string BasicDescriptionFormat = "Basic {0} sensor.";

        public override EquipmentCategory Category { get { return EquipmentCategory.FtlDampener; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtlDampenerStat" /> class.
        /// </summary>
        /// <param name="name">The name of the RangedEquipment.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The size.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The power needed to operate this equipment.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range category.</param>
        public FtlDampenerStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass,
            float pwrRqmt, float expense, RangeCategory rangeCat)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense, rangeCat, isDamageable: false) {
            D.AssertEqual(RangeCategory.Short, rangeCat);
        }

        /// <summary>
        /// Initializes a new instance of the most basic <see cref="FtlDampenerStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="rangeCat">The range category.</param>
        public FtlDampenerStat(string name, RangeCategory rangeCat)
            : this(name, AtlasID.MyGui, TempGameValues.AnImageFilename, BasicDescriptionFormat.Inject(rangeCat.GetEnumAttributeText())
                  , 0F, 0F, 0F, 0F, rangeCat) {
        }

        #region Object.Equals and GetHashCode Override

        public override int GetHashCode() {
            unchecked {
                int hash = base.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) {
            return base.Equals(obj);
        }

        #endregion

    }
}

