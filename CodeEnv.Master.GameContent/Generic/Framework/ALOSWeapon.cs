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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Abstract base class for a weapon that requires a straight line of sight to use.
    /// </summary>
    public abstract class ALOSWeapon : AWeapon {

        public event EventHandler<LosWeaponFiringSolutionEventArgs> weaponAimed;

        public new ILOSWeaponMount WeaponMount {
            get { return base.WeaponMount as ILOSWeaponMount; }
            set { base.WeaponMount = value; }
        }

        /// <summary>
        /// The maximum inaccuracy of this Weapon's bearing when launched in degrees.
        /// </summary>
        public abstract float MaxLaunchInaccuracy { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ALOSWeapon"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public ALOSWeapon(AWeaponStat stat, string name = null) : base(stat, name) { }

        public void HandleWeaponAimed(LosWeaponFiringSolution firingSolution) {
            OnWeaponAimed(firingSolution);
        }

        #region Event and Property Change Handlers

        private void OnWeaponAimed(LosWeaponFiringSolution firingSolution) {
            if (weaponAimed != null) {
                weaponAimed(this, new LosWeaponFiringSolutionEventArgs(firingSolution));
            }
        }

        #endregion

        /// <summary>
        /// Aims this LOS Weapon using the provided firing solution.
        /// </summary>
        /// <param name="firingSolution">The firing solution.</param>
        public void AimAt(LosWeaponFiringSolution firingSolution) {
            WeaponMount.TraverseTo(firingSolution);
        }

        #region Debug

        public bool __CheckLineOfSight(IElementAttackable enemyTgt) {
            return WeaponMount.__CheckLineOfSight(enemyTgt);
        }

        #endregion

        #region Nested Classes

        public class LosWeaponFiringSolutionEventArgs : EventArgs {

            public LosWeaponFiringSolution FiringSolution { get; private set; }

            public LosWeaponFiringSolutionEventArgs(LosWeaponFiringSolution firingSolution) {
                FiringSolution = firingSolution;
            }

        }

        #endregion

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

