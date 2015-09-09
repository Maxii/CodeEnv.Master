// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Sensor.cs
// An Element's Sensor. Can be Short, Medium or Long range.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// An Element's Sensor. Can be Short, Medium or Long range.
    /// </summary>
    public class Sensor : ARangedEquipment {

        private static string _editorNameFormat = "{0}[{1}({2:0.})]";

        public ISensorRangeMonitor RangeMonitor { get; set; }

        public override string Name {
            get {
#if UNITY_EDITOR
                return _editorNameFormat.Inject(base.Name, RangeCategory.GetEnumAttributeText(), RangeDistance);
#else 
                return base.Name;
#endif
            }
        }

        protected override float RangeMultiplier {
            get { return RangeMonitor != null ? RangeMonitor.Owner.SensorRangeMultiplier : Constants.OneF; }
        }

        protected new SensorStat Stat { get { return base.Stat as SensorStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sensor" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public Sensor(SensorStat stat)
            : base(stat) {
        }

        // Copy Constructor makes no sense when a RangeMonitor must be attached

        /***************************************************************************************************************************************
                    * ParentDeath Note: No need to track it as the parent element will turn off the operational state of all equipment when it initiates dying.
                    ***************************************************************************************************************************************/
        /*****************************************************************************************************************************
                  * This sensor does not need to track Owner changes. When the owner of the item with this sensor changes, the sensor's 
                  * RangeMonitor drops and then reacquires all detectedItems, notifying them as it does so. As a result, all reacquired items
                  * are categorized correctly. When the owner of an item detected by this sensor changes, the Monitor simply recategorizes 
                  * the detectedItem into the right list - enemy or non-enemy. The detectedItem itself is responsible for making any internal 
                  * changes as a result of its ownership change.
                  * *****************************************************************************************************************************/

        public override string ToString() { return Stat.ToString(); }

    }
}

