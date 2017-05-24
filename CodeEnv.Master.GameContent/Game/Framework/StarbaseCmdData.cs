// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdData.cs
// Class for Data associated with a StarbaseCmdItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with a StarbaseCmdItem.
    /// </summary>
    public class StarbaseCmdData : AUnitBaseCmdData {

        private StarbaseCategory _category;
        public StarbaseCategory Category {
            get { return _category; }
            private set { SetProperty<StarbaseCategory>(ref _category, value, "Category"); }
        }

        private int _capacity;
        public int Capacity {
            get { return _capacity; }
            private set { SetProperty<int>(ref _capacity, value, "Capacity"); }
        }

        private ResourceYield _resources;
        public ResourceYield Resources {
            get { return _resources; }
            private set { SetProperty<ResourceYield>(ref _resources, value, "Resources"); }
        }

        public new StarbaseInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as StarbaseInfoAccessController; } }

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="StarbaseCmdData" /> class
        /// with no passive countermeasures.
        /// </summary>
        /// <param name="starbaseCmd">The starbase command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="sensors">The MR and LR sensors for this UnitCmd.</param>
        /// <param name="ftlDampener">The FTL dampener.</param>
        /// <param name="cmdStat">The stat.</param>
        public StarbaseCmdData(IStarbaseCmd starbaseCmd, Player owner, IEnumerable<CmdSensor> sensors, FtlDampener ftlDampener, UnitCmdStat cmdStat)
            : this(starbaseCmd, owner, Enumerable.Empty<PassiveCountermeasure>(), sensors, ftlDampener, cmdStat) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarbaseCmdData" /> class.
        /// </summary>
        /// <param name="starbaseCmd">The starbase command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="sensors">The MR and LR sensors for this UnitCmd.</param>
        /// <param name="ftlDampener">The FTL dampener.</param>
        /// <param name="cmdStat">The stat.</param>
        public StarbaseCmdData(IStarbaseCmd starbaseCmd, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, IEnumerable<CmdSensor> sensors, FtlDampener ftlDampener, UnitCmdStat cmdStat)
            : base(starbaseCmd, owner, passiveCMs, sensors, ftlDampener, cmdStat) {
            __PopulateResourcesFromSector();
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new StarbaseInfoAccessController(this);
        }

        #endregion

        public override void AddElement(AUnitElementData elementData) {
            base.AddElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public override void RemoveElement(AUnitElementData elementData) {
            base.RemoveElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public StarbaseCategory GenerateCmdCategory(BaseComposition unitComposition) {
            int elementCount = unitComposition.GetTotalElementsCount();
            //D.Log(ShowDebugLog, "{0}'s known elements count = {1}.", DebugName, elementCount);
            if (elementCount >= 8) {
                return StarbaseCategory.TerritorialBase;
            }
            if (elementCount >= 6) {
                return StarbaseCategory.RegionalBase;
            }
            if (elementCount >= 4) {
                return StarbaseCategory.DistrictBase;
            }
            if (elementCount >= 2) {
                return StarbaseCategory.LocalBase;
            }
            if (elementCount >= 1) {
                return StarbaseCategory.Outpost;
            }
            return StarbaseCategory.None;
        }

        #region Event and Property Change Handlers

        #endregion

        //TODO Acquire resource values this starbase has access too, ala SettlementCmdData approach
        private void __PopulateResourcesFromSector() {
            Capacity = 10;
            var resources = new ResourceYield.ResourceValuePair[] {
                new ResourceYield.ResourceValuePair(ResourceID.Organics, 0.3F),
                new ResourceYield.ResourceValuePair(ResourceID.Particulates, 0.5F),
                new ResourceYield.ResourceValuePair(ResourceID.Energy, 1.2F),
                new ResourceYield.ResourceValuePair(ResourceID.Titanium, 0.5F),
                new ResourceYield.ResourceValuePair(ResourceID.Duranium, 1.1F),
                new ResourceYield.ResourceValuePair(ResourceID.Unobtanium, 0.1F)
            };
            Resources = new ResourceYield(resources);
        }

    }
}

