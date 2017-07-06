﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityDesign.cs
// Class holding the design of a facility for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using Common.LocalResources;

    /// <summary>
    /// Class holding the design of a facility for a player.
    /// </summary>
    public class FacilityDesign : AElementDesign {

        public FacilityHullCategory HullCategory { get { return HullStat.HullCategory; } }

        public FacilityHullStat HullStat { get; private set; }

        public FacilityDesign(FacilityDesign designToCopy)
            : this(designToCopy.Player, designToCopy.HQPriority, designToCopy.ReqdSRSensorStat,
                  designToCopy.HullStat) {

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

        public FacilityDesign(Player player, Priority hqPriority, SensorStat reqdSRSensorStat, FacilityHullStat hullStat)
            : base(player, hqPriority, reqdSRSensorStat) {
            HullStat = hullStat;
            InitializeValuesAndReferences();
        }

        protected override int GetMaxSlotsFor(EquipmentCategory equipCat) {
            switch (equipCat) {
                case EquipmentCategory.PassiveCountermeasure:
                    return HullCategory.__MaxPassiveCMs();
                case EquipmentCategory.ActiveCountermeasure:
                    return HullCategory.__MaxActiveCMs();
                case EquipmentCategory.Sensor:
                    return HullCategory.__MaxSensors() - Constants.One;
                case EquipmentCategory.ShieldGenerator:
                    return HullCategory.__MaxShieldGenerators();
                case EquipmentCategory.LosWeapon:
                    return HullCategory.__MaxLOSWeapons();
                case EquipmentCategory.LaunchedWeapon:
                    return HullCategory.__MaxLaunchedWeapons();
                case EquipmentCategory.Propulsion:
                case EquipmentCategory.FtlDampener:
                case EquipmentCategory.Hull:
                case EquipmentCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(equipCat));
            }
        }

        #region Value-based Equality Archive

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + HullStat.GetHashCode(); // 31 = another prime number
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        FacilityDesign oDesign = (FacilityDesign)obj;
        ////        bool isEqual = oDesign.HullStat == HullStat;
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

