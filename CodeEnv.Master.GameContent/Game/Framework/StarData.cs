﻿// --------------------------------------------------------------------------------------------------------------------
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

        private OpeYield _resources;
        public OpeYield Resources {
            get { return _resources; }
            set { SetProperty<OpeYield>(ref _resources, value, "Resources"); }
        }

        private XYield _specialResources;
        public XYield SpecialResources {
            get { return _specialResources; }
            set { SetProperty<XYield>(ref _specialResources, value, "SpecialResources"); }
        }

        public float Mass { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarData" /> class
        /// with the owner initialized to NoPlayer.
        /// </summary>
        /// <param name="starTransform">The star transform.</param>
        /// <param name="stat">The stat.</param>
        public StarData(Transform starTransform, StarStat stat)
            : this(starTransform, stat, TempGameValues.NoPlayer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarData" /> class.
        /// </summary>
        /// <param name="starTransform">The star transform.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="owner">The owner.</param>
        public StarData(Transform starTransform, StarStat stat, Player owner)
            : base(starTransform, stat.Category.GetName(), owner) {
            Category = stat.Category;
            Capacity = stat.Capacity;
            Mass = stat.Mass;
            starTransform.rigidbody.mass = stat.Mass;
            Resources = stat.Resources;
            SpecialResources = stat.SpecialResources;
            base.Topography = Topography.System;
        }

        protected override AIntel InitializeIntelState(Player player) {
            var coverage = IsAllIntelCoverageComprehensive || Owner == player ? IntelCoverage.Comprehensive : IntelCoverage.Aware;
            return new ImprovingIntel(coverage);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

