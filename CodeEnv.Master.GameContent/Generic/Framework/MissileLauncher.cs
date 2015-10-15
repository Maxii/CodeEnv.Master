// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MissileLauncher.cs
// Weapon that launches a Missile.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Weapon that launches a Missile.
    /// </summary>
    public class MissileLauncher : AWeapon {

        /// <summary>
        /// The maximum speed of this launcher's missile in units per hour in Topography.OpenSpace.
        /// </summary>
        public float OrdnanceMaxSpeed { get { return Stat.OrdnanceMaxSpeed; } }

        public float OrdnanceMass { get { return Stat.OrdnanceMass; } }

        /// <summary>
        /// The drag of this launcher's missile in Topography.OpenSpace.
        /// </summary>
        public float OrdnanceDrag { get { return Stat.OrdnanceDrag; } }

        protected new ProjectileWeaponStat Stat { get { return base.Stat as ProjectileWeaponStat; } }

        private IList<ITerminatableOrdnance> _activeFiredOrdnance;

        /// <summary>
        /// Initializes a new instance of the <see cref="MissileLauncher"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public MissileLauncher(ProjectileWeaponStat stat, string name = null)
            : base(stat, name) {
            _activeFiredOrdnance = new List<ITerminatableOrdnance>();
        }

        /// <summary>
        /// Called when an ownership change of either the ParentElement or a tracked target requires
        /// a check to see if any active ordnance is currently targeted on a non-enemy.
        /// </summary>
        public override void CheckActiveOrdnanceTargeting() {
            var ordnanceTargetingNonEnemies = _activeFiredOrdnance.Where(ord => !ord.Target.Owner.IsEnemyOf(Owner));
            if (ordnanceTargetingNonEnemies.Any()) {
                ordnanceTargetingNonEnemies.ForAll(ord => ord.Terminate());
            }
        }

        protected override void RecordFiredOrdnance(IOrdnance ordnanceFired) {
            _activeFiredOrdnance.Add(ordnanceFired as ITerminatableOrdnance);
        }

        protected override void RemoveFiredOrdnanceFromRecord(IOrdnance ordnance) {
            var isRemoved = _activeFiredOrdnance.Remove(ordnance as ITerminatableOrdnance);
            D.Assert(isRemoved);
        }

        protected override void OnToShowEffectsChanged() {
            if (_activeFiredOrdnance.Any()) {
                _activeFiredOrdnance.ForAll(ord => ord.ToShowEffects = ToShowEffects);
            }
        }

    }
}

