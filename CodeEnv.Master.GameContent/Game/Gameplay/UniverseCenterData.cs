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

        public OrbitalSlot ShipOrbitSlot { get; set; }

        public override SpaceTopography Topography {
            get { return base.Topography; }
            set { throw new NotImplementedException(); }
        }

        public UniverseCenterData(string name)
            : base(name) {
            base.Topography = SpaceTopography.OpenSpace;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

