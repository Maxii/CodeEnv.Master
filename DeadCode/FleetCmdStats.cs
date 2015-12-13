// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdStats.cs
// Class containing values and settings for building Fleet Commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Class containing values and settings for building Fleet Commands.
    /// </summary>
    [Obsolete]
    public class FleetCmdStats : ACommandStats {

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

