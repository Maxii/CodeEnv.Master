// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterItemData.cs
// Class for Data associated with the UniverseCenterItem..
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with the UniverseCenterItem..
    /// </summary>
    public class UniverseCenterItemData : AIntelItemData {

        public float Mass { get; private set; }

        public UniverseCenterItemData(Transform ucTransform, string name, float mass)
            : base(ucTransform, name) {
            Mass = mass;
            ucTransform.rigidbody.mass = mass;
            base.Topography = Topography.OpenSpace;
        }

        protected override AIntel InitializeIntelState(Player player) {
            AIntel beginningIntel = new ImprovingIntel();
            beginningIntel.CurrentCoverage = Owner == player ? IntelCoverage.Comprehensive : IntelCoverage.Aware;
            return beginningIntel;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

