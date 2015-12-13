// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IBeamOrdnance.cs
// Interface for Beam Ordnance of all types.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for Beam Ordnance of all types.
    /// </summary>
    public interface IBeamOrdnance : IOrdnance {

        void Initiate(IElementAttackableTarget target, AWeapon weapon, bool toShowEffects);

    }
}

