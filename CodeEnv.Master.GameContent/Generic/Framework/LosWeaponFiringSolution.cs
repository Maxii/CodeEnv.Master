// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LosWeaponFiringSolution.cs
// A firing solution for a Weapon against an IElementAttackableTarget that requires Line Of Sight.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// A firing solution for a Weapon against an IElementAttackableTarget that requires Line Of Sight.
    /// </summary>
    public class LosWeaponFiringSolution : WeaponFiringSolution {

        public Quaternion TurretRotation { get; private set; }

        public Quaternion TurretElevation { get; private set; }

        public new ALOSWeapon Weapon { get { return base.Weapon as ALOSWeapon; } }

        public LosWeaponFiringSolution(ALOSWeapon weapon, IElementAttackableTarget enemyTgt, Quaternion turretRotation, Quaternion turretElevation)
            : base(weapon, enemyTgt) {
            TurretRotation = turretRotation;
            TurretElevation = turretElevation;
        }

    }
}

