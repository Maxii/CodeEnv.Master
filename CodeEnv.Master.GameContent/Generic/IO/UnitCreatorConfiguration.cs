// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitCreatorConfiguration.cs
// The configuration of the Unit the Creator is to build and deploy.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// The configuration of the Unit the Creator is to build and deploy.
    /// </summary>
    public struct UnitCreatorConfiguration : IEquatable<UnitCreatorConfiguration> {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(UnitCreatorConfiguration left, UnitCreatorConfiguration right) {
            return left.Equals(right);
        }

        public static bool operator !=(UnitCreatorConfiguration left, UnitCreatorConfiguration right) {
            return !left.Equals(right);
        }

        #endregion

        public string UnitName { get; private set; }

        public Player Owner { get; private set; }

        public GameDate DeployDate { get; private set; }

        public string CmdDesignName { get; private set; }

        public IEnumerable<string> ElementDesignNames { get; private set; }

        public bool IsTrackingLabelEnabled { get; private set; }

        public UnitCreatorConfiguration(string unitName, Player owner, GameDate deployDate, string cmdDesignName, IEnumerable<string> elementDesignNames, bool enableTrackingLabel) {
            UnitName = unitName;
            Owner = owner;
            DeployDate = deployDate;
            CmdDesignName = cmdDesignName;
            ElementDesignNames = elementDesignNames;
            IsTrackingLabelEnabled = enableTrackingLabel;
            ValidateDeployDate();
        }

        private void ValidateDeployDate() {
            GameDate earliestDate;
            if (!References.GameManager.IsRunning) {
                earliestDate = GameTime.GameStartDate;
            }
            else {
                earliestDate = GameTime.Instance.CurrentDate;
            }
            D.Assert(DeployDate >= earliestDate, "{0}.DeployDate {1} < {2}!", CmdDesignName, DeployDate, earliestDate);
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is UnitCreatorConfiguration)) { return false; }
            return Equals((UnitCreatorConfiguration)obj);
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
                hash = hash * 31 + UnitName.GetHashCode();  // 31 = another prime number
                hash = hash * 31 + Owner.GetHashCode();
                hash = hash * 31 + DeployDate.GetHashCode();
                hash = hash * 31 + CmdDesignName.GetHashCode();
                hash = hash * 31 + ElementDesignNames.GetHashCode();
                hash = hash * 31 + IsTrackingLabelEnabled.GetHashCode();
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IEquatable<UnitCreatorConfiguration> Members

        public bool Equals(UnitCreatorConfiguration other) {
            return UnitName == other.UnitName && Owner == other.Owner && DeployDate == other.DeployDate && CmdDesignName == other.CmdDesignName
                && ElementDesignNames == other.ElementDesignNames && IsTrackingLabelEnabled == other.IsTrackingLabelEnabled;
        }

        #endregion

    }
}

