﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipHull.cs
// Size of ship hulls in ascending mass order.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Size of ship hulls in ascending mass order.
    /// </summary>
    public enum ShipHull {

        None,
        [EnumAttribute("F")]
        Fighter,

        [EnumAttribute("FF")]
        Frigate,

        [EnumAttribute("DD")]
        Destroyer,

        [EnumAttribute("CA")]
        Cruiser,

        [EnumAttribute("DN")]
        Dreadnaught,

        [EnumAttribute("CV")]
        Carrier

    }
}

