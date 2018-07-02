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

        protected static SensorStat GetImprovedReqdStat(Player player, SensorStat existingStat) {
            PlayerDesigns designs = GameReferences.GameManager.GetAIManagerFor(player).Designs;
            var currentStat = designs.GetCurrentSRSensorStat();
            return currentStat.Level > existingStat.Level ? currentStat : existingStat;
        }

        public SensorStat ReqdSRSensorStat { get; private set; }

        public abstract Priority HQPriority { get; }

        /// <summary>
        /// The minimum cost in units of production required to disband an Element from this Design.
        /// <remarks>The actual production cost required to disband an Element using this Design is
        /// determined separately. This value is present so the algorithm used won't assign
        /// a disband cost below this minimum.</remarks>
        /// </summary>
        public float MinimumDisbandCost { get { return ConstructionCost * TempGameValues.MinDisbandConstructionCostFactor; } }

        public AUnitElementDesign(Player player, SensorStat reqdSRSensorStat)
            : base(player) {
            ReqdSRSensorStat = reqdSRSensorStat;
        }

        protected override float CalcConstructionCost() {
            D.AssertNotEqual(SourceAndStatus.SystemCreation_Default, Status);   // Elements don't have a free default version
            float cumConstructionCost = base.CalcConstructionCost();
            cumConstructionCost += ReqdSRSensorStat.ConstructCost;
            return cumConstructionCost;
        }

        protected override float CalcHitPoints() {
            float cumHitPts = base.CalcHitPoints();
            cumHitPts += ReqdSRSensorStat.HitPoints;
            return cumHitPts;
        }

        protected override bool IsNonOptionalStatContentEqual(AUnitMemberDesign oDesign) {
            if (base.IsNonOptionalStatContentEqual(oDesign)) {
                AUnitElementDesign eDesign = oDesign as AUnitElementDesign;
                return eDesign.ReqdSRSensorStat == ReqdSRSensorStat && eDesign.HQPriority == HQPriority;
            }
            return false;
        }

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

