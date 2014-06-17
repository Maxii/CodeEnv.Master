// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOwnedTarget.cs
// Interface for a IDestinationTarget that has an owner.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for a IDestinationTarget that has an owner.
    /// </summary>
    public interface IOwnedTarget : IDestinationTarget {

        event Action<IOwnedTarget> onOwnerChanged;

        IPlayer Owner { get; }

    }
}

