// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIconFactory.cs
// Singleton. Factory that makes instances of IIcon, caches and reuses them.
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
    /// Singleton. Factory that makes instances of IIcon, caches and reuses them. The reuse is critical as 
    /// the object's equality comparer (same instance in memory) is used by the client of the factory
    /// to determine which icon is currently showing.
    /// </summary>
    public class FleetIconFactory : AIconFactory<FleetIcon, FleetCmdItemData, FleetIconFactory> {

        private FleetIconFactory() {
            Initialize();
        }

        protected override IconSelectionCriteria GetCriteriaFromCategory(FleetCmdItemData data) {
            switch (data.Category) {
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
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(data.Category));
            }
        }

        protected override IEnumerable<IconSelectionCriteria> GetCriteriaFromComposition(FleetCmdItemData data) {
            IList<IconSelectionCriteria> criteria = new List<IconSelectionCriteria>();
            IEnumerable<ShipCategory> elementCategories = data.UnitComposition.GetUniqueElementCategories();
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

