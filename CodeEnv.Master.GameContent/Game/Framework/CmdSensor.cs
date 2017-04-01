// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CmdSensor.cs
// MR or LR Sensor for a UnitCmd.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// MR or LR Sensor for a UnitCmd.
    /// </summary>
    public class CmdSensor : ASensor {

        public new ICmdSensorRangeMonitor RangeMonitor {
            get { return base.RangeMonitor as ICmdSensorRangeMonitor; }
            set { base.RangeMonitor = value; }
        }

        public CmdSensor(SensorStat stat, string name = null) : base(stat, name) {
            D.AssertNotEqual(RangeCategory.Short, stat.RangeCategory);
        }

    }
}

