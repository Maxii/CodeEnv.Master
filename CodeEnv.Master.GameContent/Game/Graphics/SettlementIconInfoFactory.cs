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
    public class SettlementIconInfoFactory : ACmdIconInfoFactory<SettlementCmdReport, SettlementIconInfoFactory> {

        protected override AtlasID AtlasID { get { return AtlasID.Fleet; } }

        private SettlementIconInfoXmlReader _xmlReader;

        private SettlementIconInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() {
            base.Initialize();
            _xmlReader = SettlementIconInfoXmlReader.Instance;
        }

        protected override IconSelectionCriteria[] GetSelectionCriteria(SettlementCmdReport userRqstdCmdReport) {
            if (userRqstdCmdReport.IntelCoverage == IntelCoverage.None) {
                // Reports are rqstd when an element/cmd loses all IntelCoverage and the Cmd re-evaluates its icon
                return new IconSelectionCriteria[] { IconSelectionCriteria.None };
            }

            if (userRqstdCmdReport.Category == SettlementCategory.None) {
                D.Assert(userRqstdCmdReport.UnitComposition == null); // UnitComposition should not be known if Category isn't known
                                                                      // User has no permission to know category so return unknown
                return new IconSelectionCriteria[] { IconSelectionCriteria.Unknown };
            }

            IList<IconSelectionCriteria> criteria = new List<IconSelectionCriteria>();
            switch (userRqstdCmdReport.Category) {
                case SettlementCategory.Colony:
                    criteria.Add(IconSelectionCriteria.Level1);
                    break;
                case SettlementCategory.City:
                    criteria.Add(IconSelectionCriteria.Level2);
                    break;
                case SettlementCategory.CityState:
                    criteria.Add(IconSelectionCriteria.Level3);
                    break;
                case SettlementCategory.Province:
                    criteria.Add(IconSelectionCriteria.Level4);
                    break;
                case SettlementCategory.Territory:
                    criteria.Add(IconSelectionCriteria.Level5);
                    break;
                case SettlementCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(userRqstdCmdReport.Category));
            }

            if (userRqstdCmdReport.UnitComposition != null) {    // check for access rights
                IEnumerable<FacilityHullCategory> elementCategories = userRqstdCmdReport.UnitComposition.GetUniqueElementCategories();
                if (elementCategories.Contains(FacilityHullCategory.Laboratory)) {
                    criteria.Add(IconSelectionCriteria.Science);
                }
                if (elementCategories.Contains(FacilityHullCategory.Barracks)) {
                    criteria.Add(IconSelectionCriteria.Troop);
                }
                if (elementCategories.Contains(FacilityHullCategory.ColonyHab)) {
                    criteria.Add(IconSelectionCriteria.Colony);
                }
            }
            return criteria.ToArray();
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


