// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IProjectileOrdnance.cs
// Interface for Projectile Ordnance of all types.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for Projectile Ordnance of all types.
    /// </summary>
    public interface IProjectileOrdnance : IOrdnance {

        void Launch(IElementAttackableTarget target, AWeapon weapon, bool toShowEffects);

    }
}

