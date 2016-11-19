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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// The configuration of the System the Creator is to build and deploy.
    /// </summary>
    public class SystemCreatorConfiguration {

        public string SystemName { get; private set; }

        public string StarDesignName { get; private set; }

        public OrbitData SettlementOrbitSlot { get; private set; }

        /// <summary>
        /// Design name for each planet. 
        /// <remarks>List index indicates which planet.</remarks>
        /// </summary>
        public IList<string> PlanetDesignNames { get; private set; }

        /// <summary>
        /// Orbit slots for each planet. 
        /// <remarks>List index indicates which planet.</remarks>
        /// </summary>
        public IList<OrbitData> PlanetOrbitSlots { get; private set; }

        /// <summary>
        /// Design names for the moons that go with each planet. 
        /// <remarks>List index indicates which planet.</remarks>
        /// </summary>
        public IList<string[]> MoonDesignNames { get; private set; }

        /// <summary>
        /// Orbit slots for the moons that go with each planet. 
        /// <remarks>List index indicates which planet.</remarks>
        /// </summary>
        public IList<OrbitData[]> MoonOrbitSlots { get; private set; }

        public SystemCreatorConfiguration(string systemName, string starDesignName, OrbitData settlementOrbitSlot, IList<string> planetDesignNames,
            IList<OrbitData> planetOrbitSlots, IList<string[]> moonDesignNames, IList<OrbitData[]> moonOrbitSlots) {
            SystemName = systemName;
            StarDesignName = starDesignName;
            SettlementOrbitSlot = settlementOrbitSlot;
            PlanetDesignNames = planetDesignNames;
            PlanetOrbitSlots = planetOrbitSlots;
            MoonDesignNames = moonDesignNames;
            MoonOrbitSlots = moonOrbitSlots;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

