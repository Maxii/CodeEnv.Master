// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SensorStat.cs
// Immutable stat containing externally acquirable values for Sensors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat containing externally acquirable values for Sensors.
    /// </summary>
    public class SensorStat : ARangedEquipmentStat {

        private const string DebugNameFormat = "{0}(Range[{1}]).";

        public override string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(base.DebugName, RangeCategory.GetEnumAttributeText());
                }
                return _debugName;
            }
        }

        public override EquipmentCategory Category { get { return EquipmentCategory.Sensor; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorStat" /> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="level">The improvement level of this stat.</param>
        /// <param name="size">The physical size of the sensor.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The power required to operate the sensor.</param>
        /// <param name="hitPts">The hit points contributed to the survivability of the item.</param>
        /// <param name="constructionCost">The cost to produce.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range category of the sensor.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public SensorStat(string name, AtlasID imageAtlasID, string imageFilename, string description, Level level, float size, float mass,
            float pwrRqmt, float hitPts, float constructionCost, float expense, RangeCategory rangeCat, bool isDamageable)
            : base(name, imageAtlasID, imageFilename, description, level, size, mass, pwrRqmt, hitPts, constructionCost, expense, rangeCat, isDamageable) {
        }

        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(SensorStat left, SensorStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(SensorStat left, SensorStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        return base.GetHashCode();
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    return base.Equals(obj);
        ////}

        #endregion


    }
}

