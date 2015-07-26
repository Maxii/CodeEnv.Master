// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOrdnance.cs
// Interface for Ordnance of all types.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for Ordnance of all types.
    /// </summary>
    public interface IOrdnance {

        event Action<IOrdnance> onDeathOneShot;

        bool ToShowEffects { get; set; }

        string Name { get; }

        ArmamentCategory ArmamentCategory { get; }

        IElementAttackableTarget Target { get; }

        void Initiate(IElementAttackableTarget target, AWeapon weapon, bool toShowEffects);

    }
}

