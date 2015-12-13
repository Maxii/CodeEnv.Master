// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CountermeasureStat.cs
// Immutable class containing externally acquirable values for Countermeasures.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable class containing externally acquirable values for Countermeasures.
    /// </summary>
    [System.Obsolete]
    public class CountermeasureStat : AEquipmentStat {

        private static string _toStringFormat = "{0}: Name[{1}], Strength[{2:0.}].";

        public CombatStrength Strength { get; private set; }

        public float Accuracy { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CountermeasureStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The physical size of the countermeasure.</param>
        /// <param name="pwrRqmt">The power required to operate the countermeasure.</param>
        /// <param name="strength">The combat strength of the countermeasure.</param>
        /// <param name="accuracy">The accuracy of the countermeasure.</param>
        public CountermeasureStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float pwrRqmt, CombatStrength strength, float accuracy)
            : base(name, imageAtlasID, imageFilename, description, size, pwrRqmt) {
            Strength = strength;
            Accuracy = accuracy;
        }

        /// <summary>
        /// Initializes a new instance of the most basic <see cref="CountermeasureStat"/> class.
        /// </summary>
        public CountermeasureStat()
            : this("BasicCM", AtlasID.MyGui, "None", "None", 0F, 0F, new CombatStrength(1F, 1F, 1F), Constants.OneHundredPercent) {
        }

        public override string ToString() {
            return _toStringFormat.Inject(typeof(Countermeasure).Name, Name, Strength.Combined);
        }

    }
}

