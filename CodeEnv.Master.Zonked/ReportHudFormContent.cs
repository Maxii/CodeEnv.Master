// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ReportHudFormContent.cs
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
    [System.Obsolete]
    public class ReportHudFormContent : AHudFormContent {

        public AItemReport Report { get; private set; }

        public ReportHudFormContent(HudFormID formID, AItemReport report)
            : base(formID) {
            Report = report;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

