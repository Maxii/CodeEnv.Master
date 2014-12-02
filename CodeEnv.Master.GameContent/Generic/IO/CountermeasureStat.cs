// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CountermeasureStat.cs
// Immutable struct containing externally acquirable values for Countermeasures.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for Countermeasures.
    /// </summary>
    public struct CountermeasureStat {

        static private string _toStringFormat = "{0}: Name[{1}], Strength[{2}], Size[{3}], Power[{4}].";

        private string _rootName;   // = string.Empty cannot use initializers in a struct
        public string RootName {
            get { return _rootName.IsNullOrEmpty() ? "Countermeasure {0}".Inject(Strength) : _rootName; }
        }

        public CombatStrength Strength { get; private set; }

        public float PhysicalSize { get; private set; }

        public float PowerRequirement { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CountermeasureStat"/> struct.
        /// </summary>
        /// <param name="strength">The combat strength of the countermeasure.</param>
        /// <param name="size">The physical size of the countermeasure.</param>
        /// <param name="pwrRqmt">The power required to operate the countermeasure.</param>
        /// <param name="rootName">The root name to use for this countermeasure before adding supplemental attributes.</param>
        public CountermeasureStat(CombatStrength strength, float size, float pwrRqmt, string rootName = Constants.Empty)
            : this() {
            Strength = strength;
            PhysicalSize = size;
            PowerRequirement = pwrRqmt;
            _rootName = rootName;
        }

        public override string ToString() {
            return _toStringFormat.Inject(GetType().Name, RootName, Strength.Combined, PhysicalSize, PowerRequirement);
        }

    }
}

