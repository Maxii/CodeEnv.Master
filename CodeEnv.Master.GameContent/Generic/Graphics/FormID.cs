// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FormID.cs
// Unique identifier for a Form.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Unique identifier for a Form. A Form supervises a collection of UIWidgets
    /// in an arrangement that can be displayed by a AGuiWindow. AForms are
    /// populated with content to display by feeding them Text, Reports or individual
    /// values (e.g. a ResourceForm is fed a ResourceID, displaying values derived from
    /// the ResourceID in a TooltipHudWindow).
    /// <remarks>6.17.17 Many AForms are shown in HUDs and the FormID is used by the HUD 
    /// to pick the form to show. Some AForms don't lend themselves to HUD displays due to 
    /// the way the form is structured. TableRowForms and MiniDesignForms are good examples
    /// as Rows can be quite long, and MiniDesignForms contain minimal info. In these cases
    /// the FormID is currently not used.</remarks>
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

        SelectedUniverseCenter,


        ShipDesign,

        SelectedShipDesign,

        SelectedEquipment,   // TEMP Placeholder for a number of EquipmentType-specific forms


    }
}

