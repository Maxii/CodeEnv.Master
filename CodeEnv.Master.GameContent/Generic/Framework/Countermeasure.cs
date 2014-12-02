// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Countermeasure.cs
// A MortalItem's defensive Countermeasures.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// A MortalItem's defensive Countermeasures.
    /// </summary>
    public class Countermeasure : APropertyChangeTracking {

        static private string _toStringFormat = "{0}: Name[{1}], Operational[{2}], Strength[{3:0.#}], Size[{4:0.#}], Power[{5:0.#}]";

        private static string _nameFormat = "{0}_{1:0.#}";

        public event Action<Countermeasure> onIsOperationalChanged;

        private bool _isOperational;
        public bool IsOperational {
            get { return _isOperational; }
            set { SetProperty<bool>(ref _isOperational, value, "IsOperational", OnIsOperationalChanged); }
        }

        public string Name { get { return _nameFormat.Inject(_stat.RootName, _stat.Strength.Combined); } }

        public CombatStrength Strength { get { return _stat.Strength; } }

        public float PhysicalSize { get { return _stat.PhysicalSize; } }

        public float PowerRequirement { get { return _stat.PowerRequirement; } }

        private CountermeasureStat _stat;

        /// <summary>
        /// Initializes a new instance of the <see cref="Countermeasure" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public Countermeasure(CountermeasureStat stat) {
            _stat = stat;
        }

        private void OnIsOperationalChanged() {
            if (onIsOperationalChanged != null) {
                onIsOperationalChanged(this);
            }
        }

        public override string ToString() {
            return _toStringFormat.Inject(GetType().Name, Name, IsOperational, Strength.Combined, PhysicalSize, PowerRequirement);
        }

    }
}

