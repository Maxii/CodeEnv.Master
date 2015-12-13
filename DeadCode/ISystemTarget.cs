﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISystemTarget.cs
// Interface for an IDestinationTarget that is a System.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for an IDestinationTarget that is a System.
    /// </summary>
    [Obsolete]
    public interface ISystemTarget : /*IOwnedTarget */ INavigableTarget {

    }
}

