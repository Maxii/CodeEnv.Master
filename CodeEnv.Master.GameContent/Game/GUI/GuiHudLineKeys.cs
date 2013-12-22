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

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum for keys associated with the display of a line of information shown on the GuiCursorHUD.
    /// </summary>
    public enum GuiHudLineKeys {

        None,
        Name,
        ParentName,
        IntelState,

        /// <summary>
        ///  Info key for distance in Units from the camera. 
        /// Intended to become distance from Selected.
        /// </summary>
        Distance,

        /// <summary>
        ///  Info key for facility?/production? capacity...
        /// </summary>
        Capacity,

        /// <summary>
        ///  Info key for density of standard resources.
        /// </summary>
        Resources,

        /// <summary>
        /// Info key for density of special resources, if any.
        /// </summary>
        Specials,

        SettlementDetails,
        Owner,
        Health,
        CombatStrength,
        CombatStrengthDetails,
        Speed,
        Composition,
        CompositionDetails,
        ShipDetails,

        /// <summary>
        ///  Info key for a type - PlanetoidType, StarType,
        ///  SettlementSize, ShipHull.
        /// </summary>
        Type,

        SectorIndex,
        Density
    }
}

