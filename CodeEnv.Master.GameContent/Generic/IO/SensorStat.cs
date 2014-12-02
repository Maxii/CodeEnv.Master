// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SensorStat.cs
// Immutable struct containing externally acquirable values for Sensors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for Sensors.
    /// </summary>
    public struct SensorStat {

        static private string _toStringFormat = "{0}: Name[{1}], Range[{2}], Size[{3}], Power[{4}].";

        private string _rootName;   // = string.Empty cannot use initializers in a struct
        public string RootName {
            get { return _rootName.IsNullOrEmpty() ? "{0}RangeSensor".Inject(Range.GetName()) : _rootName; }
        }

        public DistanceRange Range { get; private set; }

        public float PhysicalSize { get; private set; }

        public float PowerRequirement { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorStat"/> struct.
        /// </summary>
        /// <param name="range">The range of the sensor.</param>
        /// <param name="size">The physical size of the sensor.</param>
        /// <param name="pwrRqmt">The power required to operate the sensor.</param>
        /// <param name="rootName">The root name to use for this sensor before adding supplemental attributes.</param>
        public SensorStat(DistanceRange range, float size, float pwrRqmt, string rootName = Constants.Empty)
            : this() {
            Range = range;
            PhysicalSize = size;
            PowerRequirement = pwrRqmt;
            _rootName = rootName;
        }

        public override string ToString() {
            return _toStringFormat.Inject(GetType().Name, RootName, Range.GetName(), PhysicalSize, PowerRequirement);
        }

    }
}

