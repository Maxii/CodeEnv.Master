// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipDesign.cs
// The design of a ship for a player.
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
    /// The design of a ship for a player.
    /// </summary>
    public class ShipDesign : AUnitElementDesign {

        private const string DebugNameFormat = "{0}[{1}], Player = {2}, Hull = {3}, Status = {4}, ConstructionCost = {5:0.}, RefitBenefit = {6}";

        public override string DebugName {
            get {
                string designNameText = DesignName.IsNullOrEmpty() ? "Not yet named" : DesignName;
                return DebugNameFormat.Inject(GetType().Name, designNameText, Player.DebugName, HullCategory.GetValueName(), Status.GetValueName(), ConstructionCost, RefitBenefit);
            }
        }

        public ShipHullCategory HullCategory { get { return HullStat.HullCategory; } }

        public ShipHullStat HullStat { get; private set; }

        public override AtlasID ImageAtlasID { get { return HullStat.ImageAtlasID; } }

        public override string ImageFilename { get { return HullStat.ImageFilename; } }

        public EngineStat StlEngineStat { get; private set; }

        public EngineStat FtlEngineStat { get; private set; }

        public ShipCombatStance CombatStance { get; private set; }

        public ShipDesign(ShipDesign designToCopy)
            : this(designToCopy.Player, designToCopy.HQPriority, designToCopy.ReqdSRSensorStat, designToCopy.HullStat,
                  designToCopy.StlEngineStat, designToCopy.FtlEngineStat, designToCopy.CombatStance) {

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

        public ShipDesign(Player player, Priority hqPriority, SensorStat reqdSRSensorStat, ShipHullStat hullStat, EngineStat stlEngineStat,
            EngineStat ftlEngineStat, ShipCombatStance combatStance)
            : base(player, hqPriority, reqdSRSensorStat) {
            HullStat = hullStat;
            StlEngineStat = stlEngineStat;
            FtlEngineStat = ftlEngineStat;
            CombatStance = combatStance;
            InitializeValuesAndReferences();
        }

        protected override float CalcConstructionCost() {
            float cumConstructionCost = base.CalcConstructionCost();
            cumConstructionCost += HullStat.ConstructionCost;
            cumConstructionCost += FtlEngineStat != null ? FtlEngineStat.ConstructionCost : Constants.ZeroF;
            cumConstructionCost += StlEngineStat.ConstructionCost;
            return cumConstructionCost;
        }

        protected override int CalcRefitBenefit() {
            int cumBenefit = base.CalcRefitBenefit();
            cumBenefit += HullStat.RefitBenefit;
            cumBenefit += FtlEngineStat != null ? FtlEngineStat.RefitBenefit : Constants.Zero;
            cumBenefit += StlEngineStat.RefitBenefit;
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
                var sDesign = oDesign as ShipDesign;
                return sDesign.HullStat == HullStat && sDesign.StlEngineStat == StlEngineStat && sDesign.FtlEngineStat == FtlEngineStat
                    && sDesign.CombatStance == CombatStance;
            }
            return false;
        }

        #region Value-based Equality Archive

        ////public static bool operator ==(ShipDesign left, ShipDesign right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(ShipDesign left, ShipDesign right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        hash = hash * 31 + HullStat.GetHashCode();
        ////        hash = hash * 31 + StlEngineStat.GetHashCode();
        ////        hash = hash * 31 + (FtlEngineStat != null ? FtlEngineStat.GetHashCode() : Constants.Zero);
        ////        hash = hash * 31 + CombatStance.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        ShipDesign oDesign = (ShipDesign)obj;
        ////        return oDesign.HullStat == HullStat && oDesign.StlEngineStat == StlEngineStat && oDesign.FtlEngineStat == FtlEngineStat
        ////            && oDesign.CombatStance == CombatStance;
        ////    }
        ////    return false;
        ////}

        #endregion


    }
}

