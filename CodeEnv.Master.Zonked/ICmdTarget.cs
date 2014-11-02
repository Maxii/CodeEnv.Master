// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICmdTarget.cs
// Interface for a target that is a UnitCommand.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// Interface for a target that is a UnitCommand.
    /// </summary>
    public interface ICmdTarget : ICombatTarget {

        IEnumerable<IElementTarget> UnitElementTargets { get; }

    }
}

