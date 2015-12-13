// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetIconInfo.cs
// Class that acquires the filename of a Fleet Icon image based on a provided set of criteria.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class that acquires the filename of a Fleet Icon image based on a provided set of criteria.
    /// WARNING: Type name is used to find XML filename.
    /// </summary>
    [System.Obsolete]
    public class FleetIconInfo : AIconInfo {

        public FleetIconInfo(IconSection section, params IconSelectionCriteria[] criteria)
            : base(section, criteria) {
        }

        public override string ToString() {
            return _toStringFormat.Inject(GetType().Name, AtlasID.GetValueName(), Size, Filename, Color.GetValueName(), Placement.GetValueName());
        }

    }
}

