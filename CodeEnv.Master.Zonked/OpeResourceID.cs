// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OpeResourceID.cs
// Unique identifier for a common resource.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Unique identifier for a common resource.
    /// </summary>
    [System.Obsolete]
    public enum OpeResourceID {

        None = 0,

        [EnumAttribute("O")]
        Organics = 1,

        [EnumAttribute("P")]
        Particulates = 2,

        [EnumAttribute("E")]
        Energy = 3
    }
}

