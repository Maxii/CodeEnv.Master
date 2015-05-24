// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponStat.cs
// Immutable class containing externally acquirable values for Weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable class containing externally acquirable values for Weapons.
    /// </summary>
    public class WeaponStat {

        static private string _toStringFormat = "{0}: Name[{1}], Category[{2}], Strength[{3}], Range[{4}], Accuracy[{5:0.00}], ReloadPeriod[{6:0.#}], Size[{7}], Power[{8}]";

        private string _rootName;   // = string.Empty cannot use initializers in a struct
        public string RootName {
            get { return _rootName.IsNullOrEmpty() ? "Weapon {0}".Inject(Strength) : _rootName; }
        }

        public ArmamentCategory Category { get; private set; }

        public CombatStrength Strength { get; private set; }

        public DistanceRange Range { get; private set; }

        public float Accuracy { get; private set; }

        public float ReloadPeriod { get; private set; }

        public float PhysicalSize { get; private set; }

        public float PowerRequirement { get; private set; }

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
        /// <param name="rootName">The root name to use for this weapon before adding supplemental attributes.</param>
        public WeaponStat(ArmamentCategory category, CombatStrength strength, DistanceRange range, float accuracy, float reloadPeriod, float size, float pwrRqmt, string rootName = Constants.Empty) {
            Category = category;
            Strength = strength;
            Range = range;
            Arguments.ValidateForRange(accuracy, Constants.ZeroF, Constants.OneF);
            Accuracy = accuracy;
            ReloadPeriod = reloadPeriod;
            PhysicalSize = size;
            PowerRequirement = pwrRqmt;
            _rootName = rootName;
        }

        public override string ToString() {
            return _toStringFormat.Inject(GetType().Name, RootName, Category.GetName(), Strength.Combined,
                Range.GetName(), Accuracy, ReloadPeriod, PhysicalSize, PowerRequirement);
        }

    }
}

