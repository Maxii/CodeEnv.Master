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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for the Universe Center.
    /// </summary>
    public class UniverseCenterPublisher : AIntelItemPublisher<UniverseCenterReport, UniverseCenterData> {

        public override ColoredStringBuilder ItemHudText {
            get { return UniverseCenterDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        public UniverseCenterPublisher(UniverseCenterData data) : base(data) { }

        protected override UniverseCenterReport MakeReportInstance(Player player) {
            return new UniverseCenterReport(_data, player);
        }

    }
}

