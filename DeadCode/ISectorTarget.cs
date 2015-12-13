// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISectorTarget.cs
// Interface for an IDestinationTarget that is a Sector.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for an IDestinationTarget that is a Sector.
    /// </summary>
    [Obsolete]
    public interface ISectorTarget : /*IOwnedTarget*/ INavigableTarget {

    }
}

