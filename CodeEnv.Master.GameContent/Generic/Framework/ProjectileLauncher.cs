﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ProjectileLauncher.cs
// Weapon that launches a LOS projectile.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Weapon that launches a LOS projectile.
    /// </summary>
    public class ProjectileLauncher : ALOSWeapon {

        /// <summary>
        /// The maximum speed of this launcher's projectile in units per hour in Topography.OpenSpace.
        /// </summary>
        public float OrdnanceMaxSpeed { get { return Stat.OrdnanceMaxSpeed; } }

        public float OrdnanceMass { get { return Stat.OrdnanceMass; } }

        /// <summary>
        /// The drag of this launcher's projectile in Topography.OpenSpace.
        /// </summary>
        public float OrdnanceDrag { get { return Stat.OrdnanceDrag; } }

        /// <summary>
        /// The maximum inaccuracy of this Weapon's Turret mount when traversing in degrees.
        /// </summary>
        public override float MaxTraverseInaccuracy { get { return Stat.MaxTraverseInaccuracy; } }

        protected new ProjectileWeaponStat Stat { get { return base.Stat as ProjectileWeaponStat; } }

        private IList<IOrdnance> _activeFiredOrdnance;  // OPTIMIZE not currently used

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectileLauncher"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public ProjectileLauncher(ProjectileWeaponStat stat, string name = null)
            : base(stat, name) {
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

        #region Event and Property Change Handlers

        #endregion


    }
}

