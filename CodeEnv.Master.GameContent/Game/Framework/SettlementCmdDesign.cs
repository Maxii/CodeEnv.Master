// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdDesign.cs
// The design of a Settlement Command for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The design of a Settlement Command for a player.
    /// </summary>
    public class SettlementCmdDesign : AUnitCmdDesign {

        public new SettlementCmdModuleStat ReqdCmdStat { get { return base.ReqdCmdStat as SettlementCmdModuleStat; } }

        public SettlementCmdDesign(SettlementCmdDesign designToCopy)
            : this(designToCopy.Player, designToCopy.FtlDampenerStat, designToCopy.ReqdCmdStat, designToCopy.ReqdMRSensorStat) {

            EquipmentSlotID slotID;
            AEquipmentStat equipStat;
            while (designToCopy.TryGetNextEquipmentStat(out slotID, out equipStat)) {
                Add(slotID, equipStat);
            }
            AssignPropertyValues();

            RootDesignName = designToCopy.RootDesignName;
            // If copying System_CreationTemplate counter will always = 0 as they are never incremented. If copying Player_Current counter 
            // will be >= 0 ready to be incremented. If copying Player_Obsolete a new RootDesignName will be assigned resetting counter
            // to 0 to avoid creating duplicate design names when incrementing.
            DesignLevel = designToCopy.DesignLevel;
        }

        public SettlementCmdDesign(Player player, FtlDampenerStat ftlDampenerStat, SettlementCmdModuleStat cmdStat, SensorStat reqdMRSensorStat)
            : base(player, ftlDampenerStat, reqdMRSensorStat, cmdStat) {
            InitializeValuesAndReferences();
        }

        protected override bool IsNonStatContentEqual(AUnitMemberDesign oDesign) {
            return base.IsNonStatContentEqual(oDesign);
        }

        #region Value-based Equality Archive

        ////public static bool operator ==(SettlementCmdDesign left, SettlementCmdDesign right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(SettlementCmdDesign left, SettlementCmdDesign right) {
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

