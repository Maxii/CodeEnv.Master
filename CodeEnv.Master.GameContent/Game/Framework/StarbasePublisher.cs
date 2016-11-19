// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbasePublisher.cs
// Report and HudContent Publisher for Starbases.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Starbases.
    /// </summary>
    public class StarbasePublisher : ACmdPublisher<StarbaseCmdReport, StarbaseCmdData> {

        public override ColoredStringBuilder ItemHudText {
            get { return StarbaseDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        private IStarbaseCmd_Ltd _item;

        public StarbasePublisher(StarbaseCmdData data, IStarbaseCmd_Ltd item)
            : base(data) {
            _item = item;
        }

        protected override StarbaseCmdReport MakeReportInstance(Player player) {
            return new StarbaseCmdReport(_data, player, _item);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

