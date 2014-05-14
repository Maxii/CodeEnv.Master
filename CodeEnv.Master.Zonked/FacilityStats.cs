// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityStats.cs
// Class containing values and settings for building Facilities.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Class containing values and settings for building Facilities.
    /// </summary>
    [Obsolete]
    public class FacilityStats : AElementStats {

        public FacilityCategory Category { get; set; }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

