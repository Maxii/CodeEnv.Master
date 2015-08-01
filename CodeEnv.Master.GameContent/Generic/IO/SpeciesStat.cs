// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SpeciesStat.cs
//  Immutable struct containing externally acquirable values for Species.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for Species.
    /// </summary>
    public struct SpeciesStat {

        public Species Species { get; private set; }

        public string Name { get { return Species.GetValueName(); } }

        public string Name_Plural { get; private set; }

        public string Description { get; private set; }

        public AtlasID ImageAtlasID { get; private set; }

        public string ImageFilename { get; private set; }

        public float SensorRangeMultiplier { get; private set; }

        public float WeaponRangeMultiplier { get; private set; }

        public float ActiveCountermeasureRangeMultiplier { get; private set; }

        public float WeaponReloadPeriodMultiplier { get; private set; }

        public float CountermeasureReloadPeriodMultiplier { get; private set; }

        public SpeciesStat(Species species, string pluralName, string description, AtlasID imageAtlasID, string imageFilename,
            float sensorRangeMultiplier, float weaponRangeMultiplier, float activeCountermeasureRangeMultiplier, float weaponReloadPeriodMultiplier, float countermeasureReloadPeriodMultiplier)
            : this() {
            Species = species;
            Name_Plural = pluralName;
            Description = description;
            ImageAtlasID = imageAtlasID;
            ImageFilename = imageFilename;
            SensorRangeMultiplier = sensorRangeMultiplier;
            WeaponRangeMultiplier = weaponRangeMultiplier;
            ActiveCountermeasureRangeMultiplier = activeCountermeasureRangeMultiplier;
            WeaponReloadPeriodMultiplier = weaponReloadPeriodMultiplier;
            CountermeasureReloadPeriodMultiplier = countermeasureReloadPeriodMultiplier;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

