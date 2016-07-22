// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarData.cs
// Class for Data associated with a StarItem.
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
    /// Class for Data associated with a StarItem.
    /// </summary>
    public class StarData : AIntelItemData {

        public StarCategory Category { get; private set; }

        public float Radius { get; private set; }

        public float CloseOrbitInnerRadius { get; private set; }

        public float CloseOrbitOuterRadius { get { return CloseOrbitInnerRadius + TempGameValues.ShipCloseOrbitSlotDepth; } }

        private string _parentName;
        public string ParentName {
            get { return _parentName; }
            set { SetProperty<string>(ref _parentName, value, "ParentName"); }
        }

        public override string FullName {
            get { return ParentName.IsNullOrEmpty() ? Name : ParentName + Constants.Underscore + Name; }
        }

        private int _capacity;
        public int Capacity {
            get { return _capacity; }
            set { SetProperty<int>(ref _capacity, value, "Capacity"); }
        }

        private ResourceYield _resources;
        public ResourceYield Resources {
            get { return _resources; }
            set { SetProperty<ResourceYield>(ref _resources, value, "Resources"); }
        }

        public Index3D SectorIndex { get; private set; }

        public new StarInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as StarInfoAccessController; } }

        protected override IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.Basic; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarData" /> class
        /// with the owner initialized to NoPlayer.
        /// </summary>
        /// <param name="star">The star.</param>
        /// <param name="starStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        public StarData(IStar star, StarStat starStat, CameraFocusableStat cameraStat)
            : this(star, starStat, cameraStat, TempGameValues.NoPlayer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarData" /> class.
        /// </summary>
        /// <param name="star">The star.</param>
        /// <param name="starStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="owner">The owner.</param>
        public StarData(IStar star, StarStat starStat, CameraFocusableStat cameraStat, Player owner)
            : base(star, owner, cameraStat) {
            Category = starStat.Category;
            Radius = starStat.Radius;
            CloseOrbitInnerRadius = starStat.CloseOrbitInnerRadius;
            Capacity = starStat.Capacity;
            Resources = starStat.Resources;
            Topography = Topography.System;
            SectorIndex = References.SectorGrid.GetSectorIndex(Position);
        }

        protected override AIntel MakeIntel(IntelCoverage initialcoverage) {
            var intel = new ImprovingIntel();
            intel.InitializeCoverage(initialcoverage);
            return intel;
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new StarInfoAccessController(this);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

