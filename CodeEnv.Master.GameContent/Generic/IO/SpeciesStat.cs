// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SpeciesStat.cs
//  Immutable struct containing externally acquirable values for Species.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {
    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable struct containing externally acquirable values for Species.
    /// </summary>
    public struct SpeciesStat : IEquatable<SpeciesStat> {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(SpeciesStat left, SpeciesStat right) {
            return left.Equals(right);
        }

        public static bool operator !=(SpeciesStat left, SpeciesStat right) {
            return !left.Equals(right);
        }

        #endregion

        public Species Species { get; private set; }

        public string Name { get { return Species.GetValueName(); } }

        public string Name_Plural { get; private set; }

        public string Description { get; private set; }

        public AtlasID ImageAtlasID { get; private set; }

        public string ImageFilename { get; private set; }

        public float SensorRangeMultiplier { get; private set; }

        public float WeaponRangeMultiplier { get; private set; }

        public float ActiveCountermeasureRangeMultiplier { get; private set; }

        public float WeaponReloadPeriodMultiplier { get; private set; }

        public float CountermeasureReloadPeriodMultiplier { get; private set; }

        public SpeciesStat(Species species, string pluralName, string description, AtlasID imageAtlasID, string imageFilename,
            float sensorRangeMultiplier, float weaponRangeMultiplier, float activeCountermeasureRangeMultiplier, float weaponReloadPeriodMultiplier, float countermeasureReloadPeriodMultiplier)
            : this() {
            Species = species;
            Name_Plural = pluralName;
            Description = description;
            ImageAtlasID = imageAtlasID;
            ImageFilename = imageFilename;
            SensorRangeMultiplier = sensorRangeMultiplier;
            WeaponRangeMultiplier = weaponRangeMultiplier;
            ActiveCountermeasureRangeMultiplier = activeCountermeasureRangeMultiplier;
            WeaponReloadPeriodMultiplier = weaponReloadPeriodMultiplier;
            CountermeasureReloadPeriodMultiplier = countermeasureReloadPeriodMultiplier;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is SpeciesStat)) { return false; }
            return Equals((SpeciesStat)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See Page 254, C# 4.0 in a Nutshell.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = 17;
                hash = hash * 31 + Name.GetHashCode();
                hash = hash * 31 + Name_Plural.GetHashCode();
                hash = hash * 31 + Description.GetHashCode();
                hash = hash * 31 + ImageAtlasID.GetHashCode();
                hash = hash * 31 + ImageFilename.GetHashCode();
                hash = hash * 31 + SensorRangeMultiplier.GetHashCode();
                hash = hash * 31 + WeaponRangeMultiplier.GetHashCode();
                hash = hash * 31 + ActiveCountermeasureRangeMultiplier.GetHashCode();
                hash = hash * 31 + WeaponReloadPeriodMultiplier.GetHashCode();
                hash = hash * 31 + CountermeasureReloadPeriodMultiplier.GetHashCode();
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IEquatable<SpeciesStat> Members

        public bool Equals(SpeciesStat other) {
            return Name == other.Name && Name_Plural == other.Name_Plural && Description == other.Description && ImageAtlasID == other.ImageAtlasID
                && ImageFilename == other.ImageFilename && SensorRangeMultiplier == other.SensorRangeMultiplier
                && WeaponRangeMultiplier == other.WeaponRangeMultiplier
                && ActiveCountermeasureRangeMultiplier == other.ActiveCountermeasureRangeMultiplier
                && WeaponReloadPeriodMultiplier == other.WeaponReloadPeriodMultiplier
                && CountermeasureReloadPeriodMultiplier == other.CountermeasureReloadPeriodMultiplier;
        }

        #endregion


    }
}

