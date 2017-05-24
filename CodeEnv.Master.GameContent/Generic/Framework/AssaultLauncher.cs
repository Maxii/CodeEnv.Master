// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AssaultLauncher.cs
// Weapon that launches an AssaultShuttle.
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
    using UnityEngine;

    /// <summary>
    /// Weapon that launches an AssaultShuttle.
    /// </summary>
    public class AssaultLauncher : AWeapon {

        /// <summary>
        /// The turn rate of the ordnance in degrees per hour .
        /// </summary>
        public float TurnRate { get { return Stat.TurnRate; } }

        /// <summary>
        /// How often the ordnance's course is updated in updates per hour.
        /// </summary>
        public float CourseUpdateFrequency { get { return Stat.CourseUpdateFrequency; } }

        /// <summary>
        /// The maximum speed of this launcher's missile in units per hour in Topography.OpenSpace.
        /// </summary>
        public float MaxSpeed { get { return Stat.MaxSpeed; } }

        public float OrdnanceMass { get { return Stat.OrdnanceMass; } }
        /// <summary>
        /// The drag of this launcher's missile in Topography.OpenSpace.
        /// </summary>
        public float OrdnanceDrag { get { return Stat.OrdnanceDrag; } }

        /// <summary>
        /// The maximum steering inaccuracy of this weapon's missile ordnance in degrees.
        /// </summary>
        public float MaxSteeringInaccuracy { get { return Stat.MaxSteeringInaccuracy; } }

        protected new AssaultWeaponStat Stat { get { return base.Stat as AssaultWeaponStat; } }

        private HashSet<ITerminatableOrdnance> _activeFiredOrdnance;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssaultLauncherSim"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public AssaultLauncher(AssaultWeaponStat stat, string name = null)
            : base(stat, name) {
            _activeFiredOrdnance = new HashSet<ITerminatableOrdnance>();
        }

        protected override bool IsQualifiedEnemyTarget(IElementAttackable enemyTarget) {
            return (enemyTarget as IAssaultable).IsAssaultAllowedBy(Owner);
        }

        /// <summary>
        /// Checks to see if any active ordnance is currently targeted on a target that is no longer assaultable.
        /// <remarks>This method covers most circumstances but not all. AProjectile ordnance has a range. That
        /// range is how far it is allowed to travel, not how far away from the launching weapon it is. Accordingly,
        /// if the target or launcher move to a point where the target is no longer tracked by the WRM, it will be
        /// removed from this launcher's tracked targets. Now that the launcher is unsubscribed from target changes,
        /// a change in the target's enemy or assaultable status will NOT result in this targeting status check. Yet
        /// the ordnance could still reach and attack the target. A change in the target's status can come from 
        /// a number of sources - a relations change, assault by another shuttle, etc. Accordingly, the Missile or
        /// AssaultShuttle ordnance must test that it is still allowed to attack before doing so. 
        /// </remarks>
        /// <remarks>5.21.17 This hasn't shown up before now because a missile's attack using TakeHit() doesn't
        /// do a check that says the missile has a right to attack. An AssaultShuttle's attack using AttemptAssault does.</remarks>
        /// </summary>
        public override void CheckActiveOrdnanceTargeting() {
            //D.Log(ShowDebugLog, "{0}.CheckActiveOrdnanceTargeting called in Frame {1}.", DebugName, Time.frameCount);
            var ordnanceTargetingNonAssaultableTgts = _activeFiredOrdnance.Where(ord => !(ord.Target as IAssaultable).IsAssaultAllowedBy(Owner));
            if (ordnanceTargetingNonAssaultableTgts.Any()) {
                //D.Log(ShowDebugLog, "{0}.CheckActiveOrdnanceTargeting is about to terminate AssaultShuttles in Frame {1}. Shuttles: {2}.",
                //    DebugName, Time.frameCount, ordnanceTargetingNonAssaultableTgts.Select(o => o.DebugName).Concatenate());
                ordnanceTargetingNonAssaultableTgts.ForAll(ord => ord.Terminate());
            }
        }

        protected override void HandleTargetOutOfRange(IElementAttackable target) {
            // 5.21.17 nothing to do as these are 'fire and forget' ordnance and will 
            // pursue their target until they run out of fuel, aka their distance traveled range.
        }

        protected override void RecordFiredOrdnance(IOrdnance ordnanceFired) {
            _activeFiredOrdnance.Add(ordnanceFired as ITerminatableOrdnance);
        }

        protected override void RemoveFiredOrdnanceFromRecord(IOrdnance ordnance) {
            var isRemoved = _activeFiredOrdnance.Remove(ordnance as ITerminatableOrdnance);
            D.Assert(isRemoved);
        }

        public override void HandleFiringComplete(IOrdnance ordnanceFired) {
            base.HandleFiringComplete(ordnanceFired);
            D.Assert(IsOperational);
        }

    }
}

