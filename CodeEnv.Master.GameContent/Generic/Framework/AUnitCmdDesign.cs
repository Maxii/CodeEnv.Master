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

        public static EquipmentCategory[] SupportedEquipCategories =  {
                                                                            EquipmentCategory.PassiveCountermeasure,
                                                                            EquipmentCategory.CommandSensor
                                                                        };

        public FtlDampenerStat FtlDampenerStat { get; private set; }

        [Obsolete]
        public IEnumerable<SensorStat> SensorStats {
            get {
                var keys = _equipLookupBySlotID.Keys.Where(key => key.Category == EquipmentCategory.CommandSensor && _equipLookupBySlotID[key] != null);
                IList<SensorStat> stats = new List<SensorStat>();
                foreach (var key in keys) {
                    stats.Add(_equipLookupBySlotID[key] as SensorStat);
                }
                return stats;
            }
        }

        public SensorStat ReqdMRSensorStat { get; private set; }

        public ACmdModuleStat ReqdCmdStat { get; private set; }

        public sealed override AtlasID ImageAtlasID { get { return ReqdCmdStat.ImageAtlasID; } }

        public sealed override string ImageFilename { get { return ReqdCmdStat.ImageFilename; } }

        // 9.24.17 A CommandDesign is not constructed via a Base ConstructionQueue so no need for a ConstructionCost

        protected sealed override EquipmentCategory[] SupportedEquipmentCategories { get { return SupportedEquipCategories; } }

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

        protected override int CalcRefitBenefit() {
            int cumBenefit = base.CalcRefitBenefit();
            cumBenefit += ReqdCmdStat.RefitBenefit;
            cumBenefit += ReqdMRSensorStat.RefitBenefit;
            cumBenefit += FtlDampenerStat.RefitBenefit;
            return cumBenefit;
        }

        /// <summary>
        /// Returns the maximum number of AEquipmentStat slots that this design is allowed for the provided EquipmentCategory.
        /// <remarks>AEquipmentStats that are required for a design are not included. These are typically added via the constructor.</remarks>
        /// </summary>
        /// <param name="equipCat">The equip cat.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override int GetMaxEquipmentSlotsFor(EquipmentCategory equipCat) {
            switch (equipCat) {
                case EquipmentCategory.PassiveCountermeasure:
                    return TempGameValues.MaxCmdPassiveCMs;
                case EquipmentCategory.CommandSensor:
                    return TempGameValues.MaxCmdSensors - Constants.One;
                case EquipmentCategory.ElementSensor:
                case EquipmentCategory.FtlDampener:
                case EquipmentCategory.CommandModule:
                case EquipmentCategory.ShieldGenerator:
                case EquipmentCategory.ActiveCountermeasure:
                case EquipmentCategory.Hull:
                case EquipmentCategory.Propulsion:
                case EquipmentCategory.LosWeapon:
                case EquipmentCategory.LaunchedWeapon:
                case EquipmentCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(equipCat));
            }
        }

        public override bool HasEqualContent(AUnitMemberDesign oDesign) {
            if (base.HasEqualContent(oDesign)) {
                var cmdDesign = oDesign as AUnitCmdDesign;
                return cmdDesign.FtlDampenerStat == FtlDampenerStat && cmdDesign.ReqdMRSensorStat == ReqdMRSensorStat
                    && cmdDesign.ReqdCmdStat == ReqdCmdStat;
            }
            return false;
        }

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

