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
    using CodeEnv.Master.Common;
    using Common.LocalResources;

    /// <summary>
    /// The design of a facility for a player.
    /// </summary>
    public class FacilityDesign : AUnitElementDesign {

        private const string DebugNameFormat = "{0}[{1}], Player = {2}, Hull = {3}, Status = {4}, ConstructionCost = {5:0.}, RefitBenefit = {6}";

        public override string DebugName {
            get {
                string designNameText = DesignName.IsNullOrEmpty() ? "Not yet named" : DesignName;
                return DebugNameFormat.Inject(GetType().Name, designNameText, Player.DebugName, HullCategory.GetValueName(),
                    Status.GetValueName(), ConstructionCost, RefitBenefit);
            }
        }

        public FacilityHullCategory HullCategory { get { return HullStat.HullCategory; } }

        public FacilityHullStat HullStat { get; private set; }

        public override AtlasID ImageAtlasID { get { return HullStat.ImageAtlasID; } }

        public override string ImageFilename { get { return HullStat.ImageFilename; } }

        public FacilityDesign(FacilityDesign designToCopy)
            : this(designToCopy.Player, designToCopy.HQPriority, designToCopy.ReqdSRSensorStat, designToCopy.HullStat) {

            EquipmentSlotID slotID;
            AEquipmentStat equipStat;
            while (designToCopy.TryGetNextEquipmentStat(out slotID, out equipStat)) {
                Add(slotID, equipStat);
            }
            AssignPropertyValues();

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

        protected override float CalcConstructionCost() {
            float cumConstructionCost = base.CalcConstructionCost();
            cumConstructionCost += HullStat.ConstructionCost;
            return cumConstructionCost;
        }

        protected override int CalcRefitBenefit() {
            int cumBenefit = base.CalcRefitBenefit();
            cumBenefit += HullStat.RefitBenefit;
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
                    return HullCategory.__MaxPassiveCMs();
                case EquipmentCategory.ActiveCountermeasure:
                    return HullCategory.__MaxActiveCMs();
                case EquipmentCategory.ElementSensor:
                    return HullCategory.__MaxSensors() - Constants.One;
                case EquipmentCategory.ShieldGenerator:
                    return HullCategory.__MaxShieldGenerators();
                case EquipmentCategory.LosWeapon:
                    return HullCategory.__MaxLOSWeapons();
                case EquipmentCategory.LaunchedWeapon:
                    return HullCategory.__MaxLaunchedWeapons();
                case EquipmentCategory.CommandSensor:
                case EquipmentCategory.Propulsion:
                case EquipmentCategory.FtlDampener:
                case EquipmentCategory.Hull:
                case EquipmentCategory.CommandModule:
                case EquipmentCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(equipCat));
            }
        }

        public override bool HasEqualContent(AUnitMemberDesign oDesign) {
            if (base.HasEqualContent(oDesign)) {
                var fDesign = oDesign as FacilityDesign;
                return fDesign.HullStat == HullStat;
            }
            return false;
        }

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

