// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BeamWeaponStat.cs
// Immutable class containing externally acquirable values for BeamWeapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable class containing externally acquirable values for BeamWeapons.
    /// </summary>
    [System.Obsolete]
    public class BeamWeaponStat : WeaponStat {

        private static string _toStringAddendumFormat = ", Duration[{0:0.0}]";

        public float Duration { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponStat" /> struct.
        /// </summary>
        /// <param name="category">The ArmamentCategory this weapon belongs too.</param>
        /// <param name="strength">The combat strength of the weapon.</param>
        /// <param name="range">The range of the weapon.</param>
        /// <param name="accuracy">The accuracy of the weapon. Range 0...1.0</param>
        /// <param name="reloadPeriod">The time it takes to reload the weapon in hours.</param>
        /// <param name="size">The physical size of the weapon.</param>
        /// <param name="pwrRqmt">The power required to operate the weapon.</param>
        /// <param name="duration">The firing duration in hours.</param>
        /// <param name="rootName">The root name to use for this weapon before adding supplemental attributes.</param>
        public BeamWeaponStat(WDVCategory category, CombatStrength strength, RangeCategory range, float accuracy, float reloadPeriod, float size, float pwrRqmt, float duration = Constants.ZeroF, string rootName = Constants.Empty)
            : base(category, strength, range, accuracy, reloadPeriod, size, pwrRqmt, rootName) {
            Duration = duration;
        }

        public override string ToString() {
            return base.ToString() + _toStringAddendumFormat.Inject(Duration);
        }

    }
}

