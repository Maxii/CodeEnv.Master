// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponStat.cs
// Immutable struct containing externally acquirable values for Weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for Weapons.
    /// </summary>
    public struct WeaponStat {

        static private string _toStringFormat = "{0}: Name[{1}], Strength[{2}], Range[{3}], ReloadPeriod[{4}], Size[{5}], Power[{6}].";

        private string _rootName;   // = string.Empty cannot use initializers in a struct
        public string RootName {
            get { return _rootName.IsNullOrEmpty() ? "Weapon {0}".Inject(Strength) : _rootName; }
        }

        public CombatStrength Strength { get; private set; }

        public DistanceRange Range { get; private set; }

        public int ReloadPeriod { get; private set; }

        public float PhysicalSize { get; private set; }

        public float PowerRequirement { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponStat"/> struct.
        /// </summary>
        /// <param name="strength">The combat strength of the weapon.</param>
        /// <param name="range">The range of the weapon.</param>
        /// <param name="reloadPeriod">The time it takes to reload the weapon in hours.</param>
        /// <param name="size">The physical size of the weapon.</param>
        /// <param name="pwrRqmt">The power required to operate the weapon.</param>
        /// <param name="rootName">The root name to use for this weapon before adding supplemental attributes.</param>
        public WeaponStat(CombatStrength strength, DistanceRange range, int reloadPeriod, float size, float pwrRqmt, string rootName = Constants.Empty)
            : this() {
            Strength = strength;
            Range = range;
            ReloadPeriod = reloadPeriod;
            PhysicalSize = size;
            PowerRequirement = pwrRqmt;
            _rootName = rootName;
        }

        public override string ToString() {
            return _toStringFormat.Inject(GetType().Name, RootName, Strength.Combined, Range.GetName(), ReloadPeriod, PhysicalSize, PowerRequirement);
        }

    }
}

