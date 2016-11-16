// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetIconInfoFactory.cs
// Singleton. Factory that makes instances of IconInfo for Fleets.
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
    /// Singleton. Factory that makes instances of IconInfo for Fleets.
    /// </summary>
    public class FleetIconInfoFactory : ACmdIconInfoFactory<FleetCmdReport, FleetIconInfoFactory> {

        protected override AtlasID AtlasID { get { return AtlasID.Fleet; } }

        private FleetIconInfoXmlReader _xmlReader;

        private FleetIconInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() {
            base.Initialize();
            _xmlReader = FleetIconInfoXmlReader.Instance;
        }

        protected override IconSelectionCriteria[] GetSelectionCriteria(FleetCmdReport userRqstdCmdReport) {
            if (userRqstdCmdReport.IntelCoverage == IntelCoverage.None) {
                // Reports are requested when an element/cmd loses all IntelCoverage and the Cmd re-evaluates its icon
                return new IconSelectionCriteria[] { IconSelectionCriteria.None };
            }

            if (userRqstdCmdReport.Category == FleetCategory.None) {
                D.AssertNull(userRqstdCmdReport.UnitComposition); // UnitComposition should not be known if Category isn't known
                // User has no permission to know category so return unknown icon criteria
                return new IconSelectionCriteria[] { IconSelectionCriteria.Unknown };
            }

            IList<IconSelectionCriteria> criteria = new List<IconSelectionCriteria>();
            switch (userRqstdCmdReport.Category) {
                case FleetCategory.Flotilla:
                    criteria.Add(IconSelectionCriteria.Level1);
                    break;
                case FleetCategory.Squadron:
                    criteria.Add(IconSelectionCriteria.Level2);
                    break;
                case FleetCategory.TaskForce:
                    criteria.Add(IconSelectionCriteria.Level3);
                    break;
                case FleetCategory.BattleGroup:
                    criteria.Add(IconSelectionCriteria.Level4);
                    break;
                case FleetCategory.Armada:
                    criteria.Add(IconSelectionCriteria.Level5);
                    break;
                case FleetCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(userRqstdCmdReport.Category));
            }

            if (userRqstdCmdReport.UnitComposition != null) {   // check for access rights
                IEnumerable<ShipHullCategory> elementCategories = userRqstdCmdReport.UnitComposition.GetUniqueElementCategories();
                if (elementCategories.Contains(ShipHullCategory.Investigator)) {
                    criteria.Add(IconSelectionCriteria.Science);
                }
                if (elementCategories.Contains(ShipHullCategory.Troop)) {
                    criteria.Add(IconSelectionCriteria.Troop);
                }
                if (elementCategories.Contains(ShipHullCategory.Colonizer)) {
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

        public class FleetIconInfoXmlReader : ACmdIconInfoXmlReader<FleetIconInfoXmlReader> {

            protected override string XmlFilename { get { return "FleetIconInfo"; } }

            private FleetIconInfoXmlReader() {
                Initialize();
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }

        }

        #endregion

    }
}


