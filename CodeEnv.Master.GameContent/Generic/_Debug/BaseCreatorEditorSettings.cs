// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: BaseCreatorEditorSettings.cs
// Wrapper for Editor(Starbase/Settlement)Creator settings used in debugging.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Wrapper for Editor(Starbase/Settlement)Creator settings used in debugging.
    /// </summary>
    public class BaseCreatorEditorSettings : AUnitCreatorEditorSettings {

        public DebugBaseFormation Formation { get; private set; }

        public IList<FacilityHullCategory> PresetElementHullCategories { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCreatorEditorSettings" /> struct
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
        /// <param name="formation">The formation.</param>
        public BaseCreatorEditorSettings(string unitName, bool isOwnerUser, int elementQty, DebugDiploUserRelations userRelations, int cmsPerCmd, int activeCMs,
            GameDate deployDate, DebugLosWeaponLoadout losTurrets, DebugWeaponLoadout missileLaunchers, int passiveCMs, int shieldGens, int sensors,
            DebugBaseFormation formation)
            : base(unitName, isOwnerUser, elementQty, userRelations, cmsPerCmd, activeCMs, deployDate, losTurrets, missileLaunchers, passiveCMs,
                shieldGens, sensors) {
            Formation = formation;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCreatorEditorSettings" /> struct
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
        /// <param name="formation">The formation.</param>
        /// <param name="presetHullCats">The preset hull cats.</param>
        public BaseCreatorEditorSettings(string unitName, bool isOwnerUser, DebugDiploUserRelations userRelations, int cmsPerCmd, int activeCMs,
            GameDate deployDate, DebugLosWeaponLoadout losTurrets, DebugWeaponLoadout missileLaunchers, int passiveCMs, int shieldGens, int sensors,
            DebugBaseFormation formation, IList<FacilityHullCategory> presetHullCats)
            : base(unitName, isOwnerUser, userRelations, cmsPerCmd, activeCMs, deployDate, losTurrets, missileLaunchers, passiveCMs, shieldGens,
                  sensors) {
            Formation = formation;
            PresetElementHullCategories = presetHullCats;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

