// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BeamProjector.cs
// Weapon that projects a LOS Beam.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Weapon that projects a LOS Beam.
    /// </summary>
    public class BeamProjector : ALOSWeapon {

        /// <summary>
        /// The firing duration in hours.
        /// </summary>
        public float Duration { get { return Stat.Duration; } }

        private ITerminatableOrdnance _activeOrdnance;

        /// <summary>
        /// Initializes a new instance of the <see cref="BeamProjector"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public BeamProjector(WeaponStat stat, string name = null)
            : base(stat, name) {
        }

        public override void CheckActiveOrdnanceTargeting() {
            if (_activeOrdnance != null) {
                if (_activeOrdnance.Target.Owner.IsEnemyOf(Owner)) {
                    _activeOrdnance.Terminate();
                }
            }
        }

        protected override void RecordFiredOrdnance(IOrdnance ordnanceFired) {
            D.Assert(_activeOrdnance == null);
            _activeOrdnance = ordnanceFired as ITerminatableOrdnance;
        }

        protected override void RemoveFiredOrdnanceFromRecord(IOrdnance terminatedOrdnance) {
            D.Assert(_activeOrdnance == terminatedOrdnance);
            _activeOrdnance = null;
        }

        protected override void OnToShowEffectsChanged() {
            if (_activeOrdnance != null) {
                _activeOrdnance.ToShowEffects = ToShowEffects;
            }
        }

        protected override void OnIsOperationalChanged() {
            base.OnIsOperationalChanged();
            if (!IsOperational) {
                CheckActiveOrdnanceTargeting();
            }
        }

    }
}

