// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterData.cs
// Class for Data associated with the UniverseCenterItem..
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with the UniverseCenterItem..
    /// </summary>
    public class UniverseCenterData : AIntelItemData {

        public float Radius { get; private set; }

        public float LowOrbitRadius { get; private set; }

        public float HighOrbitRadius { get { return LowOrbitRadius + TempGameValues.ShipOrbitSlotDepth; } }

        protected override IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.Basic; } }

        // No SectorIndex as UC is located at the origin at the intersection of 8 sectors

        /// <summary>
        /// Initializes a new instance of the <see cref="UniverseCenterData" /> class.
        /// </summary>
        /// <param name="ucTransform">The uc transform.</param>
        /// <param name="name">The name.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="lowOrbitRadius">The low orbit radius.</param>
        public UniverseCenterData(Transform ucTransform, string name, CameraFocusableStat cameraStat, float radius, float lowOrbitRadius)
            : base(ucTransform, name, TempGameValues.NoPlayer, cameraStat) {
            Radius = radius;
            LowOrbitRadius = lowOrbitRadius;
            Topography = Topography.OpenSpace;
        }

        protected override AIntel MakeIntel(IntelCoverage initialcoverage) {
            var intel = new ImprovingIntel();
            intel.InitializeCoverage(initialcoverage);
            return intel;
        }

        #region Event and Property Change Handlers

        protected override void OwnerPropChangedHandler() {
            throw new System.InvalidOperationException("Illegal attempt by {0} to set Owner: {1}.".Inject(FullName, Owner.LeaderName));
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

