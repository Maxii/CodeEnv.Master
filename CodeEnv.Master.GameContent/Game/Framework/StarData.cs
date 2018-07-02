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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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

        // Star DebugName already includes its System's name

        private int _capacity;
        public int Capacity {
            get { return _capacity; }
            set { SetProperty<int>(ref _capacity, value, "Capacity"); }
        }

        private ResourcesYield _resources;
        public ResourcesYield Resources {
            get { return _resources; }
            set { SetProperty<ResourcesYield>(ref _resources, value, "Resources"); }
        }

        public IntVector3 SectorID { get; private set; }

        public new StarInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as StarInfoAccessController; } }

        private StarPublisher _publisher;
        public StarPublisher Publisher {
            get { return _publisher = _publisher ?? new StarPublisher(this); }
        }

        // No Mass as no Rigidbody

        protected override IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.Basic; } }

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="StarData" /> class
        /// with the owner initialized to NoPlayer.
        /// </summary>
        /// <param name="star">The star.</param>
        /// <param name="starStat">The stat.</param>
        public StarData(IStar star, StarStat starStat)
            : this(star, starStat, TempGameValues.NoPlayer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarData" /> class.
        /// </summary>
        /// <param name="star">The star.</param>
        /// <param name="starStat">The stat.</param>
        /// <param name="owner">The owner.</param>
        public StarData(IStar star, StarStat starStat, Player owner)
            : base(star, owner) {
            Category = starStat.Category;
            Radius = starStat.Radius;
            CloseOrbitInnerRadius = starStat.CloseOrbitInnerRadius;
            Capacity = starStat.Capacity;
            Resources = starStat.Resources;
            Topography = Topography.System;
            ////SectorID = GameReferences.SectorGrid.GetSectorIDThatContains(Position);
            SectorID = GameReferences.SectorGrid.GetSectorIDContaining(Position);
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new StarInfoAccessController(this);
        }

        #endregion

        protected override AIntel MakeIntelInstance() {
            return new NonRegressibleIntel();
        }

        public StarReport GetReport(Player player) { return Publisher.GetReport(player); }


    }
}

