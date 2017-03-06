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
    /// </summary>
    public class FtlDampenerStat : ARangedEquipmentStat {

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
        /// Initializes a new instance of the most basic <see cref="FtlDampenerStat"/> class.
        /// </summary>
        public FtlDampenerStat()
            : this("BasicFtlDampener", AtlasID.MyGui, TempGameValues.AnImageFilename, "BasicDescription..", 0F, 0F, 0F, 0F, RangeCategory.Short) {
        }

    }
}

