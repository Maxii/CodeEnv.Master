// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdData2.cs
// COMMENT - one line to give a brief idea of what the file does.
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
    /// 
    /// </summary>
    public class StarbaseCmdData2 : ACommandData2 {

        private StarbaseCategory _category;
        public StarbaseCategory Category {
            get { return _category; }
            private set { SetProperty<StarbaseCategory>(ref _category, value, "Category"); }
        }

        public new FacilityData2 HQElementData {
            get { return base.HQElementData as FacilityData2; }
            set { base.HQElementData = value; }
        }

        private BaseComposition _unitComposition;
        public BaseComposition UnitComposition {
            get { return _unitComposition; }
            set { SetProperty<BaseComposition>(ref _unitComposition, value, "UnitComposition"); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarbaseCmdData" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public StarbaseCmdData2(StarbaseCmdStat stat)
            : base(stat.Name, stat.MaxHitPoints) {
            MaxCmdEffectiveness = stat.MaxCmdEffectiveness;
            UnitFormation = stat.UnitFormation;
        }

        public override void AddElement(AElementData2 elementData) {
            base.AddElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public override bool RemoveElement(AElementData2 elementData) {
            bool isRemoved = base.RemoveElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
            return isRemoved;
        }

        protected override void UpdateComposition() {
            var elementCategories = ElementsData.Cast<FacilityData2>().Select(fd => fd.Category);
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

