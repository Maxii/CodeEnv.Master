// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ARangedEquipmentStat.cs
// Abstract base class for ranged Equipment stats.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    ///  Abstract base class for ranged Equipment stats.
    /// </summary>
    public abstract class ARangedEquipmentStat : AEquipmentStat {

        public RangeCategory RangeCategory { get; private set; }

        public float BaseRangeDistance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ARangedEquipmentStat"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The size.</param>
        /// <param name="pwrRqmt">The power needed to operate this equipment.</param>
        /// <param name="rangeCat">The range category.</param>
        /// <param name="baseRangeDistance">The base (no owner multiplier applied) range distance in units.</param>
        public ARangedEquipmentStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float pwrRqmt, RangeCategory rangeCat, float baseRangeDistance)
            : base(name, imageAtlasID, imageFilename, description, size, pwrRqmt) {
            RangeCategory = rangeCat;
            BaseRangeDistance = baseRangeDistance;
        }

    }
}

