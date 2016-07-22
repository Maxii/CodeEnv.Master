// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemPublisher.cs
// Report and HudContent Publisher for Systems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Systems.
    /// </summary>
    public class SystemPublisher : AItemPublisher<SystemReport, SystemData> {

        public override ColoredStringBuilder ItemHudText {
            get { return SystemDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        private ISystem_Ltd _item;

        public SystemPublisher(SystemData data, ISystem_Ltd item)
            : base(data) {
            _item = item;
        }

        protected override SystemReport MakeReportInstance(Player player) {
            return new SystemReport(_data, player, _item);
        }

        private bool IsEqual(IEnumerable<PlanetoidReport> reportsA, IEnumerable<PlanetoidReport> reportsB) {
            var isEqual = reportsA.OrderBy(r => r.Name).SequenceEqual(reportsB.OrderBy(r => r.Name));
            string equalsPhrase = isEqual ? "equal" : "not equal";
            D.Log("{0} PlanetoidReports are {1}.", GetType().Name, equalsPhrase);
            return isEqual;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

