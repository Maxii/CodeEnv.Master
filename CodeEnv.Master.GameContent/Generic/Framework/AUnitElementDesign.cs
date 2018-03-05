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

    using Common;

    /// <summary>
    /// Abstract design holding the stats of a unit element for a player.
    /// </summary>
    public abstract class AUnitElementDesign : AUnitMemberDesign {

        public static EquipmentCategory[] SupportedEquipCategories =    {
                                                                            EquipmentCategory.AssaultWeapon,
                                                                            EquipmentCategory.MissileWeapon,
                                                                            EquipmentCategory.BeamWeapon,
                                                                            EquipmentCategory.ProjectileWeapon,
                                                                            EquipmentCategory.ActiveCountermeasure,
                                                                            EquipmentCategory.PassiveCountermeasure,
                                                                            EquipmentCategory.ElementSensor,
                                                                            EquipmentCategory.ShieldGenerator
                                                                        };

        public static EquipmentMountCategory[] SupportedMountCategories =    {
                                                                            EquipmentMountCategory.Silo,
                                                                            EquipmentMountCategory.Turret,
                                                                            EquipmentMountCategory.Interior,
                                                                            EquipmentMountCategory.InteriorAlt
                                                                        };

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

        protected override EquipmentMountCategory[] SupportedHullMountCategories { get { return SupportedMountCategories; } }

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

        public override bool HasEqualContent(AUnitMemberDesign oDesign) {
            if (base.HasEqualContent(oDesign)) {
                AUnitElementDesign eDesign = oDesign as AUnitElementDesign;
                return eDesign.ReqdSRSensorStat == ReqdSRSensorStat && eDesign.HQPriority == HQPriority;
            }
            return false;
        }

        #region Debug

        protected override void __ValidateHullMountCatSequence() {
            base.__ValidateHullMountCatSequence();
            // 3.4.18 Hull prefabs have weapon mount placeholders with manually set slot number assignments. As such,
            // the slot number assignment algorithm used in InitializeValuesAndReferences relies on the proper sequence of
            // HullMountCategories when initializing the SlotIDs for this design. 
            // Silo(Launched)Weapons have slot number assignments that always start with 1, ending with the max number of 
            // Silo(launched)Weapons allowed for the hull category. Turret(Los)Weapons slot numbers follow in sequence, beginning with 
            // the next slot number after the last number used for Silo(Launched)Weapons and ending with a slot number calculated
            // the same way as done for Silo(Launched)Weapons.  
            // 
            // If Loader.__ValidateMaxHullWeaponSlots passes and so does this, slot numbers should be accurate.
            D.AssertEqual(EquipmentMountCategory.Silo, SupportedHullMountCategories[0]);
            D.AssertEqual(EquipmentMountCategory.Turret, SupportedHullMountCategories[1]);
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

