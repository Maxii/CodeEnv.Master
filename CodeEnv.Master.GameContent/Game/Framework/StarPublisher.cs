// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarPublisher.cs
// Report and HudContent Publisher for Stars.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Stars.
    /// </summary>
    public class StarPublisher : AIntelItemPublisher<StarReport, StarData> {

        public override ColoredStringBuilder HudContent {
            get { return StarDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        private IStarItem _item;

        public StarPublisher(StarData data, IStarItem item) : base(data) { _item = item; }

        protected override StarReport GenerateReport(Player player) {
            return new StarReport(_data, player, _item);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

