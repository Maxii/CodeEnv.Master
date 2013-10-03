// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SpecialKeys.cs
// Enum that translates KeyCodes into 'special' named modes.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Enum that translates KeyCodes into 'special' named modes.
    /// </summary>
    public enum SpecialKeys {

        //[EnumAttribute("")]
        None = KeyCode.None,

        SectorViewMode = KeyCode.F1,

        NormalViewMode = KeyCode.Escape

    }
}

