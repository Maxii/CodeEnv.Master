// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PassiveCountermeasureStat.cs
// Immutable Stat for passive countermeasures.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable Stat for passive countermeasures.
    /// </summary>
    public class PassiveCountermeasureStat : AEquipmentStat {

        public DamageStrength DamageMitigation { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="PassiveCountermeasureStat"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The size.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The PWR RQMT.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="damageMitigation">The damage mitigation.</param>
        public PassiveCountermeasureStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass, float pwrRqmt, float expense, DamageStrength damageMitigation)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense) {
            DamageMitigation = damageMitigation;
        }

        /// <summary>
        /// Initializes a new instance of the most basic <see cref="PassiveCountermeasureStat"/> class.
        /// </summary>
        public PassiveCountermeasureStat()
            : this("BasicPassiveCM", AtlasID.MyGui, TempGameValues.AnImageFilename, "BasicDescription..", 0F, 0F, 0F, 0F, new DamageStrength(1F, 1F, 1F)) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

