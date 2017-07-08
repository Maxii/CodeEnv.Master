// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AElementDesign.cs
// Abstract base class holding the design of an element for a player.
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common;

    /// <summary>
    /// Abstract base class holding the design of an element for a player.
    /// </summary>
    public abstract class AElementDesign : AUnitDesign {

        public static EquipmentCategory[] SupportedEquipCategories =  {
                                                                            EquipmentCategory.LaunchedWeapon,
                                                                            EquipmentCategory.LosWeapon,
                                                                            EquipmentCategory.ActiveCountermeasure,
                                                                            EquipmentCategory.PassiveCountermeasure,
                                                                            EquipmentCategory.ElementSensor,
                                                                            EquipmentCategory.ShieldGenerator
                                                                        };

        [Obsolete]
        public IEnumerable<ActiveCountermeasureStat> ActiveCmStats {
            get {
                var keys = _equipLookupBySlotID.Keys.Where(key => key.Category == EquipmentCategory.ActiveCountermeasure
                && _equipLookupBySlotID[key] != null);
                IList<ActiveCountermeasureStat> stats = new List<ActiveCountermeasureStat>();
                foreach (var key in keys) {
                    stats.Add(_equipLookupBySlotID[key] as ActiveCountermeasureStat);
                }
                return stats;
            }
        }

        [Obsolete]
        public IEnumerable<SensorStat> SensorStats {
            get {
                var keys = _equipLookupBySlotID.Keys.Where(key => key.Category == EquipmentCategory.ElementSensor
                && _equipLookupBySlotID[key] != null);
                IList<SensorStat> stats = new List<SensorStat>();
                foreach (var key in keys) {
                    stats.Add(_equipLookupBySlotID[key] as SensorStat);
                }
                return stats;
            }
        }

        [Obsolete]
        public IEnumerable<ShieldGeneratorStat> ShieldGeneratorStats {
            get {
                var keys = _equipLookupBySlotID.Keys.Where(key => key.Category == EquipmentCategory.ShieldGenerator
                && _equipLookupBySlotID[key] != null);
                IList<ShieldGeneratorStat> stats = new List<ShieldGeneratorStat>();
                foreach (var key in keys) {
                    stats.Add(_equipLookupBySlotID[key] as ShieldGeneratorStat);
                }
                return stats;
            }
        }

        public SensorStat ReqdSRSensorStat { get; private set; }

        public Priority HQPriority { get; private set; }

        protected sealed override EquipmentCategory[] SupportedEquipmentCategories { get { return SupportedEquipCategories; } }

        public AElementDesign(Player player, Priority hqPriority, SensorStat reqdSRSensorStat)
            : base(player) {
            HQPriority = hqPriority;
            ReqdSRSensorStat = reqdSRSensorStat;
        }

        #region Debug

        protected override void __ValidateEquipmentCategorySequence() {
            base.__ValidateEquipmentCategorySequence();
            // 6.29.17 Hull prefabs have weapon mount placeholders with manually set slot number assignments. As such,
            // the slot number assignment algorithm used in InitializeValuesAndReferences relies on the proper sequence of
            // EquipmentCategories when initializing the SlotIDs for this design. 
            // LaunchedWeapons have slot number assignments that always start with 1, ending with the max number of 
            // launchedWeapons allowed for the hull category. LosWeapons slot numbers follow in sequence, beginning with 
            // the next slot number after the last number used for LaunchedWeapons and ending with a slot number calculated
            // the same way as done for LaunchedWeapons.  
            // 
            // If Loader.__ValidateMaxHullWeaponSlots passes and so does this, slot numbers should be accurate.
            D.AssertEqual(EquipmentCategory.LaunchedWeapon, SupportedEquipmentCategories[0]);
            D.AssertEqual(EquipmentCategory.LosWeapon, SupportedEquipmentCategories[1]);
        }

        #endregion

        #region Value-based Equality Archive

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + ReqdSRSensorStat.GetHashCode(); // 31 = another prime number
        ////        hash = hash * 31 + HQPriority.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        AElementDesign oDesign = (AElementDesign)obj;
        ////        return oDesign.ReqdSRSensorStat == ReqdSRSensorStat && oDesign.HQPriority == HQPriority;
        ////    }
        ////    return false;
        ////}

        #endregion
    }
}

