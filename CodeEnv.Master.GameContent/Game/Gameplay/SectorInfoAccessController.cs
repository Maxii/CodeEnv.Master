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

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Sector info.
    /// </summary>
    public class SectorInfoAccessController : AIntelInfoAccessController {

        public SectorInfoAccessController(SectorData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Essential(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                case AccessControlInfoID.Owner:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Basic(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                case AccessControlInfoID.Name:
                case AccessControlInfoID.Position:
                case AccessControlInfoID.SectorIndex:
                    return true;
                case AccessControlInfoID.Owner:
                    // If gets here, Sector IntelCoverage is Basic, but a member could be allowing access.
                    SectorData sectorData = _data as SectorData;
                    bool systemHasAccess = sectorData.SystemData != null ? sectorData.SystemData.InfoAccessCntlr.HasAccessToInfo(player, infoID) : false;
                    if (systemHasAccess) {
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Archive

        //public override bool HasAccessToInfo(Player player, AccessControlInfoID infoID) {
        //    switch (infoID) {
        //        case AccessControlInfoID.Name:
        //        case AccessControlInfoID.Position:
        //        case AccessControlInfoID.SectorIndex:
        //        case AccessControlInfoID.Owner:
        //            return true;
        //        default:
        //            return false;
        //    }
        //}

        #endregion

    }
}

