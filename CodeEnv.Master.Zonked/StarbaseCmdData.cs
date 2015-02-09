// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdData.cs
// All the data associated with a StarbaseCmd.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// All the data associated with a StarbaseCmd.
    /// </summary>
    public class StarbaseCmdData : AUnitCmdItemData {

        private StarbaseCategory _category;
        public StarbaseCategory Category {
            get { return _category; }
            private set { SetProperty<StarbaseCategory>(ref _category, value, "Category"); }
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
        /// Initializes a new instance of the <see cref="StarbaseCmdItemData" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public StarbaseCmdItemData(StarbaseCmdStat stat)
            : base(stat.Name, stat.MaxHitPoints) {
            MaxCmdEffectiveness = stat.MaxCmdEffectiveness;
            UnitFormation = stat.UnitFormation;
        }

        public override void AddElement(AUnitElementItemData elementData) {
            base.AddElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public override bool RemoveElement(AUnitElementItemData elementData) {
            bool isRemoved = base.RemoveElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
            return isRemoved;
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

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

