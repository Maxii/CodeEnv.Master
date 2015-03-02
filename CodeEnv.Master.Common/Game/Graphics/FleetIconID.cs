// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetIconID.cs
// Class that acquires the filename of a Fleet Icon image based on a provided set of criteria.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Class that acquires the filename of a Fleet Icon image based on a provided set of criteria.
    /// WARNING: Type name is used to find XML filename.
    /// </summary>
    public class FleetIconID : AIconID {

        public FleetIconID(IconSection section, params IconSelectionCriteria[] criteria)
            : base(section, criteria) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

