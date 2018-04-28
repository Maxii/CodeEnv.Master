// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HeroStat.cs
// Stat for Heroes that can be deployed to UnitCmds to improve CmdEffectiveness.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using Common;

    /// <summary>
    /// Stat for Heroes that can be deployed to UnitCmds to improve CmdEffectiveness.
    /// </summary>
    public struct HeroStat : IEquatable<HeroStat> {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(HeroStat left, HeroStat right) {
            return left.Equals(right);
        }

        public static bool operator !=(HeroStat left, HeroStat right) {
            return !left.Equals(right);
        }

        #endregion

        public string DebugName { get { return GetType().Name; } }

        public string Name { get; private set; }

        public AtlasID ImageAtlasID { get; private set; }

        public string ImageFilename { get; private set; }

        public string Description { get; private set; }

        public Species Species { get; private set; }

        public Hero.HeroCategory Category { get; private set; }

        public float StartingCmdEffectiveness { get; private set; }

        public float StartingExperience { get; private set; }

        public HeroStat(string name, AtlasID imageAtlasID, string imageFilename, Species species, Hero.HeroCategory category,
            string description, float startingCmdEffectiveness, float startingExperience) : this() {
            Utility.ValidateForRange(startingCmdEffectiveness, __startingCmdEffectivenessRange);
            Name = name;
            ImageAtlasID = imageAtlasID;
            ImageFilename = imageFilename;
            Species = species;
            Category = category;
            Description = description;
            StartingCmdEffectiveness = startingCmdEffectiveness;
            StartingExperience = startingExperience;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is HeroStat)) { return false; }
            return Equals((HeroStat)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See "Page 254, C# 4.0 in a Nutshell."
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = 17;  // 17 = some prime number
                hash = hash * 31 + Name.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + ImageAtlasID.GetHashCode();
                hash = hash * 31 + ImageFilename.GetHashCode();
                hash = hash * 31 + Species.GetHashCode();
                hash = hash * 31 + Category.GetHashCode();
                hash = hash * 31 + Description.GetHashCode();
                hash = hash * 31 + StartingCmdEffectiveness.GetHashCode();
                hash = hash * 31 + StartingExperience.GetHashCode();
                return hash;
            }
        }

        #endregion


        public override string ToString() {
            return DebugName;
        }

        #region Debug

        private static ValueRange<float> __startingCmdEffectivenessRange = new ValueRange<float>(Constants.ZeroPercent, Constants.OneHundredPercent);

        #endregion

        #region IEquatable<HeroStat> Members

        public bool Equals(HeroStat other) {
            return Name == other.Name && ImageAtlasID == other.ImageAtlasID && ImageFilename == other.ImageFilename
                && Species == other.Species && Category == other.Category && Description == other.Description
                && StartingCmdEffectiveness == other.StartingCmdEffectiveness && StartingExperience == other.StartingExperience;
        }

        #endregion

    }
}

