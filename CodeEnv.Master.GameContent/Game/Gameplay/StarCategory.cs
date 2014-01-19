// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarCategory.cs
// Category of Star.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

using CodeEnv.Master.Common;
namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Category of Star.
    /// </summary>
    public enum StarCategory {

        None,

        [EnumAttribute("G")]
        Star_001,

        [EnumAttribute("Red Dwarf")]
        Star_002

    }
}

