// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RareResourceID.cs
// Unique identifier for a rare resource.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Unique identifier for a rare resource.
    /// </summary>
    [System.Obsolete]
    public enum RareResourceID {

        None,

        [EnumAttribute("T")]
        Titanium,

        [EnumAttribute("D")]
        Duranium,

        [EnumAttribute("U")]
        Unobtanium

    }
}

