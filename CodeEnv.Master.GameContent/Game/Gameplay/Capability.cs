// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Capability.cs
// Enum designating game capabilities that are activated when acquired, typically via Research.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Enum designating game capabilities that are activated when acquired, typically via Research.
    /// </summary>
    public enum Capability {

        None,

        DetectTitanium,
        DetectDuranium,
        DetectUnobtainium


    }
}

