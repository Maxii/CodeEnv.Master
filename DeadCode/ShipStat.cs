﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipStat.cs
// Immutable struct containing externally acquirable values for Ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for Ships.
    /// </summary>
    [System.Obsolete]
    public struct ShipStat {

        public string Name { get; private set; }
        public float Mass { get; private set; }
        public float MaxHitPoints { get; private set; }
        public ShipHullCategory Category { get; private set; }
        public ShipCombatStance CombatStance { get; private set; }
        /// <summary>
        /// The ship's maximum turn rate in degrees per hour.
        /// </summary>
        public float MaxTurnRate { get; private set; }
        public float Drag { get; private set; }
        /// <summary>
        /// The maximum force projected by the STL engines. FullStlSpeed = FullStlThrust / (Mass * Drag).
        /// NOTE: This value uses a Game Hour denominator. It is adjusted in
        /// realtime to a Unity seconds value in EngineRoom.ApplyThrust() using GeneralSettings.HoursPerSecond.
        /// </summary>
        public float FullStlThrust { get; private set; }

        /// <summary>
        /// The maximum force projected by the FTL engines. FullFtlSpeed = FullFtlThrust / (Mass * Drag).
        /// NOTE: This value uses a Game Hour denominator. It is adjusted in
        /// realtime to a Unity seconds value in EngineRoom.ApplyThrust() using GeneralSettings.HoursPerSecond.
        /// </summary>
        public float FullFtlThrust { get; private set; }

        public float Science { get; private set; }

        public float Culture { get; private set; }

        public float Income { get; private set; }

        public float Expense { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipStat" /> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="maxHitPts">The maximum hit PTS.</param>
        /// <param name="category">The category.</param>
        /// <param name="stance">The stance.</param>
        /// <param name="maxTurnRate">The ship's maximum turn rate in degrees per hour.</param>
        /// <param name="drag">The drag.</param>
        /// <param name="fullStlThrust">The maximum force projected by the STL engines. FullStlSpeed = FullStlThrust / (Mass * Drag).
        /// NOTE: This value uses a Game Hour denominator. It is adjusted in
        /// realtime to a Unity seconds value in EngineRoom.ApplyThrust() using GeneralSettings.HoursPerSecond.</param>
        /// <param name="fullFtlThrust">The maximum force projected by the FTL engines. FullFtlSpeed = FullFtlThrust / (Mass * Drag).
        /// NOTE: This value uses a Game Hour denominator. It is adjusted in
        /// realtime to a Unity seconds value in EngineRoom.ApplyThrust() using GeneralSettings.HoursPerSecond.</param>
        /// <param name="science">The science generated by this ship.</param>
        /// <param name="culture">The culture generated by this ship.</param>
        /// <param name="income">The income generated by this ship.</param>
        /// <param name="expense">The expense consumed by this ship.</param>
        public ShipStat(string name, float mass, float maxHitPts, ShipHullCategory category, ShipCombatStance stance,
            float maxTurnRate, float drag, float fullStlThrust, float fullFtlThrust, float science, float culture,
            float income, float expense)
            : this() {
            Name = name;
            Mass = mass;
            MaxHitPoints = maxHitPts;
            Category = category;
            CombatStance = stance;
            MaxTurnRate = maxTurnRate;
            Drag = drag;
            FullStlThrust = fullStlThrust;
            FullFtlThrust = fullFtlThrust;
            Science = science;
            Culture = culture;
            Income = income;
            Expense = expense;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

