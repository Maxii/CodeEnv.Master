// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementIconFactory.cs
//  Singleton. Factory that makes instances of IIcon, caches and reuses them.
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

    /// <summary>
    /// Singleton. Factory that makes instances of IIcon, caches and reuses them. The reuse is critical as 
    /// the object's equality comparer (same instance in memory) is used by the client of the factory
    /// to determine which icon is currently showing.
    /// </summary>
    public class SettlementIconFactory : AIconFactory<FleetIcon, SettlementData, SettlementIconFactory> {

        private SettlementIconFactory() {
            Initialize();
        }

        protected override IconSelectionCriteria GetCriteriaFromCategory(SettlementData data) {
            switch (data.Category) {
                case SettlementCategory.Colony:
                    return IconSelectionCriteria.Level1;
                case SettlementCategory.City:
                    return IconSelectionCriteria.Level2;
                case SettlementCategory.CityState:
                    return IconSelectionCriteria.Level3;
                case SettlementCategory.Province:
                    return IconSelectionCriteria.Level4;
                case SettlementCategory.Territory:
                    return IconSelectionCriteria.Level5;
                case SettlementCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(data.Category));
            }
        }

        protected override IEnumerable<IconSelectionCriteria> GetCriteriaFromComposition(SettlementData data) {
            IList<IconSelectionCriteria> criteria = new List<IconSelectionCriteria>();
            IEnumerable<FacilityCategory> shipCategories = data.Composition.Categories;
            if (shipCategories.Contains(FacilityCategory.Science)) {
                criteria.Add(IconSelectionCriteria.Science);
            }
            if (shipCategories.Contains(FacilityCategory.Defense)) {
                criteria.Add(IconSelectionCriteria.Troop);
            }
            if (shipCategories.Contains(FacilityCategory.Economic)) {
                criteria.Add(IconSelectionCriteria.Colony);
            }
            return criteria;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

