// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterData.cs
// All the data associated with the UniverseCenter object.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// All the data associated with the UniverseCenter object.
    /// </summary>
    public class UniverseCenterData : AItemData {

        public UniverseCenterData(string name)
            : base(name) {
            base.Topography = Topography.OpenSpace;
        }

        protected override AIntel InitializePlayerIntel() { return new FixedIntel(IntelCoverage.Comprehensive); }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

