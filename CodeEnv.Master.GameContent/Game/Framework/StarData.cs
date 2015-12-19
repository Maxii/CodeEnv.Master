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

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with a StarItem.
    /// </summary>
    public class StarData : AIntelItemData {

        public StarCategory Category { get; private set; }

        public float Radius { get; private set; }

        public float LowOrbitRadius { get; private set; }

        public float HighOrbitRadius { get { return LowOrbitRadius + TempGameValues.ShipOrbitSlotDepth; } }

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

        protected override IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.Basic; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarData" /> class
        /// with the owner initialized to NoPlayer.
        /// </summary>
        /// <param name="starTransform">The star transform.</param>
        /// <param name="starStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        public StarData(Transform starTransform, StarStat starStat, CameraFocusableStat cameraStat)
            : this(starTransform, starStat, cameraStat, TempGameValues.NoPlayer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarData" /> class.
        /// </summary>
        /// <param name="starTransform">The star transform.</param>
        /// <param name="starStat">The stat.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="owner">The owner.</param>
        public StarData(Transform starTransform, StarStat starStat, CameraFocusableStat cameraStat, Player owner)
            : base(starTransform, starStat.Category.GetValueName(), owner, cameraStat) {
            Category = starStat.Category;
            Radius = starStat.Radius;
            LowOrbitRadius = starStat.LowOrbitRadius;
            Capacity = starStat.Capacity;
            Resources = starStat.Resources;
            Topography = Topography.System;
            SectorIndex = References.SectorGrid.GetSectorIndex(Position);
        }

        //protected sealed override AIntel MakeIntel(IntelCoverage initialcoverage) {
        //    return new ImprovingIntel(initialcoverage);
        //}
        protected override AIntel MakeIntel(IntelCoverage initialcoverage) {
            var intel = new ImprovingIntel();
            intel.InitializeCoverage(initialcoverage);
            return intel;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

