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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Singleton. Factory that makes instances of IconInfo for Starbases.
    /// </summary>
    public class StarbaseIconInfoFactory : ACmdIconInfoFactory<StarbaseCmdReport, StarbaseIconInfoFactory> {

        protected override AtlasID AtlasID { get { return AtlasID.Fleet; } }

        private StarbaseIconInfoXmlReader _xmlReader;

        private StarbaseIconInfoFactory() {
            Initialize();
            _xmlReader = StarbaseIconInfoXmlReader.Instance;
        }

        protected sealed override void Initialize() {
            base.Initialize();
        }

        protected override IconSelectionCriteria[] GetSelectionCriteria(StarbaseCmdReport userRqstdCmdReport) {
            if (userRqstdCmdReport.IntelCoverage == IntelCoverage.None) {
                // Reports are requested when an element/cmd loses all IntelCoverage and the Cmd re-evaluates its icon
                return new IconSelectionCriteria[] { IconSelectionCriteria.None };
            }

            if (userRqstdCmdReport.Category == StarbaseCategory.None) {
                D.AssertNull(userRqstdCmdReport.UnitComposition); // UnitComposition should not be known if Category isn't known
                // User has no permission to know category so return unknown
                return new IconSelectionCriteria[] { IconSelectionCriteria.Unknown };
            }

            IList<IconSelectionCriteria> criteria = new List<IconSelectionCriteria>();
            switch (userRqstdCmdReport.Category) {
                case StarbaseCategory.Outpost:
                    criteria.Add(IconSelectionCriteria.Level1);
                    break;
                case StarbaseCategory.LocalBase:
                    criteria.Add(IconSelectionCriteria.Level2);
                    break;
                case StarbaseCategory.DistrictBase:
                    criteria.Add(IconSelectionCriteria.Level3);
                    break;
                case StarbaseCategory.RegionalBase:
                    criteria.Add(IconSelectionCriteria.Level4);
                    break;
                case StarbaseCategory.TerritorialBase:
                    criteria.Add(IconSelectionCriteria.Level5);
                    break;
                case StarbaseCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(userRqstdCmdReport.Category));
            }

            if (userRqstdCmdReport.UnitComposition != null) {   // check for access rights
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

        #region Nested Classes

        public class StarbaseIconInfoXmlReader : ACmdIconInfoXmlReader<StarbaseIconInfoXmlReader> {

            protected override string XmlFilename { get { return "FleetIconInfo"; } }   //TODO

            private StarbaseIconInfoXmlReader() {
                Initialize();
            }

        }

        #endregion

    }
}


