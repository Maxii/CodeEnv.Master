// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
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
    [Obsolete]
    public interface IOrdnance {

        //event Action<Weapon, IOrdnance> onWeaponFiringCompleteOneShot;

        event Action<IOrdnance> onDeathOneShot;

        bool ToShowEffects { get; set; }

        string Name { get; }

        WDVCategory ArmamentCategory { get; }

        IElementAttackableTarget Target { get; }

        void Terminate();

    }
}

