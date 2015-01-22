// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IntelCoverageExtensions.cs
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
    public static class IntelCoverageExtensions {


        private static IList<ItemDataPropertyID> _awareCoverageIDs = new List<ItemDataPropertyID>() {
            ItemDataPropertyID.Position,
            ItemDataPropertyID.Topography,
            ItemDataPropertyID.SectorIndex
        };

        private static IList<ItemDataPropertyID> _minimalCoverageIDs = new List<ItemDataPropertyID>() {
        ItemDataPropertyID.Position,
        ItemDataPropertyID.Topography,
        ItemDataPropertyID.SectorIndex,

        // SectorData
        ItemDataPropertyID.Name,
        ItemDataPropertyID.Density,

        // StarData
        ItemDataPropertyID.Category,
        ItemDataPropertyID.Capacity,
        ItemDataPropertyID.Resources,
        ItemDataPropertyID.SpecialResources,

        // SystemData
        ItemDataPropertyID.SettlementOrbitSlot,
        ItemDataPropertyID.SettlementData,
        ItemDataPropertyID.StarData,

        // AMortalItemData
        ItemDataPropertyID.Countermeasures,
        ItemDataPropertyID.MaxHitPoints,
        ItemDataPropertyID.CurrentHitPoints, 
        ItemDataPropertyID.Health,
        ItemDataPropertyID.DefensiveStrength,
        ItemDataPropertyID.Mass,

        // APlanetoidData
        // Category

        // PlanetData
        ItemDataPropertyID.OrbitalSpeed,

        // MoonData

        // AElementData
        ItemDataPropertyID.Weapons,
        ItemDataPropertyID.Sensors,
        ItemDataPropertyID.OffensiveStrength,
        ItemDataPropertyID.MaxWeaponsRange,
        ItemDataPropertyID.MaxSensorsRange,

        // ShipData
        ItemDataPropertyID.IsFtlOperational,
        ItemDataPropertyID.IsFtlDampedByField,
        ItemDataPropertyID.IsFtlAvailableForUse,
        ItemDataPropertyID.IsFlapsDeployed,
        ItemDataPropertyID.Target,
        // Category,
        ItemDataPropertyID.CurrentSpeed,
        ItemDataPropertyID.RequestedSpeed,
        ItemDataPropertyID.CombatStance,
        ItemDataPropertyID.Drag,
        ItemDataPropertyID.FullThrust,
        ItemDataPropertyID.FullStlThrust,
        ItemDataPropertyID.FullFtlThrust,
        ItemDataPropertyID.CurrentHeading,
        ItemDataPropertyID.RequestedHeading,
        ItemDataPropertyID.FullSpeed,
        ItemDataPropertyID.FullStlSpeed,
        ItemDataPropertyID.FullFtlSpeed,
        ItemDataPropertyID.MaxTurnRate,

        // FacilityData
        // Category,


        // ACommandData
        ItemDataPropertyID.UnitFormation,
        ItemDataPropertyID.HQElementData,
        ItemDataPropertyID.CurrentCmdEffectiveness,
        ItemDataPropertyID.MaxCmdEffectiveness,
        ItemDataPropertyID.UnitMaxWeaponsRange,
        ItemDataPropertyID.UnitMaxSensorsRange,
        ItemDataPropertyID.UnitOffensiveStrength,
        ItemDataPropertyID.UnitDefensiveStrength,
        ItemDataPropertyID.UnitMaxHitPoints,
        ItemDataPropertyID.UnitCurrentHitPoints,
        ItemDataPropertyID.UnitHealth,

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
        ItemDataPropertyID.Composition,

        // SettlementCmdData
        // Category,
        ItemDataPropertyID.Population,
        ItemDataPropertyID.CapacityUsed,   //?
        ItemDataPropertyID.ResourcesUsed,  //?
        ItemDataPropertyID.SpecialResourcesUsed //?
        // HQElementData
        // Composition,

        // StarbaseCmdData
        // Category,
        // HQElementData,
        // Composition,

        };

        private static IList<ItemDataPropertyID> _moderateCoverageIDs = new List<ItemDataPropertyID>() {
                // AItemData
        ItemDataPropertyID.Name,
        ItemDataPropertyID.ParentName,
        ItemDataPropertyID.Owner,
        ItemDataPropertyID.Topography,
        ItemDataPropertyID.Position,

        // SectorData
        ItemDataPropertyID.SectorIndex,
        ItemDataPropertyID.Density,

        // StarData
        ItemDataPropertyID.Category,
        ItemDataPropertyID.Capacity,
        ItemDataPropertyID.Resources,
        ItemDataPropertyID.SpecialResources,

        // SystemData
        ItemDataPropertyID.SettlementOrbitSlot,
        ItemDataPropertyID.SettlementData,
        ItemDataPropertyID.StarData,

        // AMortalItemData
        ItemDataPropertyID.Countermeasures,
        ItemDataPropertyID.MaxHitPoints,
        ItemDataPropertyID.CurrentHitPoints, 
        ItemDataPropertyID.Health,
        ItemDataPropertyID.DefensiveStrength,
        ItemDataPropertyID.Mass,

        // APlanetoidData
        // Category

        // PlanetData
        ItemDataPropertyID.OrbitalSpeed,

        // MoonData

        // AElementData
        ItemDataPropertyID.Weapons,
        ItemDataPropertyID.Sensors,
        ItemDataPropertyID.OffensiveStrength,
        ItemDataPropertyID.MaxWeaponsRange,
        ItemDataPropertyID.MaxSensorsRange,

        // ShipData
        ItemDataPropertyID.IsFtlOperational,
        ItemDataPropertyID.IsFtlDampedByField,
        ItemDataPropertyID.IsFtlAvailableForUse,
        ItemDataPropertyID.IsFlapsDeployed,
        ItemDataPropertyID.Target,
        // Category,
        ItemDataPropertyID.CurrentSpeed,
        ItemDataPropertyID.RequestedSpeed,
        ItemDataPropertyID.CombatStance,
        ItemDataPropertyID.Drag,
        ItemDataPropertyID.FullThrust,
        ItemDataPropertyID.FullStlThrust,
        ItemDataPropertyID.FullFtlThrust,
        ItemDataPropertyID.CurrentHeading,
        ItemDataPropertyID.RequestedHeading,
        ItemDataPropertyID.FullSpeed,
        ItemDataPropertyID.FullStlSpeed,
        ItemDataPropertyID.FullFtlSpeed,
        ItemDataPropertyID.MaxTurnRate,

        // FacilityData
        // Category,


        // ACommandData
        ItemDataPropertyID.UnitFormation,
        ItemDataPropertyID.HQElementData,
        ItemDataPropertyID.CurrentCmdEffectiveness,
        ItemDataPropertyID.MaxCmdEffectiveness,
        ItemDataPropertyID.UnitMaxWeaponsRange,
        ItemDataPropertyID.UnitMaxSensorsRange,
        ItemDataPropertyID.UnitOffensiveStrength,
        ItemDataPropertyID.UnitDefensiveStrength,
        ItemDataPropertyID.UnitMaxHitPoints,
        ItemDataPropertyID.UnitCurrentHitPoints,
        ItemDataPropertyID.UnitHealth,

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
        ItemDataPropertyID.Composition,

        // SettlementCmdData
        // Category,
        ItemDataPropertyID.Population,
        ItemDataPropertyID.CapacityUsed,   //?
        ItemDataPropertyID.ResourcesUsed,  //?
        ItemDataPropertyID.SpecialResourcesUsed //?
        // HQElementData
        // Composition,

        // StarbaseCmdData
        // Category,
        // HQElementData,
        // Composition,

        };

        public static bool AccessGranted(this IntelCoverage intelCoverage, ItemDataPropertyID propertyID) {
            switch (intelCoverage) {
                case IntelCoverage.None:
                    return false;
                case IntelCoverage.Aware:
                    return _awareCoverageIDs.Contains(propertyID);
                case IntelCoverage.Minimal:
                    return _minimalCoverageIDs.Contains(propertyID);
                case IntelCoverage.Moderate:
                    return _moderateCoverageIDs.Contains(propertyID);
                case IntelCoverage.Comprehensive:
                    return true;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(intelCoverage));
            }
        }

    }
}

