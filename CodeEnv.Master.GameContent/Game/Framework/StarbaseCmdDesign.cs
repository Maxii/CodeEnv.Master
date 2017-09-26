﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdDesign.cs
// The design of a Starbase Command for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The design of a Starbase Command for a player.
    /// </summary>
    public class StarbaseCmdDesign : AUnitCmdDesign {

        public new StarbaseCmdModuleStat ReqdCmdStat { get { return base.ReqdCmdStat as StarbaseCmdModuleStat; } }

        public StarbaseCmdDesign(StarbaseCmdDesign designToCopy)
            : this(designToCopy.Player, designToCopy.FtlDampenerStat, designToCopy.ReqdCmdStat, designToCopy.ReqdMRSensorStat) {

            EquipmentSlotID slotID;
            AEquipmentStat equipStat;
            while (designToCopy.GetNextEquipmentStat(out slotID, out equipStat)) {
                Add(slotID, equipStat);
            }

            RootDesignName = designToCopy.RootDesignName;
            // If copying System_CreationTemplate counter will always = 0 as they are never incremented. If copying Player_Current counter 
            // will be >= 0 ready to be incremented. If copying Player_Obsolete a new RootDesignName will be assigned resetting counter
            // to 0 to avoid creating duplicate design names when incrementing.
            _designNameCounter = designToCopy._designNameCounter;
        }

        public StarbaseCmdDesign(Player player, FtlDampenerStat ftlDampenerStat, StarbaseCmdModuleStat cmdStat, SensorStat reqdMRSensorStat)
            : base(player, ftlDampenerStat, reqdMRSensorStat, cmdStat) {
            InitializeValuesAndReferences();
        }

        #region Value-based Equality Archive

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + CmdStat.GetHashCode(); // 31 = another prime number
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        StarbaseCmdDesign oDesign = (StarbaseCmdDesign)obj;
        ////        bool isEqual = oDesign.CmdStat == CmdStat;
        ////        if (isEqual) {
        ////            __ValidateHashCodesEqual(obj);
        ////        }
        ////        return isEqual;
        ////    }
        ////    return false;
        ////}

        #endregion



    }
}

