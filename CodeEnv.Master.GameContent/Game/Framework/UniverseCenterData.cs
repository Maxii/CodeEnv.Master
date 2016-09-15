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
    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with the UniverseCenterItem..
    /// </summary>
    public class UniverseCenterData : AIntelItemData {

        public float Radius { get; private set; }

        public float CloseOrbitInnerRadius { get; private set; }

        public float CloseOrbitOuterRadius { get { return CloseOrbitInnerRadius + TempGameValues.ShipCloseOrbitSlotDepth; } }

        public new UniverseCenterInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as UniverseCenterInfoAccessController; } }

        protected override IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.Basic; } }

        // No Mass as no Rigidbody
        // No SectorIndex as UC is located at the origin at the intersection of 8 sectors

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="UniverseCenterData" /> class.
        /// </summary>
        /// <param name="ucenter">The uCenter.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="closeOrbitInnerRadius">The close orbit inner radius.</param>
        public UniverseCenterData(IUniverseCenter ucenter, float radius, float closeOrbitInnerRadius)
            : base(ucenter, TempGameValues.NoPlayer) {
            Radius = radius;
            CloseOrbitInnerRadius = closeOrbitInnerRadius;
            Topography = Topography.OpenSpace;
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new UniverseCenterInfoAccessController(this);
        }

        #endregion

        protected override AIntel MakeIntel(IntelCoverage initialcoverage) {
            var intel = new ImprovingIntel();
            intel.InitializeCoverage(initialcoverage);
            return intel;
        }

        #region Event and Property Change Handlers

        protected override void HandleOwnerChanged() {
            throw new System.InvalidOperationException("Illegal attempt by {0} to set Owner: {1}.".Inject(FullName, Owner.LeaderName));
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

