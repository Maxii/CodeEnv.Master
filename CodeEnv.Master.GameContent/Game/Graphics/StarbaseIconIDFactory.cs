// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseIconIDFactory.cs
// Singleton. Factory that makes instances of AIconID, caches and reuses them. 
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
    /// Singleton. Factory that makes instances of AIconID, caches and reuses them. The reuse is critical as 
    /// the object's equality comparer (same instance in memory) is used by the client of the factory
    /// to determine which icon is currently showing.
    /// </summary>
    public class StarbaseIconIDFactory : AIconIDFactory<FleetIconID, StarbaseCmdData, StarbaseIconIDFactory> {

        private StarbaseIconIDFactory() {
            Initialize();
        }

        protected override IconSelectionCriteria GetCriteriaFromCategory(StarbaseCmdData data) {
            switch (data.Category) {
                case StarbaseCategory.Outpost:
                    return IconSelectionCriteria.Level1;
                case StarbaseCategory.LocalBase:
                    return IconSelectionCriteria.Level2;
                case StarbaseCategory.DistrictBase:
                    return IconSelectionCriteria.Level3;
                case StarbaseCategory.RegionalBase:
                    return IconSelectionCriteria.Level4;
                case StarbaseCategory.TerritorialBase:
                    return IconSelectionCriteria.Level5;
                case StarbaseCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(data.Category));
            }
        }

        protected override IEnumerable<IconSelectionCriteria> GetCriteriaFromComposition(StarbaseCmdData data) {
            IList<IconSelectionCriteria> criteria = new List<IconSelectionCriteria>();
            IEnumerable<FacilityCategory> elementCategories = data.UnitComposition.GetUniqueElementCategories();
            if (elementCategories.Contains(FacilityCategory.Science)) {
                criteria.Add(IconSelectionCriteria.Science);
            }
            if (elementCategories.Contains(FacilityCategory.Defense)) {
                criteria.Add(IconSelectionCriteria.Troop);
            }
            if (elementCategories.Contains(FacilityCategory.Economic)) {
                criteria.Add(IconSelectionCriteria.Colony);
            }
            return criteria;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

