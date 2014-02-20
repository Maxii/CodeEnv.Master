// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICmdTarget.cs
// Interface for a UnitCommandItem that is an attack target of another Item.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// Interface for a UnitCommandItem that is an attack target of another Item.
    /// </summary>
    public interface ICmdTarget : ITarget {

        IEnumerable<ITarget> ElementTargets { get; }

    }
}

