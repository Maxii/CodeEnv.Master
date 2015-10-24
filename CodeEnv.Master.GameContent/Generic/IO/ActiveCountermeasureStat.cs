// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ActiveCountermeasureStat.cs
// Immutable Stat for an active countermeasure.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable Stat for an active countermeasure.
    /// </summary>
    public class ActiveCountermeasureStat : ARangedEquipmentStat {

        private static string _toStringFormat = "{0}({1})";

        public WDVStrength[] InterceptStrengths { get; private set; }

        public float InterceptAccuracy { get; private set; }

        public float ReloadPeriod { get; private set; }

        public DamageStrength DamageMitigation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveCountermeasureStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The size.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The PWR RQMT.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range cat.</param>
        /// <param name="baseRangeDistance">The base range distance.</param>
        /// <param name="interceptStrengths">The intercept strengths.</param>
        /// <param name="accuracy">The accuracy.</param>
        /// <param name="reloadPeriod">The reload period.</param>
        /// <param name="damageMitigation">The damage mitigation.</param>
        public ActiveCountermeasureStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass,
            float pwrRqmt, float expense, RangeCategory rangeCat, float baseRangeDistance, WDVStrength[] interceptStrengths,
            float accuracy, float reloadPeriod, DamageStrength damageMitigation)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense, rangeCat, baseRangeDistance) {
            // confirm if more than one interceptStrength, that they each contain a unique WDVCategory
            D.Assert(interceptStrengths.Length == interceptStrengths.Select(intS => intS.Category).Distinct().Count(), "Duplicate Categories found.");
            InterceptStrengths = interceptStrengths;
            InterceptAccuracy = accuracy;
            ReloadPeriod = reloadPeriod;
            DamageMitigation = damageMitigation;
        }

        public override string ToString() {
            string interceptTypesMsg = InterceptStrengths.Select(intS => intS.Category.GetEnumAttributeText()).Concatenate();
            return _toStringFormat.Inject(Name, interceptTypesMsg);
        }

        #region ActiveCM Firing Solutions Check Job Archive

        // Note: Removed to allow elimination of ActiveCM CheckFiringSolutionJobs. If an ActiveCM
        // might not be able to engage (bear on) a target, then no firing solutions was a possibility
        // which then required the expensive and numerous check jobs.
        /// <summary>
        /// How frequently this CM can bear on a qualified threat and engage it.
        /// <remarks>Simulates having a hull mount with limited field of fire.</remarks>
        /// </summary>  
        //public float EngagePercent { get; private set; }

        #endregion

    }
}

