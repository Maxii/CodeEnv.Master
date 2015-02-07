// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityData2.cs
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
    public class FacilityData2 : AElementData2 {

        public FacilityCategory Category { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityData" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="topography">The topography.</param>
        public FacilityData2(FacilityStat stat, Topography topography)
            : base(stat.Name, stat.Mass, stat.MaxHitPoints) {
            Category = stat.Category;
            base.Topography = topography;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

