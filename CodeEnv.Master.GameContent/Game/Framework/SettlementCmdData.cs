// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdData.cs
// Class for Data associated with a SettlementCmdItem.
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
    /// Class for Data associated with a SettlementCmdItem.
    /// </summary>
    public class SettlementCmdData : AUnitCmdItemData {

        private SettlementCategory _category;
        public SettlementCategory Category {
            get { return _category; }
            private set { SetProperty<SettlementCategory>(ref _category, value, "Category"); }
        }

        private int _population;
        public int Population {
            get { return _population; }
            set {
                SetProperty<int>(ref _population, value, "Population");
            }
        }

        private int _capacityUsed;
        public int CapacityUsed {
            get { return _capacityUsed; }
            set {
                SetProperty<int>(ref _capacityUsed, value, "CapacityUsed");
            }
        }

        private OpeYield _resourcesUsed;
        public OpeYield ResourcesUsed {
            get { return _resourcesUsed; }
            set {
                SetProperty<OpeYield>(ref _resourcesUsed, value, "ResourcesUsed");
            }
        }

        private XYield _specialResourcesUsed;
        public XYield SpecialResourcesUsed {
            get { return _specialResourcesUsed; }
            set {
                SetProperty<XYield>(ref _specialResourcesUsed, value, "SpecialResourcesUsed");
            }
        }

        public new FacilityData HQElementData {
            get { return base.HQElementData as FacilityData; }
            set { base.HQElementData = value; }
        }

        private BaseComposition _unitComposition;
        public BaseComposition UnitComposition {
            get { return _unitComposition; }
            set { SetProperty<BaseComposition>(ref _unitComposition, value, "UnitComposition"); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdData" /> class.
        /// </summary>
        /// <param name="cmdTransform">The command transform.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="owner">The owner.</param>
        public SettlementCmdData(Transform cmdTransform, SettlementCmdStat stat, Player owner)
            : base(cmdTransform, stat.Name, stat.MaxHitPoints, owner) {
            MaxCmdEffectiveness = stat.MaxCmdEffectiveness;
            Population = stat.Population;
            UnitFormation = stat.UnitFormation;
        }

        public override void AddElement(AUnitElementItemData elementData) {
            base.AddElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public override void RemoveElement(AUnitElementItemData elementData) {
            base.RemoveElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        protected override void UpdateComposition() {
            var elementCategories = ElementsData.Cast<FacilityData>().Select(fd => fd.Category);
            UnitComposition = new BaseComposition(elementCategories);
        }

        public SettlementCategory GenerateCmdCategory(BaseComposition unitComposition) {
            int elementCount = UnitComposition.GetTotalElementsCount();
            //D.Log("{0}'s known elements count = {1}.", FullName, elementCount);
            if (elementCount >= 8) {
                return SettlementCategory.Territory;
            }
            if (elementCount >= 6) {
                return SettlementCategory.Province;
            }
            if (elementCount >= 4) {
                return SettlementCategory.CityState;
            }
            if (elementCount >= 2) {
                return SettlementCategory.City;
            }
            if (elementCount >= 1) {
                return SettlementCategory.Colony;
            }
            return SettlementCategory.None;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

