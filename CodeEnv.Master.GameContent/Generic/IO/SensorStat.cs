// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SensorStat.cs
// Immutable class containing externally acquirable values for Sensors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable class containing externally acquirable values for Sensors.
    /// </summary>
    public class SensorStat : ARangedEquipmentStat {

        private static string _toStringFormat = "{0}: Name[{1}], Range[{2}({3:0.})].";

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorStat" /> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The physical size of the sensor.</param>
        /// <param name="pwrRqmt">The power required to operate the sensor.</param>
        /// <param name="rangeCat">The range category of the sensor.</param>
        /// <param name="baseRangeDistance">The base (no owner multiplier applied) range distance in units.</param>
        public SensorStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float pwrRqmt, RangeCategory rangeCat, float baseRangeDistance)
            : base(name, imageAtlasID, imageFilename, description, size, pwrRqmt, rangeCat, baseRangeDistance) {
            Validate();
        }

        private void Validate() {
            Arguments.ValidateForRange(BaseRangeDistance, RangeCategory.__GetBaseSensorRangeSpread());
        }

        public override string ToString() {
            return _toStringFormat.Inject(typeof(Sensor).Name, Name, RangeCategory.GetEnumAttributeText(), BaseRangeDistance);
        }

    }
}

