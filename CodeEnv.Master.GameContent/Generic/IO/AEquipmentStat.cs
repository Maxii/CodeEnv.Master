// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AEquipmentStat.cs
// Abstract base class for Equipment stats.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract base class for Equipment stats.
    /// </summary>
    public abstract class AEquipmentStat {

        public string Name { get; private set; }

        public AtlasID ImageAtlasID { get; private set; }

        public string ImageFilename { get; private set; }

        public string Description { get; private set; }

        public float PhysicalSize { get; private set; }

        public float PowerRequirement { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AEquipmentStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The physical size of the device.</param>
        /// <param name="pwrRqmt">The power required to operate the device.</param>
        public AEquipmentStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float pwrRqmt) {
            Name = name;
            ImageAtlasID = imageAtlasID;
            ImageFilename = imageFilename;
            Description = description;
            PhysicalSize = size;
            PowerRequirement = pwrRqmt;
        }

    }
}

