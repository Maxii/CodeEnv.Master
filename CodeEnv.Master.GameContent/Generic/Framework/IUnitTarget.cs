// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitTarget.cs
//  Interface for items that can be targeted by unit commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for items that can be targeted by unit commands.
    /// </summary>
    public interface IUnitTarget : IDestinationTarget {

        event Action<IMortalItem> onDeathOneShot;

        bool IsAlive { get; }


    }
}

