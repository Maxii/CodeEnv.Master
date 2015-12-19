// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementIconInfoFactory.cs
// Singleton. Factory that makes instances of IconInfo for Settlements.
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
    /// Singleton. Factory that makes instances of IconInfo for Settlements.
    /// </summary>
    public class SettlementIconInfoFactory : ACmdIconInfoFactory<SettlementReport, SettlementIconInfoFactory> {

        protected override AtlasID AtlasID { get { return AtlasID.Fleet; } }

        private SettlementIconInfoXmlReader _xmlReader;

        private SettlementIconInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() {
            base.Initialize();
            _xmlReader = SettlementIconInfoXmlReader.Instance;
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
            IEnumerable<FacilityHullCategory> elementCategories = settlementReport.UnitComposition.GetUniqueElementCategories();
            if (elementCategories.Contains(FacilityHullCategory.Laboratory)) {
                criteria.Add(IconSelectionCriteria.Science);
            }
            if (elementCategories.Contains(FacilityHullCategory.Barracks)) {
                criteria.Add(IconSelectionCriteria.Troop);
            }
            if (elementCategories.Contains(FacilityHullCategory.ColonyHab)) {
                criteria.Add(IconSelectionCriteria.Colony);
            }
            return criteria;
        }

        protected override string AcquireFilename(IconSection section, params IconSelectionCriteria[] criteria) {
            return _xmlReader.AcquireFilename(section, criteria);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Nested Classes

        public class SettlementIconInfoXmlReader : ACmdIconInfoXmlReader<SettlementIconInfoXmlReader> {

            protected override string XmlFilename { get { return "FleetIconInfo"; } }   //TODO

            private SettlementIconInfoXmlReader() {
                Initialize();
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }

        }

        #endregion

    }
}


