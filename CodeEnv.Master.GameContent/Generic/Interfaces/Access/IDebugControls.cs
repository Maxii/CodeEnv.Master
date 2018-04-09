// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDebugControls.cs
// Interface for access to the DebugControls MonoBehaviour.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using Common;

    /// <summary>
    /// Interface for access to the DebugControls MonoBehaviour.
    /// </summary>
    public interface IDebugControls {

        event EventHandler validatePlayerKnowledgeNow;

        event EventHandler showFleetCoursePlots;

        event EventHandler showShipCoursePlots;

        event EventHandler showFleetVelocityRays;

        event EventHandler showShipVelocityRays;

        event EventHandler showFleetFormationStations;

        event EventHandler showShipCollisionDetectionZones;

        event EventHandler showShields;

        event EventHandler showSensors;

        event EventHandler showObstacleZones;

        event EventHandler showSystemTrackingLabels;

        event EventHandler showUnitTrackingLabels;

        event EventHandler showElementIcons;

        event EventHandler showPlanetIcons;

        event EventHandler showStarIcons;


        bool ShowOrdnanceDebugLogs { get; }

        bool ShowFacilityDebugLogs { get; }

        bool ShowShipDebugLogs { get; }

        bool ShowStarDebugLogs { get; }

        bool ShowPlanetoidDebugLogs { get; }

        bool ShowBaseCmdDebugLogs { get; }

        bool ShowFleetCmdDebugLogs { get; }

        bool ShowSystemDebugLogs { get; }

        bool ShowDeploymentDebugLogs { get; }

        /// <summary>
        /// Indicates whether fleets should automatically explore without countervailing orders.
        /// <remarks>10.17.16 The only current source of countervailing orders are from editor fields
        /// on DebugFleetCreators.</remarks>
        /// </summary>
        bool FleetsAutoExploreAsDefault { get; }

        /// <summary>
        /// Indicates whether fleets should automatically attack all war enemies. All players, when they first
        /// meet will have their relationship set to War.
        /// <remarks>2.14.17 User Relationship Settings and existing orders for DebugUnitCreators will be ignored
        /// as all players will have their relationship set to War when they first meet.</remarks>
        /// </summary>
        bool FleetsAutoAttackAsDefault { get; }

        /// <summary>
        /// The maximum number of concurrently attacking fleets allowed per player.
        /// </summary>
        int MaxAttackingFleetsPerPlayer { get; }

        /// <summary>
        /// If <c>true</c> every player knows everything about every item they detect. 
        /// It DOES NOT MEAN that they have detected everything or that players have met yet.
        /// Players meet when they first detect a HQ Element owned by another player.
        /// </summary>
        bool IsAllIntelCoverageComprehensive { get; }

        bool AreAssaultsAlwaysSuccessful { get; }


        bool DeactivateMRSensors { get; }

        bool DeactivateLRSensors { get; }

        /// <summary>
        /// If <c>true</c> the User will be prompted to manually select the tech to research from the ResearchWindow.
        /// </summary>
        bool UserSelectsTechs { get; }

        bool IsAutoRelationsChangeEnabled { get; }



        bool ShowFleetCoursePlots { get; }

        bool ShowShipCoursePlots { get; }

        bool ShowFleetVelocityRays { get; }

        bool ShowShipVelocityRays { get; }

        bool ShowShipCollisionDetectionZones { get; }

        bool ShowFleetFormationStations { get; }

        bool ShowShields { get; }

        bool ShowSensors { get; }

        bool ShowObstacleZones { get; }

        bool ShowUnitTrackingLabels { get; }

        bool ShowSystemTrackingLabels { get; }

        /// <summary>
        /// If <c>true</c> elements will display 2D icons when the camera is too far away to discern the mesh.
        /// </summary>
        bool ShowElementIcons { get; }

        /// <summary>
        /// If <c>true</c> planets will display 2D icons when the camera is too far away to discern the mesh.
        /// </summary>
        bool ShowPlanetIcons { get; }

        /// <summary>
        /// If <c>true</c> stars will display 2D icons when the camera is too far away to discern the mesh.
        /// </summary>
        bool ShowStarIcons { get; }




    }
}

