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
    using CodeEnv.Master.Common;

    /// <summary>
    /// An Element's Sensor. Can be Short, Medium or Long range.
    /// </summary>
    public class Sensor : APropertyChangeTracking {

        static private string _toStringFormat = "{0}: Name[{1}], Operational[{2}], Range[{3:0.#}], Size[{4:0.#}], Power[{5:0.#}]";

        private static string _nameFormat = "{0}({1:0.#})";

        public event Action<Sensor> onIsOperationalChanged;

        public ISensorRangeMonitor RangeMonitor { get; set; }

        private bool _isAnyEnemyInRange;
        public bool IsAnyEnemyInRange {
            get { return _isAnyEnemyInRange; }
            set { SetProperty<bool>(ref _isAnyEnemyInRange, value, "IsAnyEnemyInRange", OnIsAnyEnemyInRangeChanged); }
        }

        private bool _isOperational;
        public bool IsOperational {
            get { return _isOperational; }
            set { SetProperty<bool>(ref _isOperational, value, "IsOperational", OnIsOperationalChanged); }
        }

        public DistanceRange Range { get { return _stat.Range; } }

        public string Name {
            get {
                if (RangeMonitor == null) { return _stat.RootName; }
                return _nameFormat.Inject(_stat.RootName, Range.GetSensorRange(RangeMonitor.ParentCommand.Owner));
            }
        }

        public float PhysicalSize { get { return _stat.PhysicalSize; } }

        public float PowerRequirement { get { return _stat.PowerRequirement; } }

        private SensorStat _stat;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sensor" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public Sensor(SensorStat stat) {
            _stat = stat;
        }

        private void OnIsAnyEnemyInRangeChanged() {
            // TODO
        }

        private void OnIsOperationalChanged() {
            if (onIsOperationalChanged != null) {
                onIsOperationalChanged(this);
            }
        }

        public override string ToString() {
            return _toStringFormat.Inject(GetType().Name, Name, IsOperational, Range.GetName(), PhysicalSize, PowerRequirement);
        }

    }
}

