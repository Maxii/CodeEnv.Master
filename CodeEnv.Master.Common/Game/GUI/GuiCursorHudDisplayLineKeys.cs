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

        None,
        Name,
        IntelState,
        Distance,
        Capacity,
        Resources,
        Specials,
        SettlementSize,
        SettlementDetails,
        Owner,
        Health,
        CombatStrength,
        CombatStrengthDetails,
        Speed,
        Composition,
        CompositionDetails,
        ShipDetails
    }
}

