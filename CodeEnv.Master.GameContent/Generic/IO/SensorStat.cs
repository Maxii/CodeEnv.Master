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
    /// <remarks>Implements value-based Equality and HashCode.</remarks>
    /// </summary>
    public class SensorStat : ARangedEquipmentStat {

        private const string DebugNameFormat = "{0}(Range[{1}]).";

        private const string BasicDescriptionFormat = "Basic {0} sensor.";

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(SensorStat left, SensorStat right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(SensorStat left, SensorStat right) {
            return !(left == right);
        }

        #endregion

        public override string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(base.DebugName, RangeCategory.GetEnumAttributeText());
                }
                return _debugName;
            }
        }

        public override EquipmentCategory Category {
            get { return RangeCategory == RangeCategory.Short ? EquipmentCategory.ElementSensor : EquipmentCategory.CommandSensor; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorStat" /> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The physical size of the sensor.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The power required to operate the sensor.</param>
        /// <param name="constructionCost">The cost to produce.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range category of the sensor.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public SensorStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass, float pwrRqmt,
            float constructionCost, decimal expense, RangeCategory rangeCat, bool isDamageable)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, constructionCost, expense, rangeCat, isDamageable) {
        }

        /// <summary>
        /// Initializes a new instance of the most basic <see cref="SensorStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="rangeCat">The range category of the sensor.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public SensorStat(string name, RangeCategory rangeCat, bool isDamageable)
            : this(name, AtlasID.MyGui, TempGameValues.AnImageFilename,
                  BasicDescriptionFormat.Inject(rangeCat.GetEnumAttributeText()), 0F, 0F, 0F, 1F, Constants.ZeroCurrency, rangeCat, isDamageable) {
        }

        #region Object.Equals and GetHashCode Override

        public override int GetHashCode() {
            unchecked {
                return base.GetHashCode();
            }
        }

        public override bool Equals(object obj) {
            return base.Equals(obj);
        }

        #endregion

    }
}

