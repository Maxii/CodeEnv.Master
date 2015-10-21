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

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
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
            set { SetProperty<int>(ref _population, value, "Population"); }
        }

        public int Capacity { get { return SystemData.Capacity; } } // TODO need SetProperty to properly keep isChanged updated?

        public ResourceYield Resources { get { return SystemData.Resources; } } // TODO need SetProperty to properly keep isChanged updated?

        private float _approval;
        public float Approval {
            get { return _approval; }
            set { SetProperty<float>(ref _approval, value, "Approval", OnApprovalChanged); }
        }

        public SystemData SystemData { private get; set; }

        public new FacilityData HQElementData {
            get { return base.HQElementData as FacilityData; }
            set { base.HQElementData = value; }
        }

        private BaseComposition _unitComposition;
        public BaseComposition UnitComposition {
            get { return _unitComposition; }
            set { SetProperty<BaseComposition>(ref _unitComposition, value, "UnitComposition"); }
        }

        public override Index3D SectorIndex { get { return References.SectorGrid.GetSectorIndex(Position); } }   // Settlements get relocated

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdData"/> class
        /// with no passive countermeasures.
        /// </summary>
        /// <param name="cmdTransform">The command transform.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="owner">The owner.</param>
        public SettlementCmdData(Transform cmdTransform, SettlementCmdStat stat, Player owner)
            : this(cmdTransform, stat, owner, Enumerable.Empty<PassiveCountermeasure>()) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdData"/> class.
        /// </summary>
        /// <param name="cmdTransform">The command transform.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        public SettlementCmdData(Transform cmdTransform, SettlementCmdStat stat, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs)
            : base(cmdTransform, stat.Name, stat.MaxHitPoints, owner, passiveCMs) {
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

        protected override void RefreshComposition() {
            var elementCategories = ElementsData.Cast<FacilityData>().Select(fd => fd.HullCategory);
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

        private void OnApprovalChanged() {
            Arguments.ValidateForRange(Approval, Constants.ZeroPercent, Constants.OneHundredPercent);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

