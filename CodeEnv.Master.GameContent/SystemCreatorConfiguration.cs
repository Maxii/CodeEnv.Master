// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemCreatorConfiguration.cs
// The configuration of the System the Creator is to build and deploy.
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
    /// The configuration of the System the Creator is to build and deploy.
    /// </summary>
    public class SystemCreatorConfiguration {
        //public struct SystemCreatorConfiguration : IEquatable<SystemCreatorConfiguration> {

        //#region Comparison Operators Override

        //// see C# 4.0 In a Nutshell, page 254

        //public static bool operator ==(SystemCreatorConfiguration left, SystemCreatorConfiguration right) {
        //    return left.Equals(right);
        //}

        //public static bool operator !=(SystemCreatorConfiguration left, SystemCreatorConfiguration right) {
        //    return !left.Equals(right);
        //}

        //#endregion

        public string SystemName { get; private set; }

        public string StarDesignName { get; private set; }

        public OrbitData SettlementOrbitSlot { get; private set; }

        public IList<string> PlanetDesignNames { get; private set; }

        public IList<OrbitData> PlanetOrbitSlots { get; private set; }

        public IList<string[]> MoonDesignNames { get; private set; }

        public IList<OrbitData[]> MoonOrbitSlots { get; private set; }

        public bool IsTrackingLabelEnabled { get; private set; }

        public SystemCreatorConfiguration(string systemName, string starDesignName, OrbitData settlementOrbitSlot, IList<string> planetDesignNames,
            IList<OrbitData> planetOrbitSlots, IList<string[]> moonDesignNames, IList<OrbitData[]> moonOrbitSlots, bool enableTrackingLabel) {
            SystemName = systemName;
            StarDesignName = starDesignName;
            SettlementOrbitSlot = settlementOrbitSlot;
            PlanetDesignNames = planetDesignNames;
            PlanetOrbitSlots = planetOrbitSlots;
            MoonDesignNames = moonDesignNames;
            MoonOrbitSlots = moonOrbitSlots;
            IsTrackingLabelEnabled = enableTrackingLabel;
        }

        //#region Object.Equals and GetHashCode Override

        //public override bool Equals(object obj) {
        //    if (!(obj is SystemCreatorConfiguration)) { return false; }
        //    return Equals((SystemCreatorConfiguration)obj);
        //}

        ///// <summary>
        ///// Returns a hash code for this instance.
        ///// See "Page 254, C# 4.0 in a Nutshell."
        ///// </summary>
        ///// <returns>
        ///// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        ///// </returns>
        //public override int GetHashCode() {
        //    unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
        //        int hash = 17;  // 17 = some prime number
        //        hash = hash * 31 + SystemName.GetHashCode();    // 31 = another prime number
        //        hash = hash * 31 + StarDesignName.GetHashCode();
        //        hash = hash * 31 + SettlementOrbitSlot.GetHashCode();
        //        hash = hash * 31 + PlanetDesignNames.GetHashCode();
        //        hash = hash * 31 + MoonDesignNames.GetHashCode();
        //        hash = hash * 31 + IsTrackingLabelEnabled.GetHashCode();
        //        return hash;
        //    }
        //}

        //#endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        //#region IEquatable<SystemCreatorConfiguration> Members

        //public bool Equals(SystemCreatorConfiguration other) {
        //    return SystemName == other.SystemName && StarDesignName == other.StarDesignName && SettlementOrbitSlot == other.SettlementOrbitSlot
        //        && PlanetDesignNames == other.PlanetDesignNames && MoonDesignNames == other.MoonDesignNames
        //        && IsTrackingLabelEnabled == other.IsTrackingLabelEnabled;
        //}

        //#endregion

    }
}

