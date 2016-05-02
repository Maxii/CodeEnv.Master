// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponFiringSolution.cs
// A firing solution for a Weapon against an IElementAttackable target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// A firing solution for a Weapon against an IElementAttackable target.
    /// </summary>
    public class WeaponFiringSolution {

        private static string _toStringFormat = "{0}: WeaponName = {1}, TargetName = {2}.";

        public IElementAttackable EnemyTarget { get; private set; }

        public AWeapon Weapon { get; private set; }

        public WeaponFiringSolution(AWeapon weapon, IElementAttackable enemyTgt) {
            Weapon = weapon;
            EnemyTarget = enemyTgt;
        }

        public sealed override string ToString() {
            return _toStringFormat.Inject(GetType().Name, Weapon.FullName, EnemyTarget.FullName);
        }

    }
}

