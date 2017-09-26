// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ItemInfoID.cs
// Unique identifier for each piece of info held by Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Unique identifier for each piece of info held by Items. Used for
    /// Player Access Control and the ID of info that should be displayed.
    /// </summary>
    public enum ItemInfoID {

        None,

        // Commonly available at IntelCoverage.Basic
        Name,

        UnitName,
        Position,
        SectorID,

        // Commonly available at IntelCoverage.Essential
        Owner,
        Category,
        Mass,

        Capacity,   // TODO Not used and not clear what it means
        Resources,

        Health,
        MaxHitPoints,
        CurrentHitPoints,

        Offense,
        Defense,
        CombatStance,
        AlertStatus,

        Science,
        Culture,
        NetIncome,
        Production,
        ConstructionCost,

        WeaponsRange,
        SensorRange,

        Target,
        TargetDistance,

        CurrentHeading,
        CurrentSpeedSetting,
        FullSpeed,
        MaxTurnRate,

        // Cmd only values
        Composition,
        Formation,
        CurrentCmdEffectiveness,
        Hero,

        // Unit values
        UnitWeaponsRange,
        UnitSensorRange,
        UnitOffense,
        UnitDefense,
        UnitMaxHitPts,
        UnitCurrentHitPts,
        UnitHealth,
        UnitScience,
        UnitCulture,
        UnitNetIncome,
        UnitFullSpeed,
        UnitMaxTurnRate,
        UnitProduction,

        // Unique
        Population,
        Approval,
        OrbitalSpeed,

        // Always available if relevant
        IntelState,
        ActualSpeed,
        CameraDistance,

        // Formatting
        Separator,
    }
}

