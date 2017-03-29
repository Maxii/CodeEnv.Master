// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ASensor.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public abstract class ASensor : ARangedEquipment {

        private IASensorRangeMonitor _rangeMonitor;
        public IASensorRangeMonitor RangeMonitor {
            get { return _rangeMonitor; }
            set { SetProperty<IASensorRangeMonitor>(ref _rangeMonitor, value, "RangeMonitor"); }
        }

        public override string DebugName {
            get {
                return RangeMonitor != null ? DebugNameFormat.Inject(RangeMonitor.DebugName, Name) : Name;
            }
        }

        protected new SensorStat Stat { get { return base.Stat as SensorStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sensor" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public ASensor(SensorStat stat, string name = null)
            : base(stat, name) {
        }

        // Copy Constructor makes no sense when a RangeMonitor must be attached

        /***************************************************************************************************************************************
        * ParentDeath Note: No need to track it as the parent element will turn off the operational state of all equipment when it initiates dying.
        ***************************************************************************************************************************************/
        /*****************************************************************************************************************************
        * This sensor does not need to track Owner changes. When the owner of the item with this sensor changes, the sensor's 
        * RangeMonitor drops and then reacquires all detectedItems, notifying them as it does so. As a result, all reacquired items
        * are categorized correctly. When the owner of an item detected by this sensor changes, the Monitor simply re-categorizes 
        * the detectedItem into the right list - enemy or non-enemy. The detectedItem itself is responsible for making any internal 
        * changes as a result of its ownership change.
        * *****************************************************************************************************************************/

        public sealed override string ToString() { return Stat.ToString(); }

    }
}

