// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitAttackableTarget.cs
// Interface for targets that can be attacked by unit commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for targets that can be attacked by unit commands.
    /// </summary>
    public interface IUnitAttackableTarget : INavigableTarget {

        event Action<IMortalItem> onDeathOneShot;

        bool IsAlive { get; }

    }
}

