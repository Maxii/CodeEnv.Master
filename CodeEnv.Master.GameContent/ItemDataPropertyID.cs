// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ItemDataPropertyID.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public enum ItemDataPropertyID {

        None,

        // AItemData
        Name,
        ParentName,
        Owner,
        Topography,
        Position,

        // SectorData
        SectorIndex,
        Density,

        // StarData
        Category,
        Capacity,
        Resources,
        SpecialResources,

        // SystemData
        SettlementOrbitSlot,
        SettlementData,
        StarData,

        // AMortalItemData
        Countermeasures,
        MaxHitPoints,
        CurrentHitPoints,
        Health,
        DefensiveStrength,
        Mass,

        // APlanetoidData
        // Category

        // PlanetData
        OrbitalSpeed,

        // MoonData

        // AElementData
        Weapons,
        Sensors,
        OffensiveStrength,
        MaxWeaponsRange,
        MaxSensorsRange,

        // ShipData
        IsFtlOperational,
        IsFtlDampedByField,
        IsFtlAvailableForUse,
        IsFlapsDeployed,
        Target,
        // Category,
        CurrentSpeed,
        RequestedSpeed,
        CombatStance,
        Drag,
        FullThrust,
        FullStlThrust,
        FullFtlThrust,
        CurrentHeading,
        RequestedHeading,
        FullSpeed,
        FullStlSpeed,
        FullFtlSpeed,
        MaxTurnRate,

        // FacilityData
        // Category,


        // ACommandData
        UnitFormation,
        HQElementData,
        CurrentCmdEffectiveness,
        MaxCmdEffectiveness,
        UnitMaxWeaponsRange,
        UnitMaxSensorsRange,
        UnitOffensiveStrength,
        UnitDefensiveStrength,
        UnitMaxHitPoints,
        UnitCurrentHitPoints,
        UnitHealth,

        // FleetCmdData
        // Target,
        // CurrentSpeed,
        // RequestedSpeed,
        // CurrentHeading,
        // RequestedHeading,
        //FullSpeed,
        //FullStlSpeed,
        //FullFtlSpeed,
        //MaxTurnRate,
        Composition,

        // SettlementCmdData
        // Category,
        Population,
        CapacityUsed,   //?
        ResourcesUsed,  //?
        SpecialResourcesUsed, //?
        // HQElementData,
        // Composition,

        // StarbaseCmdData
        // Category,
        // HQElementData,
        // Composition,


    }
}

