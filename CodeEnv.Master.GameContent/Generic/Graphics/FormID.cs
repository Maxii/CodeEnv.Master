// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FormID.cs
// Unique identifier for Forms.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Unique identifier for Forms. A Form supervises a collection of UIWidgets
    ///in an arrangement that can be displayed by a GuiWindow.  
    /// </summary>
    public enum FormID {

        None,

        TextHud,

        ResourceHud,

        SettlementTableRow,

        StarbaseTableRow,

        FleetTableRow,

        SystemTableRow,

        SelectedSettlement,

        SelectedStarbase,

        SelectedFleet,

        SelectedSystem,

        SelectedShip,

        SelectedFacility,

        SelectedStar,

        SelectedPlanetoid,

        SelectedUniverseCenter

    }
}

