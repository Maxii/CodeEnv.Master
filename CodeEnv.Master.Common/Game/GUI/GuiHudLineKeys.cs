// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiHudLineKeys.cs
// Enum for keys associated with the display of a line of information shown on the GuiCursorHUD.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Enum for keys associated with the display of a line of information shown on the GuiCursorHUD.
    /// </summary>
    public enum GuiHudLineKeys {

        None,
        Name,
        ParentName,
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
        ShipSize,
        ShipDetails
    }
}

