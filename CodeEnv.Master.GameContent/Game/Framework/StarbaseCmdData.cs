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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with a StarbaseCmdItem.
    /// </summary>
    public class StarbaseCmdData : AUnitCmdItemData {

        private StarbaseCategory _category;
        public StarbaseCategory Category {
            get { return _category; }
            private set { SetProperty<StarbaseCategory>(ref _category, value, "Category"); }
        }

        public int Capacity { get; private set; }

        public ResourceYield Resources { get; private set; }

        public new FacilityData HQElementData {
            get { return base.HQElementData as FacilityData; }
            set { base.HQElementData = value; }
        }

        private BaseComposition _unitComposition;
        public BaseComposition UnitComposition {
            get { return _unitComposition; }
            set { SetProperty<BaseComposition>(ref _unitComposition, value, "UnitComposition"); }
        }

        private Index3D _sectorIndex;
        public override Index3D SectorIndex { get { return _sectorIndex; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarbaseCmdData" /> class.
        /// </summary>
        /// <param name="cmdTransform">The command transform.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="owner">The owner.</param>
        public StarbaseCmdData(Transform cmdTransform, StarbaseCmdStat stat, Player owner)
            : base(cmdTransform, stat.Name, stat.MaxHitPoints, owner) {
            MaxCmdEffectiveness = stat.MaxCmdEffectiveness;
            UnitFormation = stat.UnitFormation;
            _sectorIndex = References.SectorGrid.GetSectorIndex(Position);
            __PopulateResourcesFromSector();
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

        public StarbaseCategory GenerateCmdCategory(BaseComposition unitComposition) {
            int elementCount = UnitComposition.GetTotalElementsCount();
            D.Log("{0}'s known elements count = {1}.", FullName, elementCount);
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

        // TODO Acquire resource values this starbase has access too, ala SettlementCmdData approach
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

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

