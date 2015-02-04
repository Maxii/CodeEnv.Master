// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarDataReport.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// 
    /// </summary>
    public struct StarDataReport : IEquatable<StarDataReport> {

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(StarDataReport left, StarDataReport right) {
            return left.Equals(right);
        }

        public static bool operator !=(StarDataReport left, StarDataReport right) {
            return !left.Equals(right);
        }

        #endregion


        public string Name { get; private set; }

        public string ParentName { get; private set; }

        public Player Owner { get; private set; }

        public StarCategory Category { get; private set; }

        public int? Capacity { get; private set; }

        public OpeYield? Resources { get; private set; }

        public XYield? SpecialResources { get; private set; }


        public Player Player { get; private set; }

        public IntelCoverage IntelCoverage { get; private set; }

        public StarDataReport(StarData data, Player player, IntelCoverage intelCoverage)
            : this() {
            Player = player;
            IntelCoverage = intelCoverage;
            SetValues(data);
        }

        private void SetValues(StarData data) {
            switch (IntelCoverage) {
                case IntelCoverage.Comprehensive:
                    SpecialResources = data.SpecialResources;

                    goto case IntelCoverage.Moderate;
                case IntelCoverage.Moderate:
                    Capacity = data.Capacity;
                    Resources = data.Resources;

                    goto case IntelCoverage.Minimal;
                case IntelCoverage.Minimal:
                    Owner = data.Owner;

                    goto case IntelCoverage.Aware;
                case IntelCoverage.Aware:
                    Name = data.Name;
                    ParentName = data.ParentName;
                    Category = data.Category;

                    goto case IntelCoverage.None;
                case IntelCoverage.None:
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(IntelCoverage));
            }
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is StarDataReport)) { return false; }
            return Equals((StarDataReport)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See Page 254, C# 4.0 in a Nutshell.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            int hash = 17;  // 17 = some prime number
            hash = hash * 31 + Name.GetHashCode(); // 31 = another prime number
            hash = hash * 31 + ParentName.GetHashCode();
            hash = hash * 31 + Owner.GetHashCode();
            hash = hash * 31 + Category.GetHashCode();
            hash = hash * 31 + Capacity.GetHashCode();
            hash = hash * 31 + Resources.GetHashCode();
            hash = hash * 31 + SpecialResources.GetHashCode();
            hash = hash * 31 + Player.GetHashCode();
            hash = hash * 31 + IntelCoverage.GetHashCode();
            return hash;
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IEquatable<StarDataReport> Members

        public bool Equals(StarDataReport other) {
            return Name == other.Name && ParentName == other.ParentName && Category == other.Category &&
                Capacity == other.Capacity && Resources == other.Resources && SpecialResources == other.SpecialResources &&
                Player == other.Player && IntelCoverage == other.IntelCoverage;
        }

        #endregion

    }
}

