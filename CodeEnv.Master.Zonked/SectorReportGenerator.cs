// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorReportGenerator.cs
// COMMENT - one line to give a brief idea of what the file does.
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
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public class SectorReportGenerator : AReportGenerator<SectorItemData, SectorReport> {

        static SectorReportGenerator() {
            LabelFormatter = new SectorLabelFormatter();
        }

        public SectorReportGenerator(SectorItemData data) : base(data) { }

        protected override SectorReport GenerateReport(Player player, AIntel intel) {
            return new SectorReport(_data, player, intel);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

