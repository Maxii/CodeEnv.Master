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

        public string UnitName { get; private set; }

        public bool IsOwnerUser { get; private set; }

        public bool IsCompositionPreset { get; private set; }

        public int NonPresetElementQty { get; private set; }

        public DebugDiploUserRelations DesiredRelationshipWithUser { get; private set; }

        public int CMsPerCommand { get; private set; }

        public int ActiveCMsPerElement { get; private set; }

        public GameDate DateToDeploy { get; private set; }

        public DebugWeaponLoadout LosTurretsPerElement { get; private set; }

        public DebugWeaponLoadout MissileLaunchersPerElement { get; private set; }

        public int PassiveCMsPerElement { get; private set; }

        public int ShieldGeneratorsPerElement { get; private set; }

        public int SensorsPerElement { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AUnitCreatorEditorSettings" /> struct
        /// when the unit composition is NOT preset.
        /// </summary>
        /// <param name="unitName">Name of the unit.</param>
        /// <param name="isOwnerUser">if set to <c>true</c> [is owner user].</param>
        /// <param name="elementQty">The element qty.</param>
        /// <param name="userRelations">The user relations.</param>
        /// <param name="cmsPerCmd">The CMS per command.</param>
        /// <param name="activeCMs">The active c ms.</param>
        /// <param name="deployDate">The deploy date.</param>
        /// <param name="losTurrets">The LOS turrets.</param>
        /// <param name="missileLaunchers">The missile launchers.</param>
        /// <param name="passiveCMs">The passive c ms.</param>
        /// <param name="shieldGens">The shield gens.</param>
        /// <param name="sensors">The sensors.</param>
        public AUnitCreatorEditorSettings(string unitName, bool isOwnerUser, int elementQty, DebugDiploUserRelations userRelations, int cmsPerCmd,
            int activeCMs, GameDate deployDate, DebugWeaponLoadout losTurrets, DebugWeaponLoadout missileLaunchers, int passiveCMs, int shieldGens,
            int sensors) {
            UnitName = unitName;
            IsOwnerUser = isOwnerUser;
            IsCompositionPreset = false;
            NonPresetElementQty = elementQty;
            DesiredRelationshipWithUser = userRelations;
            CMsPerCommand = cmsPerCmd;
            ActiveCMsPerElement = activeCMs;
            DateToDeploy = deployDate;
            LosTurretsPerElement = losTurrets;
            MissileLaunchersPerElement = missileLaunchers;
            PassiveCMsPerElement = passiveCMs;
            ShieldGeneratorsPerElement = shieldGens;
            SensorsPerElement = sensors;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AUnitCreatorEditorSettings" /> struct
        /// when the unit composition is preset.
        /// </summary>
        /// <param name="unitName">Name of the unit.</param>
        /// <param name="isOwnerUser">if set to <c>true</c> [is owner user].</param>
        /// <param name="userRelations">The user relations.</param>
        /// <param name="cmsPerCmd">The CMS per command.</param>
        /// <param name="activeCMs">The active c ms.</param>
        /// <param name="deployDate">The deploy date.</param>
        /// <param name="losTurrets">The LOS turrets.</param>
        /// <param name="missileLaunchers">The missile launchers.</param>
        /// <param name="passiveCMs">The passive c ms.</param>
        /// <param name="shieldGens">The shield gens.</param>
        /// <param name="sensors">The sensors.</param>
        public AUnitCreatorEditorSettings(string unitName, bool isOwnerUser, DebugDiploUserRelations userRelations, int cmsPerCmd, int activeCMs,
            GameDate deployDate, DebugWeaponLoadout losTurrets, DebugWeaponLoadout missileLaunchers, int passiveCMs, int shieldGens, int sensors) {
            UnitName = unitName;
            IsOwnerUser = isOwnerUser;
            IsCompositionPreset = true;
            NonPresetElementQty = Constants.Zero;
            DesiredRelationshipWithUser = userRelations;
            CMsPerCommand = cmsPerCmd;
            ActiveCMsPerElement = activeCMs;
            DateToDeploy = deployDate;
            LosTurretsPerElement = losTurrets;
            MissileLaunchersPerElement = missileLaunchers;
            PassiveCMsPerElement = passiveCMs;
            ShieldGeneratorsPerElement = shieldGens;
            SensorsPerElement = sensors;
        }

    }
}

