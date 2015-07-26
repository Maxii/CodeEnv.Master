// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ProjectileLauncher.cs
//  An Element's offensive unguided Projectile-firing weapon.
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
    ///  An Element's offensive unguided Projectile-firing weapon.
    /// </summary>
    public class ProjectileLauncher : AWeapon {

        public override ArmamentCategory ArmamentCategory { get { return ArmamentCategory.Projectile; } }

        private IList<IOrdnance> _activeFiredOrdnance;

        public ProjectileLauncher(WeaponStat stat)
            : base(stat) {
            _activeFiredOrdnance = new List<IOrdnance>();
        }

        protected override void RecordFiredOrdnance(IOrdnance ordnanceFired) {
            _activeFiredOrdnance.Add(ordnanceFired);
        }

        protected override void RemoveFiredOrdnanceFromRecord(IOrdnance ordnance) {
            var isRemoved = _activeFiredOrdnance.Remove(ordnance);
            D.Assert(isRemoved);
        }

        public override void CheckActiveOrdnanceTargeting() { } // Projectile ordnance cannot be remotely terminated

        protected override void OnToShowEffectsChanged() {
            if (_activeFiredOrdnance.Any()) {
                _activeFiredOrdnance.ForAll(ord => ord.ToShowEffects = ToShowEffects);
            }
        }

    }
}

