// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOwnedTarget.cs
// Interface for a target that can have an owner.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for a target that can have an owner.
    /// </summary>
    [Obsolete]
    public interface IOwnedTarget : ITarget {

        event Action<IOwnedTarget> onOwnerChanged;

        IPlayer Owner { get; }

    }
}

