// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCreatorEditorSettings.cs
// Wrapper for EditorFleetCreator settings used in debugging.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Wrapper for EditorFleetCreator settings used in debugging.
    /// </summary>
    public class FleetCreatorEditorSettings : AUnitCreatorEditorSettings {

        public bool Move { get; private set; }

        public bool FindFarthest { get; private set; }

        public bool Attack { get; private set; }

        public DebugShipCombatStanceExclusions StanceExclusions { get; private set; }

        public IList<ShipHullCategory> PresetElementHullCategories { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCreatorEditorSettings" /> struct
        /// when the unit composition is NOT preset.
        /// </summary>
        /// <param name="isOwnerUser">if set to <c>true</c> [is owner user].</param>
        /// <param name="elementQty">The element qty.</param>
        /// <param name="userRelations">The user relations.</param>
        /// <param name="cmsPerCmd">The CMS per command.</param>
        /// <param name="sensorsPerCmd">MR and LR sensor qty per command.</param>
        /// <param name="activeCMs">The active c ms.</param>
        /// <param name="deployDate">The deploy date.</param>
        /// <param name="losTurrets">The LOS turrets.</param>
        /// <param name="launchers">The launchers.</param>
        /// <param name="passiveCMs">The passive c ms.</param>
        /// <param name="shieldGens">The shield gens.</param>
        /// <param name="srSensors">SR sensor qty per element.</param>
        /// <param name="toMove">if set to <c>true</c> [to move].</param>
        /// <param name="findFarthest">if set to <c>true</c> [find farthest].</param>
        /// <param name="toAttack">if set to <c>true</c> [to attack].</param>
        /// <param name="stanceExclusions">The stance exclusions.</param>
        public FleetCreatorEditorSettings(bool isOwnerUser, int elementQty, DebugDiploUserRelations userRelations,
            DebugPassiveCMLoadout cmsPerCmd, DebugSensorLoadout sensorsPerCmd, DebugActiveCMLoadout activeCMs, GameDate deployDate,
            DebugLosWeaponLoadout losTurrets, DebugLaunchedWeaponLoadout launchers, DebugPassiveCMLoadout passiveCMs,
            DebugShieldGenLoadout shieldGens, DebugSensorLoadout srSensors, bool toMove, bool findFarthest, bool toAttack,
            DebugShipCombatStanceExclusions stanceExclusions)
            : base(isOwnerUser, elementQty, userRelations, cmsPerCmd, sensorsPerCmd, activeCMs, deployDate, losTurrets, launchers,
                  passiveCMs, shieldGens, srSensors) {
            Move = toMove;
            FindFarthest = findFarthest;
            Attack = toAttack;
            StanceExclusions = stanceExclusions;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="FleetCreatorEditorSettings" /> struct
        /// when the unit composition is preset.
        /// </summary>
        /// <param name="isOwnerUser">if set to <c>true</c> [is owner user].</param>
        /// <param name="userRelations">The user relations.</param>
        /// <param name="cmsPerCmd">The CMS per command.</param>
        /// <param name="sensorsPerCmd">MR and LR sensor qty per command.</param>
        /// <param name="activeCMs">The active c ms.</param>
        /// <param name="deployDate">The deploy date.</param>
        /// <param name="losTurrets">The LOS turrets.</param>
        /// <param name="launchers">The launchers.</param>
        /// <param name="passiveCMs">The passive c ms.</param>
        /// <param name="shieldGens">The shield gens.</param>
        /// <param name="srSensors">SR sensor qty per element.</param>
        /// <param name="toMove">if set to <c>true</c> [to move].</param>
        /// <param name="findFarthest">if set to <c>true</c> [find farthest].</param>
        /// <param name="toAttack">if set to <c>true</c> [to attack].</param>
        /// <param name="stanceExclusions">The stance exclusions.</param>
        /// <param name="presetHullCats">The preset hull cats.</param>
        public FleetCreatorEditorSettings(bool isOwnerUser, DebugDiploUserRelations userRelations, DebugPassiveCMLoadout cmsPerCmd,
            DebugSensorLoadout sensorsPerCmd, DebugActiveCMLoadout activeCMs, GameDate deployDate, DebugLosWeaponLoadout losTurrets,
            DebugLaunchedWeaponLoadout launchers, DebugPassiveCMLoadout passiveCMs, DebugShieldGenLoadout shieldGens,
            DebugSensorLoadout srSensors, bool toMove, bool findFarthest, bool toAttack, DebugShipCombatStanceExclusions stanceExclusions,
            IList<ShipHullCategory> presetHullCats)
            : base(isOwnerUser, userRelations, cmsPerCmd, sensorsPerCmd, activeCMs, deployDate, losTurrets, launchers, passiveCMs,
                shieldGens, srSensors) {
            Move = toMove;
            FindFarthest = findFarthest;
            Attack = toAttack;
            StanceExclusions = stanceExclusions;
            PresetElementHullCategories = presetHullCats;
        }

    }
}

