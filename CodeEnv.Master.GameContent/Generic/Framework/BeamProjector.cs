﻿// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Weapon that projects a LOS Beam.
    /// </summary>
    public class BeamProjector : ALOSWeapon {

        /// <summary>
        /// The firing duration in hours.
        /// </summary>
        public float Duration { get { return Stat.Duration; } }

        /// <summary>
        /// The maximum inaccuracy of this Weapon's bearing when launched in degrees.
        /// </summary>
        public override float MaxLaunchInaccuracy { get { return Stat.MaxLaunchInaccuracy; } }

        public DamageStrength BeamIntegrity { get { return Stat.BeamIntegrity; } }

        protected new BeamWeaponStat Stat { get { return base.Stat as BeamWeaponStat; } }

        private ITerminatableOrdnance _activeOrdnance;

        /// <summary>
        /// Initializes a new instance of the <see cref="BeamProjector"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public BeamProjector(BeamWeaponStat stat, string name = null)
            : base(stat, name) {
        }

        /// <summary>
        /// Checks to see if any active ordnance is currently targeted on a non-enemy.
        /// </summary>
        public override void CheckActiveOrdnanceTargeting() {
            if (_activeOrdnance != null) {
                if (!_activeOrdnance.Target.IsAttackAllowedBy(Owner)) {
                    _activeOrdnance.Terminate();
                }
            }
        }

        protected override void HandleTargetOutOfRange(IElementAttackable target) {
            // 5.21.17 Stop firing and start reloading as target is 'out of range'.
            if (_activeOrdnance != null && _activeOrdnance.Target == target) {
                if (target.IsDead || !target.IsAttackAllowedBy(Owner)) {
                    return; // CheckActiveOrdnanceTargeting will handle
                }
                _activeOrdnance.Terminate();    // target is alive and still an enemy but out of range
            }
        }

        public override void HandleFiringInitiated(IElementAttackable targetFiredOn, IOrdnance ordnanceFired) {
            base.HandleFiringInitiated(targetFiredOn, ordnanceFired);
            // IMPROVE Track target with turret
        }

        protected override void RecordFiredOrdnance(IOrdnance ordnanceFired) {
            D.AssertNull(_activeOrdnance);
            _activeOrdnance = ordnanceFired as ITerminatableOrdnance;
        }

        public override void HandleFiringComplete(IOrdnance ordnanceFired) {
            base.HandleFiringComplete(ordnanceFired);
            // IMPROVE Stop tracking target with turret?? 3.4.17 Turrets don't continuously track targets
        }

        protected override void RemoveFiredOrdnanceFromRecord(IOrdnance terminatedOrdnance) {
            D.AssertEqual(_activeOrdnance, terminatedOrdnance);
            _activeOrdnance = null;
        }

        #region Event and Property Change Handlers

        protected override void IsOperationalPropChangedHandler() {
            base.IsOperationalPropChangedHandler();
            if (!IsOperational) {
                CheckActiveOrdnanceTargeting();
            }
        }

        #endregion

        public override bool AreSpecsEqual(AEquipmentStat otherStat) {
            return Stat == otherStat as BeamWeaponStat;
        }

    }
}

