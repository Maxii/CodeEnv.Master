// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ALOSWeapon.cs
// Abstract base class for a weapon that requires a straight line of sight to use.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Abstract base class for a weapon that requires a straight line of sight to use.
    /// </summary>
    public abstract class ALOSWeapon : AWeapon {

        public event Action<LosWeaponFiringSolution> onWeaponAimedAtTarget;

        public new ILOSWeaponMount WeaponMount {
            get { return base.WeaponMount as ILOSWeaponMount; }
            set { base.WeaponMount = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ALOSWeapon"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public ALOSWeapon(WeaponStat stat, string name = null) : base(stat, name) { }

        public void OnTraverseCompleted(LosWeaponFiringSolution firingSolution) {
            if (onWeaponAimedAtTarget != null) {
                onWeaponAimedAtTarget(firingSolution);
            }
        }

        // TODO what happens when the traverse fails aka job is killed? onTraverseFailed? why would it ever fail?

        /// <summary>
        /// Aims this LOS Weapon at the target defined by the provided firing solution.
        /// </summary>
        /// <param name="firingSolution">The firing solution.</param>
        public void AimAtTarget(LosWeaponFiringSolution firingSolution) {
            WeaponMount.TraverseTo(firingSolution);
        }

        #region Archive

        //public override bool TryPickBestTarget(IElementAttackableTarget hint, out IElementAttackableTarget enemyTgt) {
        //    if (hint != null && _qualifiedEnemyTargets.Contains(hint)) {
        //        if (WeaponMount.CheckFiringSolution(hint)) {
        //            IElementAttackableTarget interferingEnemyTgt;
        //            if (WeaponMount.CheckLineOfSight(hint, out interferingEnemyTgt)) {
        //                enemyTgt = hint;
        //                return true;
        //            }
        //            if (interferingEnemyTgt != null) {
        //                enemyTgt = interferingEnemyTgt;
        //                return true;
        //            }
        //        }
        //    }
        //    var possibleTargets = new List<IElementAttackableTarget>(_qualifiedEnemyTargets);
        //    return TryPickBestTarget(possibleTargets, out enemyTgt);
        //}

        //protected override bool TryPickBestTarget(IList<IElementAttackableTarget> possibleTargets, out IElementAttackableTarget enemyTgt) {
        //    enemyTgt = null;
        //    if (possibleTargets.Count == Constants.Zero) {
        //        return false;
        //    }
        //    var candidateTgt = possibleTargets.First();
        //    if (WeaponMount.CheckFiringSolution(candidateTgt)) {
        //        IElementAttackableTarget interferingEnemyTgt;
        //        if (WeaponMount.CheckLineOfSight(candidateTgt, out interferingEnemyTgt)) {
        //            enemyTgt = candidateTgt;
        //            return true;
        //        }
        //        if (interferingEnemyTgt != null) {
        //            enemyTgt = interferingEnemyTgt;
        //            return true;
        //        }
        //    }
        //    possibleTargets.Remove(candidateTgt);
        //    return TryPickBestTarget(possibleTargets, out enemyTgt);
        //}

        #endregion

    }
}

