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

        private static EquipmentCategory[] _supportedEquipCategories =  {
                                                                            EquipmentCategory.PassiveCountermeasure,
                                                                            EquipmentCategory.Sensor
                                                                        };

        public FtlDampenerStat FtlDampenerStat { get; private set; }

        [Obsolete]
        public IEnumerable<SensorStat> SensorStats {
            get {
                var keys = _equipLookupBySlotID.Keys.Where(key => key.Category == EquipmentCategory.Sensor && _equipLookupBySlotID[key] != null);
                IList<SensorStat> stats = new List<SensorStat>();
                foreach (var key in keys) {
                    stats.Add(_equipLookupBySlotID[key] as SensorStat);
                }
                return stats;
            }
        }

        public SensorStat ReqdMRSensorStat { get; private set; }

        protected sealed override EquipmentCategory[] SupportedEquipmentCategories { get { return _supportedEquipCategories; } }

        public ACommandDesign(Player player, FtlDampenerStat ftlDampenerStat, SensorStat reqdMRSensorStat)
            : base(player) {
            FtlDampenerStat = ftlDampenerStat;
            ReqdMRSensorStat = reqdMRSensorStat;
        }

        protected override int GetMaxSlotsFor(EquipmentCategory equipCat) {
            switch (equipCat) {
                case EquipmentCategory.PassiveCountermeasure:
                    return TempGameValues.MaxCmdPassiveCMs;
                case EquipmentCategory.Sensor:
                    return TempGameValues.MaxCmdSensors - Constants.One;
                case EquipmentCategory.FtlDampener:
                    return Constants.One;
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

