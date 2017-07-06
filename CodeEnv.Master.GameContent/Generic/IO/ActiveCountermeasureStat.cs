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

    /// <summary>
    /// Immutable Stat for an active countermeasure.
    /// </summary>
    public class ActiveCountermeasureStat : ARangedEquipmentStat {

        private const string DebugNameFormat = "{0}({1})";

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(ActiveCountermeasureStat left, ActiveCountermeasureStat right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(ActiveCountermeasureStat left, ActiveCountermeasureStat right) {
            return !(left == right);
        }

        #endregion

        public override string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(base.DebugName, InterceptStrengths.Select(intS => intS.Category.GetEnumAttributeText()).Concatenate());
                }
                return _debugName;
            }
        }

        public override EquipmentCategory Category { get { return EquipmentCategory.ActiveCountermeasure; } }

        public WDVStrength[] InterceptStrengths { get; private set; }

        public float InterceptAccuracy { get; private set; }

        public float ReloadPeriod { get; private set; }

        public DamageStrength DamageMitigation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveCountermeasureStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The size.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The PWR RQMT.</param>
        /// <param name="expense">The expense.</param>
        /// <param name="rangeCat">The range cat.</param>
        /// <param name="interceptStrengths">The intercept strengths.</param>
        /// <param name="interceptAccuracy">The intercept accuracy.</param>
        /// <param name="reloadPeriod">The reload period.</param>
        /// <param name="damageMitigation">The damage mitigation.</param>
        public ActiveCountermeasureStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass,
            float pwrRqmt, float expense, RangeCategory rangeCat, WDVStrength[] interceptStrengths, float interceptAccuracy, float reloadPeriod,
            DamageStrength damageMitigation)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense, rangeCat, isDamageable: true) {
            // confirm if more than one interceptStrength, that they each contain a unique WDVCategory
            D.AssertEqual(interceptStrengths.Length, interceptStrengths.Select(intS => intS.Category).Distinct().Count(), "Duplicate Categories found.");
            InterceptStrengths = interceptStrengths;
            InterceptAccuracy = interceptAccuracy;
            ReloadPeriod = reloadPeriod;
            DamageMitigation = damageMitigation;
        }

        #region Object.Equals and GetHashCode Override

        public override int GetHashCode() {
            unchecked {
                int hash = base.GetHashCode();
                foreach (var strength in InterceptStrengths) {
                    hash = hash * 31 + strength.GetHashCode();
                }
                hash = hash * 31 + InterceptAccuracy.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + ReloadPeriod.GetHashCode();
                hash = hash * 31 + DamageMitigation.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) {
            if (base.Equals(obj)) {
                ActiveCountermeasureStat oStat = (ActiveCountermeasureStat)obj;
                return oStat.InterceptStrengths.SequenceEqual(InterceptStrengths) && oStat.InterceptAccuracy == InterceptAccuracy
                    && oStat.ReloadPeriod == ReloadPeriod && oStat.DamageMitigation == DamageMitigation;
            }
            return false;
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

    }
}

