﻿// --------------------------------------------------------------------------------------------------------------------
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
                    _debugName = DebugNameFormat.Inject(base.DebugName, RangeCategory.GetValueName());
                }
                return _debugName;
            }
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
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range category of the sensor.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public SensorStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass, float pwrRqmt,
            float expense, RangeCategory rangeCat, bool isDamageable)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense, rangeCat, isDamageable) { }

        /// <summary>
        /// Initializes a new instance of the most basic <see cref="SensorStat"/> class.
        /// </summary>
        public SensorStat(RangeCategory rangeCat)
            : this("BasicSensorStat", AtlasID.MyGui, TempGameValues.AnImageFilename, "BasicDescription..", 0F, 0F, 0F, 0F, rangeCat, true) {
        }

    }
}

