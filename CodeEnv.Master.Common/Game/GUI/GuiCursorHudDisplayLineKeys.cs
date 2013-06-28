// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiCursorHudDisplayLineKeys.cs
// Enum for keys associated with the display of a line of information shown on the GuiCursorHUD.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Enum for keys associated with the display of a line of information shown on the GuiCursorHUD.
    /// </summary>
    public enum GuiCursorHudDisplayLineKeys {

        None = 0,
        Name = 1,
        Distance = 2,
        Capacity = 3,
        Resources = 4,
        Specials = 5,
        IntelState = 6,
        Owner = 7,
        Health = 8,
        CombatStrength = 9,
        Speed = 10,
        Composition = 11
    }
}

