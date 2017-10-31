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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Stars.
    /// </summary>
    public class StarPublisher : AIntelItemPublisher<StarReport, StarData> {

        public override ColoredStringBuilder ItemHudText {
            get { return StarDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        public StarPublisher(StarData data) : base(data) { }

        protected override StarReport MakeReportInstance(Player player) {
            return new StarReport(_data, player);
        }

    }
}

