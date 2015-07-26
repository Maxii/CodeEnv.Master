// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterPublisher.cs
// Report and HudContent Publisher for the Universe Center.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for the Universe Center.
    /// </summary>
    public class UniverseCenterPublisher : AIntelItemPublisher<UniverseCenterReport, UniverseCenterData> {

        public override ColoredStringBuilder ItemHudText {
            get { return UniverseCenterDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        private IUniverseCenterItem _item;

        public UniverseCenterPublisher(UniverseCenterData data, IUniverseCenterItem item)
            : base(data) {
            _item = item;
        }

        protected override UniverseCenterReport GenerateReport(Player player) {
            return new UniverseCenterReport(_data, player, _item);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

