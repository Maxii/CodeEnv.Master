// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitElementDesign.cs
// Abstract design holding the stats of a unit element for a player.
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
    /// Abstract design holding the stats of a unit element for a player.
    /// </summary>
    public abstract class AUnitElementDesign : AUnitMemberDesign {

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

        /// <summary>
        /// The minimum cost in units of production required to refit an Element using this Design.
        /// <remarks>The actual production cost required to refit an Element using this Design is
        /// determined separately. This value is present so the algorithm used won't assign
        /// a refit cost below this minimum. Typically used when refitting an Element to an older
        /// and/or obsolete Design whose cost is significantly less than what the current Element costs.</remarks>
        /// </summary>
        public float MinimumRefitCost { get { return ConstructionCost * TempGameValues.MinRefitConstructionCostFactor; } }

        /// <summary>
        /// The minimum cost in units of production required to disband an Element from this Design.
        /// <remarks>The actual production cost required to disband an Element using this Design is
        /// determined separately. This value is present so the algorithm used won't assign
        /// a disband cost below this minimum.</remarks>
        /// </summary>
        public float MinimumDisbandCost { get { return ConstructionCost * TempGameValues.MinDisbandConstructionCostFactor; } }


        protected sealed override EquipmentCategory[] SupportedEquipmentCategories { get { return SupportedEquipCategories; } }

        public AUnitElementDesign(Player player, Priority hqPriority, SensorStat reqdSRSensorStat)
            : base(player) {
            HQPriority = hqPriority;
            ReqdSRSensorStat = reqdSRSensorStat;
        }

        protected override float CalcConstructionCost() {
            float cumConstructionCost = base.CalcConstructionCost();
            cumConstructionCost += ReqdSRSensorStat.ConstructionCost;
            return cumConstructionCost;
        }

        protected override int CalcRefitBenefit() {
            int cumBenefit = base.CalcRefitBenefit();
            cumBenefit += ReqdSRSensorStat.RefitBenefit;
            return cumBenefit;
        }

        public override bool HasEqualContent(AUnitMemberDesign oDesign) {
            if (base.HasEqualContent(oDesign)) {
                AUnitElementDesign eDesign = oDesign as AUnitElementDesign;
                return eDesign.ReqdSRSensorStat == ReqdSRSensorStat && eDesign.HQPriority == HQPriority;
            }
            return false;
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

        ////public static bool operator ==(AUnitElementDesign left, AUnitElementDesign right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(AUnitElementDesign left, AUnitElementDesign right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + ReqdSRSensorStat.GetHashCode();
        ////        hash = hash * 31 + HQPriority.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        AUnitElementDesign oDesign = (AUnitElementDesign)obj;
        ////        return oDesign.ReqdSRSensorStat == ReqdSRSensorStat && oDesign.HQPriority == HQPriority;
        ////    }
        ////    return false;
        ////}

        #endregion
    }
}

