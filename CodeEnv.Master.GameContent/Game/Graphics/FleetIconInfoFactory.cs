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
    public class FleetIconInfoFactory : ACmdIconInfoFactory<FleetReport, FleetIconInfoFactory> {

        protected override AtlasID AtlasID { get { return AtlasID.Fleet; } }

        private FleetIconInfoXmlReader _xmlReader;

        private FleetIconInfoFactory() {
            Initialize();
        }

        protected override void Initialize() {
            base.Initialize();
            _xmlReader = FleetIconInfoXmlReader.Instance;
        }

        protected override IconSelectionCriteria GetCriteriaFromCategory(FleetReport fleetReport) {
            switch (fleetReport.Category) {
                case FleetCategory.Flotilla:
                    return IconSelectionCriteria.Level1;
                case FleetCategory.Squadron:
                    return IconSelectionCriteria.Level2;
                case FleetCategory.TaskForce:
                    return IconSelectionCriteria.Level3;
                case FleetCategory.BattleGroup:
                    return IconSelectionCriteria.Level4;
                case FleetCategory.Armada:
                    return IconSelectionCriteria.Level5;
                case FleetCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(fleetReport.Category));
            }
        }

        protected override IEnumerable<IconSelectionCriteria> GetCriteriaFromComposition(FleetReport fleetReport) {
            IList<IconSelectionCriteria> criteria = new List<IconSelectionCriteria>();
            IEnumerable<ShipHullCategory> elementCategories = fleetReport.UnitComposition.GetUniqueElementCategories();
            if (elementCategories.Contains(ShipHullCategory.Science)) {
                criteria.Add(IconSelectionCriteria.Science);
            }
            if (elementCategories.Contains(ShipHullCategory.Troop)) {
                criteria.Add(IconSelectionCriteria.Troop);
            }
            if (elementCategories.Contains(ShipHullCategory.Colonizer)) {
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


