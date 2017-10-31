// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ElementSensor.cs
// SR Sensor for a UnitElement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// SR Sensor for a UnitElement.
    /// </summary>
    public class ElementSensor : ASensor {

        public new IElementSensorRangeMonitor RangeMonitor {
            get { return base.RangeMonitor as IElementSensorRangeMonitor; }
            set { base.RangeMonitor = value; }
        }

        public ElementSensor(SensorStat stat, string name = null) : base(stat, name) {
            D.AssertEqual(RangeCategory.Short, stat.RangeCategory);
        }

        public override bool AreSpecsEqual(AEquipmentStat otherStat) {
            return Stat == otherStat as SensorStat;
        }
    }
}

