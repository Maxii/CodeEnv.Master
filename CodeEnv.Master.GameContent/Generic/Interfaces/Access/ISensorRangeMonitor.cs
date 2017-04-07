// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISensorRangeMonitor.cs
// Interface allowing access to SensorRangeMonitor MonoBehaviours.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface allowing access to SensorRangeMonitor MonoBehaviours.
    /// </summary>
    public interface ISensorRangeMonitor : IRangedEquipmentMonitor {

        /// <summary>
        /// Occurs when AreEnemyTargetsInRange changes. Only fires on a change
        /// in the property state, not when the qty of enemy targets in range changes.
        /// </summary>
        event EventHandler enemyTargetsInRangeChgd;

        /// <summary>
        /// Occurs when AreEnemyCmdsInRange changes. Only fires on a change
        /// in the property state, not when the qty of enemy cmds in range changes.
        /// </summary>
        event EventHandler enemyCmdsInRangeChgd;

        /// <summary>
        /// Occurs when AreWarEnemyElementsInRange changes. Only fires on a change
        /// in the property state, not when the qty of war enemy elements in range changes.
        /// </summary>
        event EventHandler warEnemyElementsInRangeChgd;

        /// <summary>
        /// Indicates whether there are any enemy targets in range including WarEnemy targets.
        /// </summary>
        bool AreEnemyTargetsInRange { get; }

        /// <summary>
        /// Indicates whether there are any enemy UnitElements in range including WarEnemy UnitElements.
        /// <remarks>Not subscribable as AreEnemyTargetsInRange could be incorrect when it fired.</remarks>
        /// </summary>
        bool AreEnemyElementsInRange { get; }

        /// <summary>
        /// Indicates whether there are any enemy UnitCmds in range including WarEnemy UnitCmds.
        /// <remarks>Not subscribable as AreEnemyTargetsInRange could be incorrect when it fired.</remarks>
        /// </summary>
        bool AreEnemyCmdsInRange { get; }

        /// <summary>
        /// Indicates whether there are any enemy 'Bombardable' Planetoids in range including WarEnemy Planetoids.
        /// <remarks>Not subscribable as AreEnemyTargetsInRange could be incorrect when it fired.</remarks>
        /// </summary>
        bool AreEnemyPlanetoidsInRange { get; }

        /// <summary>
        /// Indicates whether there are any enemy targets in range where DiplomaticRelationship.War exists.
        /// <remarks>Not subscribable as AreEnemyTargetsInRange could be incorrect when it fired.</remarks>
        /// </summary>
        bool AreWarEnemyTargetsInRange { get; }

        /// <summary>
        /// Indicates whether there are any enemy UnitElements in range where DiplomaticRelationship.War exists.
        /// <remarks>Not subscribable as AreEnemyTargetsInRange could be incorrect when it fired.</remarks>
        /// </summary>
        bool AreWarEnemyElementsInRange { get; }

        /// <summary>
        /// Indicates whether there are any enemy UnitCmds in range where DiplomaticRelationship.War exists.
        /// <remarks>Not subscribable as AreEnemyTargetsInRange could be incorrect when it fired.</remarks>
        /// </summary>
        bool AreWarEnemyCmdsInRange { get; }

        /// <summary>
        /// Indicates whether there are any enemy 'Bombardable' Planetoids in range where DiplomaticRelationship.War exists.
        /// <remarks>Not subscribable as AreEnemyTargetsInRange could be incorrect when it fired.</remarks>
        /// </summary>
        bool AreWarEnemyPlanetoidsInRange { get; }

        /// <summary>
        /// A copy of all the detected enemy targets that are in range of the sensors of this monitor.
        /// <remarks>Includes both ColdWar and War enemies.</remarks>
        /// <remarks>TODO 3.27.17 Not currently used as planetoids no longer IElementAttackable, aka Targets
        /// and Elements sets always the same. Will be used again once other enemy owned Items besides elements can 
        /// be fired on by 'normal' (not Bombard type) weapons.</remarks>
        /// </summary>
        HashSet<IElementAttackable> EnemyTargetsDetected { get; }

        /// <summary>
        /// A copy of all the detected enemy UnitElements that are in range of the sensors of this monitor.
        /// <remarks>Includes both ColdWar and War enemies.</remarks>
        /// </summary>
        HashSet<IUnitElement_Ltd> EnemyElementsDetected { get; }

        /// <summary>
        /// A copy of all the detected enemy UnitCmds that are in range of the sensors of this monitor.
        /// <remarks>Includes both ColdWar and War enemies.</remarks>
        /// <remarks>While a UnitCmd is not itself detectable, its HQElement is.</remarks>
        /// </summary>
        HashSet<IUnitCmd_Ltd> EnemyCmdsDetected { get; }

        /// <summary>
        /// A copy of all the detected enemy 'Bombardable' Planetoids that are in range of the sensors of this monitor.
        /// <remarks>Includes both ColdWar and War enemies.</remarks>
        /// </summary>
        HashSet<IPlanetoid_Ltd> EnemyPlanetoidsDetected { get; }


        /// <summary>
        /// A copy of all the detected war enemy targets that are in range of the sensors of this monitor.
        /// <remarks>TODO 3.27.17 Not currently used as planetoids no longer IElementAttackable, aka Targets
        /// and Elements sets always the same. Will be used again once other enemy owned Items besides elements can 
        /// be fired on by 'normal' (not Bombard type) weapons.</remarks>
        /// </summary>
        HashSet<IElementAttackable> WarEnemyTargetsDetected { get; }

        /// <summary>
        /// A copy of all the detected war enemy UnitElements that are in range of the sensors of this monitor.
        /// </summary>
        HashSet<IUnitElement_Ltd> WarEnemyElementsDetected { get; }

        /// <summary>
        /// A copy of all the detected war enemy UnitCmds that are in range of the sensors of this monitor.
        /// <remarks>While a UnitCmd is not itself detectable, its HQElement is.</remarks>
        /// </summary>
        HashSet<IUnitCmd_Ltd> WarEnemyCmdsDetected { get; }

        /// <summary>
        /// A copy of all the detected war enemy 'Bombardable' Planetoids that are in range of the sensors of this monitor.
        /// </summary>
        HashSet<IPlanetoid_Ltd> WarEnemyPlanetoidsDetected { get; }

        HashSet<IElementAttackable> UnknownTargetsDetected { get; }

        void Add(ASensor sensor);

        /// <summary>
        /// Removes the specified sensor. Returns <c>true</c> if this monitor
        /// is still in use (has sensors remaining even if not operational), <c>false</c> otherwise.
        /// </summary>
        /// <param name="sensor">The sensor.</param>
        /// <returns></returns>
        bool Remove(ASensor sensor);

        /// <summary>
        /// Resets this Monitor for reuse by the parent item.
        /// </summary>
        void Reset();

    }
}

