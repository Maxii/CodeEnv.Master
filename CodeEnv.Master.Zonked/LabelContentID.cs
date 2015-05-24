// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LabelContentID.cs
// Identifier for each element of individual content present in a label.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Identifier for each element of individual content present in a label.
    /// </summary>
    [System.Obsolete]
    public enum LabelContentID {

        None,

        Name,
        ParentName,
        Owner,

        IntelState,

        Category,

        Capacity,
        Resources,

        MaxHitPoints,
        CurrentHitPoints,
        Health,
        Defense,
        Mass,

        Science,
        Culture,
        NetIncome,

        Approval,

        SectorIndex,

        MaxWeaponsRange,
        MaxSensorRange,
        Offense,

        Target,
        CombatStance,
        CurrentSpeed,
        FullSpeed,
        MaxTurnRate,

        Composition,
        Formation,
        CurrentCmdEffectiveness,

        UnitMaxWeaponsRange,
        UnitMaxSensorRange,
        UnitOffense,
        UnitDefense,
        UnitMaxHitPts,
        UnitCurrentHitPts,
        UnitHealth,

        Population,
        UnitScience,
        UnitCulture,
        UnitNetIncome,

        UnitFullSpeed,
        UnitMaxTurnRate,
        Density,

        CameraDistance,
        TargetDistance,
        OrbitalSpeed

    }
}

