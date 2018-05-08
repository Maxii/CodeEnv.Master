// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdModuleDesign.cs
// The design of a Settlement Command Module for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The design of a Settlement Command Module for a player.
    /// </summary>
    public class SettlementCmdModuleDesign : AUnitCmdModuleDesign {

        private static SettlementCmdModuleStat GetImprovedReqdStat(Player player, SettlementCmdModuleStat existingStat) {
            var designs = GameReferences.GameManager.GetAIManagerFor(player).Designs;
            var currentStat = designs.GetCurrentSettlementCmdModuleStat();
            return currentStat.Level > existingStat.Level ? currentStat : existingStat;
        }

        public new SettlementCmdModuleStat CmdModuleStat { get { return base.CmdModuleStat as SettlementCmdModuleStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdModuleDesign"/> class.
        /// <remarks>This version automatically improves any Reqd EquipmentStats to the highest Level available,
        /// and copies the rest of the content of the design into the new design instance, allowing the player to upgrade and/or change 
        /// the mix of optional EquipmentStats.</remarks>
        /// </summary>
        /// <param name="designToImprove">The design to improve.</param>
        public SettlementCmdModuleDesign(SettlementCmdModuleDesign designToImprove)
            : this(designToImprove.Player, GetImprovedReqdStat(designToImprove.Player, designToImprove.FtlDampenerStat),
                  GetImprovedReqdStat(designToImprove.Player, designToImprove.CmdModuleStat),
                  GetImprovedReqdStat(designToImprove.Player, designToImprove.ReqdMRSensorStat)) {

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

        public SettlementCmdModuleDesign(Player player, FtlDampenerStat ftlDampenerStat, SettlementCmdModuleStat cmdModStat, SensorStat reqdMRSensorStat)
            : base(player, ftlDampenerStat, reqdMRSensorStat, cmdModStat) {
            InitializeValuesAndReferences();
        }

        protected override bool IsNonStatContentEqual(AUnitMemberDesign oDesign) {
            return base.IsNonStatContentEqual(oDesign);
        }

        #region Value-based Equality Archive

        ////public static bool operator ==(SettlementCmdModuleDesign left, SettlementCmdModuleDesign right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(SettlementCmdModuleDesign left, SettlementCmdModuleDesign right) {
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

