// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BeamWeapon.cs
// An Element's offensive BeamWeapon.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// An Element's offensive BeamWeapon.
    /// </summary>
    public class BeamWeapon : Weapon {

        public float Duration { get { return _stat.Duration; } }

        private BeamWeaponStat _stat;

        public BeamWeapon(BeamWeaponStat stat)
            : base(stat) {
            _stat = stat;
        }

        public override string ToString() {
            return _stat.ToString();
        }

    }
}

