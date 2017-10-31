﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipPublisher.cs
// Report and HudContent Publisher for Ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Report and HudContent Publisher for Ships.
    /// </summary>
    public class ShipPublisher : AIntelItemPublisher<ShipReport, ShipData> {

        public override ColoredStringBuilder ItemHudText {
            get { return ShipDisplayInfoFactory.Instance.MakeInstance(GetUserReport()); }
        }

        public ShipPublisher(ShipData data) : base(data) { }

        protected override ShipReport MakeReportInstance(Player player) {
            return new ShipReport(_data, player);
        }

    }
}

