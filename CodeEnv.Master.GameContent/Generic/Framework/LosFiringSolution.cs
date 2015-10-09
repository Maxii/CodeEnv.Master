// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LosFiringSolution.cs
// A firing solution for a Weapon that requires a Line Of Sight to the target.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// A firing solution for a Weapon that requires a Line Of Sight to the target.
    /// </summary>
    public class LosFiringSolution : FiringSolution {

        public Quaternion TurretRotation { get; private set; }

        public Quaternion TurretElevation { get; private set; }

        public new ALOSWeapon Weapon { get { return base.Weapon as ALOSWeapon; } }

        public LosFiringSolution(ALOSWeapon weapon, IElementAttackableTarget enemyTgt, Quaternion turretRotation, Quaternion turretElevation)
            : base(weapon, enemyTgt) {
            TurretRotation = turretRotation;
            TurretElevation = turretElevation;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

