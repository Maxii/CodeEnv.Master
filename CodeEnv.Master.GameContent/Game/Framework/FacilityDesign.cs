// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityDesign.cs
// The design of a facility for a player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using Common.LocalResources;

    /// <summary>
    /// The design of a facility for a player.
    /// </summary>
    public class FacilityDesign : AUnitElementDesign {

        private const string DebugNameFormat = "{0}[{1}], Player = {2}, Hull = {3}, Status = {4}, ConstructionCost = {5:0.}, DesignLevel = {6}";

        private static OptionalEquipMountCategory[] SupportedMountCategories =  {
                                                                                    OptionalEquipMountCategory.Silo,
                                                                                    OptionalEquipMountCategory.Turret,
                                                                                    OptionalEquipMountCategory.Sensor,
                                                                                    OptionalEquipMountCategory.Skin,
                                                                                    OptionalEquipMountCategory.Screen,
                                                                                    OptionalEquipMountCategory.Flex
                                                                                };

        static FacilityDesign() {
            __ValidateWeaponHullMountCatSequence();
            __ValidateSupportedMountsCanAccommodateSupportedEquipment();
        }

        private static FacilityHullStat GetImprovedReqdStat(Player player, FacilityHullStat existingStat) {
            PlayerDesigns designs = GameReferences.GameManager.GetAIManagerFor(player).Designs;
            FacilityHullCategory hullCat = existingStat.HullCategory;
            FacilityHullStat currentStat;
            if (designs.TryGetCurrentHullStat(hullCat, out currentStat)) {
                if (currentStat.Level > existingStat.Level) {
                    return currentStat;
                }
            }
            return existingStat;
        }

        public override string DebugName {
            get {
                string designNameText = DesignName.IsNullOrEmpty() ? "Not yet named" : DesignName;
                return DebugNameFormat.Inject(GetType().Name, designNameText, Player.DebugName, HullCategory.GetValueName(),
                    Status.GetValueName(), ConstructionCost, DesignLevel);
            }
        }

        public FacilityHullCategory HullCategory { get { return HullStat.HullCategory; } }

        public override Priority HQPriority { get { return HullStat.HqPriority; } }

        public FacilityHullStat HullStat { get; private set; }

        public override AtlasID ImageAtlasID { get { return HullStat.ImageAtlasID; } }

        public override string ImageFilename { get { return HullStat.ImageFilename; } }

        protected override OptionalEquipMountCategory[] SupportedOptionalMountCategories { get { return SupportedMountCategories; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityDesign"/> class.
        /// <remarks>This version automatically improves any Reqd EquipmentStats (including the HullStat) to the highest Level available,
        /// and copies the rest of the content of the design into the new design instance, allowing the player to upgrade and/or change 
        /// the mix of optional EquipmentStats.</remarks>
        /// </summary>
        /// <param name="designToImprove">The design to improve.</param>
        public FacilityDesign(FacilityDesign designToImprove)
            : this(designToImprove.Player, GetImprovedReqdStat(designToImprove.Player, designToImprove.ReqdSRSensorStat),
                  GetImprovedReqdStat(designToImprove.Player, designToImprove.HullStat)) {

            OptionalEquipSlotID slotID;
            AEquipmentStat equipStat;
            while (designToImprove.TryGetNextOptEquipStat(out slotID, out equipStat)) {
                Add(slotID, equipStat);
            }
            AssignPropertyValues();

            RootDesignName = designToImprove.RootDesignName;
            // If copying System_CreationTemplate counter will always = 0 as they are never incremented. If copying Player_Current counter 
            // will be >= 0 ready to be incremented. If copying Player_Obsolete a new RootDesignName will be assigned resetting counter
            // to 0 to avoid creating duplicate design names when incrementing.
            DesignLevel = designToImprove.DesignLevel;
        }

        public FacilityDesign(Player player, SensorStat reqdSRSensorStat, FacilityHullStat hullStat)
            : base(player, reqdSRSensorStat) {
            HullStat = hullStat;
            InitializeValuesAndReferences();
        }

        protected override float CalcConstructionCost() {
            float cumConstructionCost = base.CalcConstructionCost();
            cumConstructionCost += HullStat.ConstructCost;
            return cumConstructionCost;
        }

        protected override float CalcHitPoints() {
            float cumHitPts = base.CalcHitPoints();
            cumHitPts += HullStat.HitPoints;
            return cumHitPts;
        }

        /// <summary>
        /// Returns the maximum number of slots for optional equipment that this design is allowed for the provided OptionalEquipMountCategory.
        /// <remarks>Equipment that is required for a design is not included as they don't require slots.</remarks>
        /// </summary>
        /// <param name="mountCat">The OptionalEquipMountCategory.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override int GetMaxOptionalEquipSlotsFor(OptionalEquipMountCategory mountCat) {
            switch (mountCat) {
                case OptionalEquipMountCategory.Turret:
                    return HullCategory.MaxTurretMounts();
                case OptionalEquipMountCategory.Silo:
                    return HullCategory.MaxSiloMounts();
                case OptionalEquipMountCategory.Sensor:
                    return HullCategory.__MaxSensorMounts();
                case OptionalEquipMountCategory.Skin:
                    return HullCategory.__MaxSkinMounts();
                case OptionalEquipMountCategory.Screen:
                    return HullCategory.__MaxScreenMounts();
                case OptionalEquipMountCategory.Flex:
                    return HullCategory.__MaxFlexMounts();
                case OptionalEquipMountCategory.FtlEngine:
                case OptionalEquipMountCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mountCat));
            }
        }

        protected override bool IsNonStatContentEqual(AUnitMemberDesign oDesign) {
            if (base.IsNonStatContentEqual(oDesign)) {
                var fDesign = oDesign as FacilityDesign;
                return fDesign.HullStat == HullStat;
            }
            return false;
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private static void __ValidateWeaponHullMountCatSequence() {
            // 3.4.18 Hull prefabs have weapon mount placeholders with manually set slot number assignments. As such,
            // the slot number assignment algorithm used in InitializeValuesAndReferences relies on the proper sequence of
            // HullMountCategories when initializing the SlotIDs for this design. 
            // Silo(Launched)Weapons have slot number assignments that always start with 1, ending with the max number of 
            // Silo(launched)Weapons allowed for the hull category. Turret(Los)Weapons slot numbers follow in sequence, beginning with 
            // the next slot number after the last number used for Silo(Launched)Weapons and ending with a slot number calculated
            // the same way as done for Silo(Launched)Weapons.  
            // 
            // If Loader.__ValidateMaxHullWeaponSlots passes and so does this, slot numbers should be accurate.
            D.AssertEqual(OptionalEquipMountCategory.Silo, SupportedMountCategories[0]);
            D.AssertEqual(OptionalEquipMountCategory.Turret, SupportedMountCategories[1]);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void __ValidateSupportedMountsCanAccommodateSupportedEquipment() {
            IEnumerable<EquipmentCategory> equipmentSupportedByMounts = new List<EquipmentCategory>();
            foreach (var mount in SupportedMountCategories) {
                equipmentSupportedByMounts = equipmentSupportedByMounts.Union(mount.SupportedEquipment());
            }
            TempGameValues.EquipCatsSupportedByFacilityDesigner.ForAll(se => D.Assert(equipmentSupportedByMounts.Contains(se)));
        }

        #endregion


        #region Value-based Equality Archive

        ////public static bool operator ==(FacilityDesign left, FacilityDesign right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(FacilityDesign left, FacilityDesign right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + HullStat.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        FacilityDesign oDesign = (FacilityDesign)obj;
        ////        return oDesign.HullStat == HullStat;
        ////    }
        ////    return false;
        ////}

        #endregion



    }
}

