// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AccessControlInfoID.cs
// Unique identifier for each piece of info that is under access control.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Unique identifier for each piece of info that is under access control.
    /// </summary>
    public enum AccessControlInfoID {

        None,

        // Commonly available at IntelCoverage.Basic
        Name,
        ParentName,
        Position,
        SectorIndex,

        // Commonly available at IntelCoverage.Essential
        Owner,
        Category,
        Mass,

        Capacity,
        Resources,

        Health,
        MaxHitPoints,
        CurrentHitPoints,

        Offense,
        Defense,
        CombatStance,

        Science,
        Culture,
        NetIncome,

        WeaponsRange,
        SensorRange,

        Target,
        TargetDistance,

        CurrentSpeed,
        FullSpeed,
        MaxTurnRate,

        // Cmd only values
        Composition,
        Formation,
        CurrentCmdEffectiveness,

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

        // Unique
        Population,
        Approval,
        OrbitalSpeed,

        // Debug Special
        IntelState,
        CameraDistance,

    }
}

