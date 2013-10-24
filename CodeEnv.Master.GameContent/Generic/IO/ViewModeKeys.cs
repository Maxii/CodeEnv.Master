// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ViewModeKeys.cs
// Enum that translates KeyCodes into named PlayerViewMode keys.
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
    public enum ViewModeKeys {

        //[EnumAttribute("")]
        None = KeyCode.None,

        SectorView = KeyCode.F1,

        //SectorOrder = KeyCode.F2,

        NormalView = KeyCode.Escape

    }
}

