// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ActiveCountermeasureStat.cs
// Immutable Stat for an active countermeasure.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using CodeEnv.Master.Common;
    using Common.LocalResources;
    using System;

    /// <summary>
    /// Immutable Stat for an active countermeasure.
    /// </summary>
    public class ActiveCountermeasureStat : ARangedEquipmentStat {

        private const string DebugNameFormat = "{0}({1})";

        private static RangeCategory GetRangeCat(EquipmentCategory acmCat) {
            switch (acmCat) {
                case EquipmentCategory.MRActiveCountermeasure:
                    return RangeCategory.Medium;
                case EquipmentCategory.SRActiveCountermeasure:
                    return RangeCategory.Short;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(acmCat));
            }
        }

        public override string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(base.DebugName, InterceptStrength.DebugName);
                }
                return _debugName;
            }
        }

        public DamageStrength InterceptStrength { get; private set; }

        public CountermeasureAccuracy InterceptAccy { get; private set; }

        public float ReloadPeriod { get; private set; }

        public DamageStrength DamageMitigation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveCountermeasureStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="size">The size.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The PWR RQMT.</param>
        /// <param name="hitPts">The hit points contributed to the survivability of the item.</param>
        /// <param name="constructionCost">The production cost.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="interceptStrength">The intercept strength.</param>
        /// <param name="interceptAccy">The likelihood of an intercept for each type of ordnance.</param>
        /// <param name="reloadPeriod">The reload period.</param>
        /// <param name="dmgMitigation">The contribution of this equipment to element damage mitigation.</param>
        public ActiveCountermeasureStat(string name, AtlasID imageAtlasID, string imageFilename, string description, EquipmentStatID id,
            float size, float mass, float pwrRqmt, float hitPts, float constructionCost, float expense, DamageStrength interceptStrength,
            CountermeasureAccuracy interceptAccy, float reloadPeriod, DamageStrength dmgMitigation)
            : base(name, imageAtlasID, imageFilename, description, id, size, mass, pwrRqmt, hitPts, constructionCost, expense,
                  GetRangeCat(id.Category), isDamageable: true) {
            InterceptStrength = interceptStrength;
            InterceptAccy = interceptAccy;
            ReloadPeriod = reloadPeriod;
            DamageMitigation = dmgMitigation;
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        [Obsolete]
        private void __ValidateInterceptStrengths(WDVStrength[] interceptStrengths) {
            D.AssertEqual(interceptStrengths.Length, interceptStrengths.Select(intS => intS.Category).Distinct().Count(),
                "Duplicate Categories found.");
        }

        #endregion


        #region ActiveCM Firing Solutions Check Job Archive

        // Note: Removed to allow elimination of ActiveCM CheckFiringSolutionJobs. If an ActiveCM
        // might not be able to engage (bear on) a target, then no firing solutions was a possibility
        // which then required the expensive and numerous check jobs.
        /// <summary>
        /// How frequently this CM can bear on a qualified threat and engage it.
        /// <remarks>Simulates having a hull mount with limited field of fire.</remarks>
        /// </summary>  
        //public float EngagePercent { get; private set; }

        #endregion

        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(ActiveCountermeasureStat left, ActiveCountermeasureStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(ActiveCountermeasureStat left, ActiveCountermeasureStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked {
        ////        int hash = base.GetHashCode();
        ////        foreach (var strength in InterceptStrengths) {
        ////            hash = hash * 31 + strength.GetHashCode();
        ////        }
        ////        hash = hash * 31 + InterceptAccuracy.GetHashCode(); // 31 = another prime number
        ////        hash = hash * 31 + ReloadPeriod.GetHashCode();
        ////        hash = hash * 31 + DamageMitigation.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (base.Equals(obj)) {
        ////        ActiveCountermeasureStat oStat = (ActiveCountermeasureStat)obj;
        ////        return oStat.InterceptStrengths.SequenceEqual(InterceptStrengths) && oStat.InterceptAccuracy == InterceptAccuracy
        ////            && oStat.ReloadPeriod == ReloadPeriod && oStat.DamageMitigation == DamageMitigation;
        ////    }
        ////    return false;
        ////}

        #endregion


    }
}

