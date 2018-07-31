﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdModuleDesign.cs
// The design of a Starbase Command Module for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// The design of a Starbase Command Module for a player.
    /// </summary>
    public class StarbaseCmdModuleDesign : AUnitCmdModuleDesign {

        private static StarbaseCmdModuleStat GetImprovedReqdStat(Player player, StarbaseCmdModuleStat existingStat) {
            var designs = GameReferences.GameManager.GetAIManagerFor(player).Designs;
            StarbaseCmdModuleStat currentStat;
            var isFound = designs.TryGetCurrentStarbaseCmdModuleStat(out currentStat);
            D.Assert(isFound);  // has to be there as need existingStat to request improvement
            return currentStat.Level > existingStat.Level ? currentStat : existingStat;
        }

        public new StarbaseCmdModuleStat CmdModuleStat { get { return base.CmdModuleStat as StarbaseCmdModuleStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarbaseCmdModuleDesign"/> class.
        /// <remarks>This version automatically improves any Reqd EquipmentStats to the highest Level available,
        /// and copies the rest of the content of the design into the new design instance, allowing the player to upgrade and/or change 
        /// the mix of optional EquipmentStats.</remarks>
        /// <remarks>Warning: Does NOT transfer over Status which will be None until externally changed.</remarks>
        /// </summary>
        /// <param name="designToImprove">The design to improve.</param>
        public StarbaseCmdModuleDesign(StarbaseCmdModuleDesign designToImprove)
            : this(designToImprove.Player, GetImprovedReqdStat(designToImprove.Player, designToImprove.FtlDamperStat),
            GetImprovedReqdStat(designToImprove.Player, designToImprove.CmdModuleStat),
            GetImprovedReqdStat(designToImprove.Player, designToImprove.ReqdMRSensorStat)) {

            D.AssertNotEqual(SourceAndStatus.SystemCreation_Default, designToImprove.Status);

            OptionalEquipSlotID slotID;
            AEquipmentStat equipStat;
            while (designToImprove.TryGetNextOptEquipStat(out slotID, out equipStat)) {
                Add(slotID, equipStat);
            }
            AssignPropertyValues();

            RootDesignName = designToImprove.RootDesignName;
            // If copying System_CreationTemplate counter will always = 0 as they are never incremented. If copying Player_Current counter 
            // will be >= 0 ready to be incremented. If copying Player_Obsolete a new RootDesignName will be assigned resetting counter
            // to 0 to avoid creating duplicate design names when incrementing.
            DesignLevel = designToImprove.DesignLevel;
        }

        public StarbaseCmdModuleDesign(Player player, FtlDamperStat ftlDamperStat, StarbaseCmdModuleStat cmdStat, SensorStat reqdMRSensorStat)
            : base(player, ftlDamperStat, reqdMRSensorStat, cmdStat) {
            InitializeValuesAndReferences();
        }

        protected override bool IsNonOptionalStatContentEqual(AUnitMemberDesign oDesign) {
            return base.IsNonOptionalStatContentEqual(oDesign);
        }

        #region Value-based Equality Archive

        ////public static bool operator ==(StarbaseCmdModuleDesign left, StarbaseCmdModuleDesign right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(StarbaseCmdModuleDesign left, StarbaseCmdModuleDesign right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        return base.GetHashCode();
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    return base.Equals(obj);
        ////}

        #endregion



    }
}

