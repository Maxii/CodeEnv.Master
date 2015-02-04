// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterReportGenerator.cs
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
    public class UniverseCenterReportGenerator : AReportGenerator<UniverseCenterData, UniverseCenterReport> {

        static UniverseCenterReportGenerator() {
            LabelFormatter = new UniverseCenterLabelFormatter();
        }

        public UniverseCenterReportGenerator(UniverseCenterData data) : base(data) { }

        protected override UniverseCenterReport GenerateReport(Player player, AIntel intel) {
            return new UniverseCenterReport(_data, player, intel);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

