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
    /// As searching XML docs to find the filename is expensive, this implementation caches and reuses the 
    /// IconInfo instances, even though they are structures. 
    /// </summary>
    [Obsolete]
    public class FleetIconInfoFactory : ACmdIconInfoFactory<FleetReport, FleetIconInfoFactory> {

        protected override AtlasID AtlasID { get { return AtlasID.Fleet; } }

        protected override string XmlFilename { get { return "FleetIconInfo"; } }

        private FleetIconInfoFactory() {
            Initialize();
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
            IEnumerable<ShipCategory> elementCategories = fleetReport.UnitComposition.GetUniqueElementCategories();
            if (elementCategories.Contains(ShipCategory.Science)) {
                criteria.Add(IconSelectionCriteria.Science);
            }
            if (elementCategories.Contains(ShipCategory.Troop)) {
                criteria.Add(IconSelectionCriteria.Troop);
            }
            if (elementCategories.Contains(ShipCategory.Colonizer)) {
                criteria.Add(IconSelectionCriteria.Colony);
            }
            return criteria;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}


