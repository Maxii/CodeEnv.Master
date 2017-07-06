// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCreatorEditorSettings.cs
// Abstract data container for Unit Creator editor settings used in debugging.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract data container for Unit Creator editor settings used in debugging.
    /// </summary>
    public abstract class AUnitCreatorEditorSettings {

        public string DebugName { get { return GetType().Name; } }

        public bool IsOwnerUser { get; private set; }

        public bool IsCompositionPreset { get; private set; }

        public int NonPresetElementQty { get; private set; }

        public DebugDiploUserRelations DesiredRelationshipWithUser { get; private set; }

        public DebugPassiveCMLoadout CMsPerCommand { get; private set; }

        public DebugSensorLoadout SensorsPerCommand { get; private set; }

        public DebugActiveCMLoadout ActiveCMsPerElement { get; private set; }

        public GameDate DateToDeploy { get; private set; }

        public DebugLosWeaponLoadout LosTurretLoadout { get; private set; }

        public DebugLaunchedWeaponLoadout LauncherLoadout { get; private set; }

        public DebugPassiveCMLoadout PassiveCMsPerElement { get; private set; }

        public DebugShieldGenLoadout ShieldGeneratorsPerElement { get; private set; }

        public DebugSensorLoadout SRSensorsPerElement { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AUnitCreatorEditorSettings" /> struct
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
        public AUnitCreatorEditorSettings(bool isOwnerUser, int elementQty, DebugDiploUserRelations userRelations,
            DebugPassiveCMLoadout cmsPerCmd, DebugSensorLoadout sensorsPerCmd, DebugActiveCMLoadout activeCMs, GameDate deployDate,
            DebugLosWeaponLoadout losTurrets, DebugLaunchedWeaponLoadout launchers, DebugPassiveCMLoadout passiveCMs,
            DebugShieldGenLoadout shieldGens, DebugSensorLoadout srSensors) {
            IsOwnerUser = isOwnerUser;
            IsCompositionPreset = false;
            NonPresetElementQty = elementQty;
            DesiredRelationshipWithUser = userRelations;
            CMsPerCommand = cmsPerCmd;
            SensorsPerCommand = sensorsPerCmd;
            ActiveCMsPerElement = activeCMs;
            DateToDeploy = deployDate;
            LosTurretLoadout = losTurrets;
            LauncherLoadout = launchers;
            PassiveCMsPerElement = passiveCMs;
            ShieldGeneratorsPerElement = shieldGens;
            SRSensorsPerElement = srSensors;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AUnitCreatorEditorSettings" /> struct
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
        public AUnitCreatorEditorSettings(bool isOwnerUser, DebugDiploUserRelations userRelations, DebugPassiveCMLoadout cmsPerCmd,
            DebugSensorLoadout sensorsPerCmd, DebugActiveCMLoadout activeCMs, GameDate deployDate, DebugLosWeaponLoadout losTurrets,
            DebugLaunchedWeaponLoadout launchers, DebugPassiveCMLoadout passiveCMs, DebugShieldGenLoadout shieldGens,
            DebugSensorLoadout srSensors) {
            IsOwnerUser = isOwnerUser;
            IsCompositionPreset = true;
            NonPresetElementQty = Constants.Zero;
            DesiredRelationshipWithUser = userRelations;
            CMsPerCommand = cmsPerCmd;
            ActiveCMsPerElement = activeCMs;
            DateToDeploy = deployDate;
            LosTurretLoadout = losTurrets;
            LauncherLoadout = launchers;
            PassiveCMsPerElement = passiveCMs;
            ShieldGeneratorsPerElement = shieldGens;
            SRSensorsPerElement = srSensors;
        }

        public sealed override string ToString() {
            return DebugName;
        }

    }
}

