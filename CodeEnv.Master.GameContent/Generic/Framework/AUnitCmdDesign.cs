// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCmdDesign.cs
// Abstract design holding the stats of a unit command for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common;
    using Common.LocalResources;

    /// <summary>
    /// Abstract design holding the stats of a unit command for a player.
    /// </summary>
    public abstract class AUnitCmdDesign : AUnitMemberDesign {

        private static EquipmentMountCategory[] SupportedMountCategories =  {
                                                                                EquipmentMountCategory.Skin,
                                                                                EquipmentMountCategory.Sensor,
                                                                                EquipmentMountCategory.Flex
                                                                            };

        static AUnitCmdDesign() {
            __ValidateSupportedMountsCanAccommodateSupportedEquipment();
        }

        public FtlDampenerStat FtlDampenerStat { get; private set; }

        public SensorStat ReqdMRSensorStat { get; private set; }

        public ACmdModuleStat ReqdCmdStat { get; private set; }

        public sealed override AtlasID ImageAtlasID { get { return ReqdCmdStat.ImageAtlasID; } }

        public sealed override string ImageFilename { get { return ReqdCmdStat.ImageFilename; } }

        // 9.24.17 A CommandDesign is not constructed via a Base ConstructionQueue so no need for a ConstructionCost

        protected override EquipmentMountCategory[] SupportedHullMountCategories { get { return SupportedMountCategories; } }

        public AUnitCmdDesign(Player player, FtlDampenerStat ftlDampenerStat, SensorStat reqdMRSensorStat, ACmdModuleStat reqdCmdStat)
            : base(player) {
            FtlDampenerStat = ftlDampenerStat;
            ReqdMRSensorStat = reqdMRSensorStat;
            ReqdCmdStat = reqdCmdStat;
        }

        protected override float CalcConstructionCost() {
            float cumConstructionCost = base.CalcConstructionCost();
            cumConstructionCost += ReqdCmdStat.ConstructionCost;
            cumConstructionCost += ReqdMRSensorStat.ConstructionCost;
            cumConstructionCost += FtlDampenerStat.ConstructionCost;
            return cumConstructionCost;
        }

        protected override float CalcHitPoints() {
            float cumHitPts = base.CalcHitPoints();
            cumHitPts += ReqdCmdStat.HitPoints;
            cumHitPts += ReqdMRSensorStat.HitPoints;
            cumHitPts += FtlDampenerStat.HitPoints;
            return cumHitPts;
        }

        /// <summary>
        /// Returns the maximum number of AEquipmentStat slots that this design is allowed for the provided HullMountCategory.
        /// <remarks>AEquipmentStats that are required for a design are not included. These are typically added via the constructor.</remarks>
        /// </summary>
        /// <param name="equipCat">The equip cat.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override int GetMaxOptionalEquipmentSlotsFor(EquipmentMountCategory mountCat) {
            switch (mountCat) {
                case EquipmentMountCategory.Sensor:
                    return TempGameValues.MaxSensorMounts;
                case EquipmentMountCategory.Skin:
                    return TempGameValues.MaxSkinMounts;
                case EquipmentMountCategory.Flex:
                    return TempGameValues.MaxFlexMounts;
                case EquipmentMountCategory.Screen:
                case EquipmentMountCategory.Turret:
                case EquipmentMountCategory.Silo:
                case EquipmentMountCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mountCat));
            }
        }

        protected override bool IsNonStatContentEqual(AUnitMemberDesign oDesign) {
            if (base.IsNonStatContentEqual(oDesign)) {
                var cmdDesign = oDesign as AUnitCmdDesign;
                return cmdDesign.FtlDampenerStat == FtlDampenerStat && cmdDesign.ReqdMRSensorStat == ReqdMRSensorStat
                    && cmdDesign.ReqdCmdStat == ReqdCmdStat;
            }
            return false;
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private static void __ValidateSupportedMountsCanAccommodateSupportedEquipment() {
            IEnumerable<EquipmentCategory> equipmentSupportedByMounts = new List<EquipmentCategory>();
            foreach (var mount in SupportedMountCategories) {
                equipmentSupportedByMounts = equipmentSupportedByMounts.Union(mount.SupportedEquipment());
            }
            TempGameValues.EquipCatsSupportedByCmdModuleDesigner.ForAll(se => D.Assert(equipmentSupportedByMounts.Contains(se)));
        }

        #endregion

        #region Value-based Equality Archive

        ////public static bool operator ==(AUnitCmdDesign left, AUnitCmdDesign right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(AUnitCmdDesign left, AUnitCmdDesign right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + FtlDampenerStat.GetHashCode();
        ////        hash = hash * 31 + ReqdMRSensorStat.GetHashCode();
        ////        hash = hash * 31 + ReqdCmdStat.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        AUnitCmdDesign oDesign = (AUnitCmdDesign)obj;
        ////        return oDesign.FtlDampenerStat == FtlDampenerStat && oDesign.ReqdMRSensorStat == ReqdMRSensorStat
        ////            && oDesign.ReqdCmdStat == ReqdCmdStat;
        ////    }
        ////    return false;
        ////}

        #endregion



    }
}

