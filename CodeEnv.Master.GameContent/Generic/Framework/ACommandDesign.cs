// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACommandDesign.cs
// Abstract base class holding the design of a command for a player.
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
    /// Abstract base class holding the design of a command for a player.
    /// </summary>
    public abstract class ACommandDesign : AUnitDesign {

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

        protected sealed override EquipmentCategory[] SupportedEquipmentCategories { get { return SupportedEquipCategories; } }

        public ACommandDesign(Player player, FtlDampenerStat ftlDampenerStat, SensorStat reqdMRSensorStat, ACmdModuleStat reqdCmdStat)
            : base(player) {
            FtlDampenerStat = ftlDampenerStat;
            ReqdMRSensorStat = reqdMRSensorStat;
            ReqdCmdStat = reqdCmdStat;
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

        #region Value-based Equality Archive

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + ReqdMRSensorStat.GetHashCode(); // 31 = another prime number
        ////        hash = hash * 31 + FtlDampenerStat.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        ACommandDesign oDesign = (ACommandDesign)obj;
        ////        return oDesign.ReqdMRSensorStat == ReqdMRSensorStat && oDesign.FtlDampenerStat == FtlDampenerStat;
        ////    }
        ////    return false;
        ////}

        #endregion



    }
}

