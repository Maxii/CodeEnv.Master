// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorInfoAccessController.cs
// Controls access to Sector info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Sector info.
    /// </summary>
    public class SectorInfoAccessController : AInfoAccessController {

        public SectorInfoAccessController(SectorData data) : base(data) { }

        public override bool HasAccessToInfo(Player player, AccessControlInfoID infoID) {
            switch (infoID) {
                case AccessControlInfoID.Name:
                case AccessControlInfoID.Owner:
                case AccessControlInfoID.Position:
                case AccessControlInfoID.SectorIndex:
                    return true;
                default:
                    return false;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

