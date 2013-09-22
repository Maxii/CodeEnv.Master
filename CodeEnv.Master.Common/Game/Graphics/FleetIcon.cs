// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetIcon.cs
// Class that acquires the filename of a Fleet Icon image based on a provided set of criteria.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Class that acquires the filename of a Fleet Icon image based on a provided set of criteria.
    /// </summary>
    public class FleetIcon : AIcon<FleetIcon> {

        public FleetIcon(IconSection section, params IconSelectionCriteria[] criteria)
            : base(section, criteria) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

