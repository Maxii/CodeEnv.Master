// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MissileLauncher.cs
//  An Element's offensive guided Missile-firing weapon.
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
    /// An Element's offensive guided Missile-firing weapon.
    /// </summary>
    public class MissileLauncher : AWeapon {

        private IList<ITerminatableOrdnance> _activeFiredOrdnance;

        public MissileLauncher(WeaponStat stat)
            : base(stat) {
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

