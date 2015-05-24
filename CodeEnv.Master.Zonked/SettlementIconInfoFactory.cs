// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementIconInfoFactory.cs
// Singleton. Factory that makes instances of IIconInfo for Settlements, caches and reuses them.
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
    /// Singleton. Factory that makes instances of IIconInfo for Settlements, caches and reuses them. The reuse is critical as 
    /// the object's equality comparer (same instance in memory) is used by the client of the factory to determine which icon is currently showing.
    /// </summary>
    [System.Obsolete]
    public class SettlementIconInfoFactory : ACmdIconInfoFactory<FleetIconInfo, SettlementReport, SettlementIconInfoFactory> {

        protected override AtlasID AtlasID { get { return AtlasID.Fleet; } }

        private SettlementIconInfoFactory() {
            Initialize();
        }

        protected override IconSelectionCriteria GetCriteriaFromCategory(SettlementReport settlementReport) {
            switch (settlementReport.Category) {
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
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(settlementReport.Category));
            }
        }

        protected override IEnumerable<IconSelectionCriteria> GetCriteriaFromComposition(SettlementReport settlementReport) {
            IList<IconSelectionCriteria> criteria = new List<IconSelectionCriteria>();
            IEnumerable<FacilityCategory> elementCategories = settlementReport.UnitComposition.GetUniqueElementCategories();
            if (elementCategories.Contains(FacilityCategory.Laboratory)) {
                criteria.Add(IconSelectionCriteria.Science);
            }
            if (elementCategories.Contains(FacilityCategory.Barracks)) {
                criteria.Add(IconSelectionCriteria.Troop);
            }
            if (elementCategories.Contains(FacilityCategory.Colonizer)) {
                criteria.Add(IconSelectionCriteria.Colony);
            }
            return criteria;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

