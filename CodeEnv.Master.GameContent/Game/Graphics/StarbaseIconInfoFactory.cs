// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseIconInfoFactory.cs
// Singleton. Factory that makes instances of IconInfo for Starbases.
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
    /// Singleton. Factory that makes instances of IconInfo for Starbases.
    /// </summary>
    public class StarbaseIconInfoFactory : ACmdIconInfoFactory<StarbaseReport, StarbaseIconInfoFactory> {

        protected override AtlasID AtlasID { get { return AtlasID.Fleet; } }

        private StarbaseIconInfoXmlReader _xmlReader;

        private StarbaseIconInfoFactory() {
            Initialize();
            _xmlReader = StarbaseIconInfoXmlReader.Instance;
        }

        protected override void Initialize() {
            base.Initialize();
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
            IEnumerable<FacilityHullCategory> elementCategories = starbaseReport.UnitComposition.GetUniqueElementCategories();
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

        public class StarbaseIconInfoXmlReader : ACmdIconInfoXmlReader<StarbaseIconInfoXmlReader> {

            protected override string XmlFilename { get { return "FleetIconInfo"; } }   //TODO

            private StarbaseIconInfoXmlReader() {
                Initialize();
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }

        }

        #endregion

    }
}


