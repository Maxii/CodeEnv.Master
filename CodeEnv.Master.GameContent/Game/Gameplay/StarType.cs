// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarType.cs
// Type of Star.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

using CodeEnv.Master.Common;
namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Type of Star.
    /// </summary>
    public enum StarType {

        None,

        [EnumAttribute("G")]
        Star_001,

        [EnumAttribute("Red Dwarf")]
        Star_002

    }
}

