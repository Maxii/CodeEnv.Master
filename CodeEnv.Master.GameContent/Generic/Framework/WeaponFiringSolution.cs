// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: WeaponFiringSolution.cs
// A firing solution for a Weapon against an IElementAttackableTarget target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// A firing solution for a Weapon against an IElementAttackableTarget target.
    /// </summary>
    public class WeaponFiringSolution {

        public IElementAttackableTarget EnemyTarget { get; private set; }

        public AWeapon Weapon { get; private set; }

        public WeaponFiringSolution(AWeapon weapon, IElementAttackableTarget enemyTgt) {
            Weapon = weapon;
            EnemyTarget = enemyTgt;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

