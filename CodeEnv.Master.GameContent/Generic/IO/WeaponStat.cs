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

        private string _baseName;   // initializes to null
        public string BaseName {
            get {
                return _baseName.IsNullOrEmpty() ? string.Empty : _baseName;
            }
        }

        public CombatStrength Strength { get; private set; }

        public float Range { get; private set; }

        public float ReloadPeriod { get; private set; }

        public float PhysicalSize { get; private set; }

        public float PowerRequirement { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeaponStat"/> struct.
        /// </summary>
        /// <param name="baseName">Name of the base.</param>
        /// <param name="strength">The strength.</param>
        /// <param name="range">The range.</param>
        /// <param name="reloadPeriod">The reload period.</param>
        /// <param name="size">The size.</param>
        /// <param name="pwrRqmt">The PWR RQMT.</param>
        public WeaponStat(string baseName, CombatStrength strength, float range, float reloadPeriod, float size, float pwrRqmt)
            : this() {
            _baseName = baseName;
            Strength = strength;
            Range = range;
            ReloadPeriod = reloadPeriod;
            PhysicalSize = size;
            PowerRequirement = pwrRqmt;
        }

        public override string ToString() {
            return _toStringFormat.Inject(GetType().Name, BaseName, Strength.Combined, Range, ReloadPeriod, PhysicalSize, PowerRequirement);
        }

    }
}

