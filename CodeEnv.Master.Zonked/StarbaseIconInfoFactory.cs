// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseIconInfoFactory.cs
// Singleton. Factory that makes instances of IIconInfo for Starbases, caches and reuses them. 
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
    /// Singleton. Factory that makes instances of IIconInfo for Starbases, caches and reuses them. The reuse is critical as 
    /// the object's equality comparer (same instance in memory) is used by the client of the factory to determine which icon is currently showing.
    /// </summary>
    [System.Obsolete]
    public class StarbaseIconInfoFactory : ACmdIconInfoFactory<FleetIconInfo, StarbaseReport, StarbaseIconInfoFactory> {

        protected override AtlasID AtlasID { get { return AtlasID.Fleet; } }

        private StarbaseIconInfoFactory() {
            Initialize();
        }

        protected override IconSelectionCriteria GetCriteriaFromCategory(StarbaseReport starbaseReport) {
            switch (starbaseReport.Category) {
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
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(starbaseReport.Category));
            }
        }

        protected override IEnumerable<IconSelectionCriteria> GetCriteriaFromComposition(StarbaseReport starbaseReport) {
            IList<IconSelectionCriteria> criteria = new List<IconSelectionCriteria>();
            IEnumerable<FacilityCategory> elementCategories = starbaseReport.UnitComposition.GetUniqueElementCategories();
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

