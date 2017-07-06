// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipDesign.cs
// Class holding the design of a ship for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using Common.LocalResources;

    /// <summary>
    /// Class holding the design of a ship for a player.
    /// </summary>
    public class ShipDesign : AElementDesign {

        public ShipHullCategory HullCategory { get { return HullStat.HullCategory; } }

        public ShipHullStat HullStat { get; private set; }

        public EngineStat StlEngineStat { get; private set; }

        public EngineStat FtlEngineStat { get; private set; }

        public ShipCombatStance CombatStance { get; private set; }

        public ShipDesign(ShipDesign designToCopy)
            : this(designToCopy.Player, designToCopy.HQPriority, designToCopy.ReqdSRSensorStat, designToCopy.HullStat,
                  designToCopy.StlEngineStat, designToCopy.FtlEngineStat, designToCopy.CombatStance) {

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

        public ShipDesign(Player player, Priority hqPriority, SensorStat reqdSRSensorStat, ShipHullStat hullStat,
            EngineStat stlEngineStat, EngineStat ftlEngineStat, ShipCombatStance combatStance)
            : base(player, hqPriority, reqdSRSensorStat) {
            HullStat = hullStat;
            StlEngineStat = stlEngineStat;
            FtlEngineStat = ftlEngineStat;
            CombatStance = combatStance;
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
        ////        hash = hash * 31 + StlEngineStat.GetHashCode();
        ////        hash = hash * 31 + FtlEngineStat.GetHashCode();
        ////        hash = hash * 31 + CombatStance.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        ShipDesign oDesign = (ShipDesign)obj;
        ////        bool isEqual = oDesign.HullStat == HullStat && oDesign.StlEngineStat == StlEngineStat && oDesign.FtlEngineStat == FtlEngineStat
        ////            && oDesign.CombatStance == CombatStance;
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

