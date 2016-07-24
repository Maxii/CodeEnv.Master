// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemInfoAccessController.cs
// Controls access to System info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to System info.
    /// </summary>
    public class SystemInfoAccessController : AIntelInfoAccessController {

        public SystemInfoAccessController(SystemData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                case AccessControlInfoID.Capacity:
                case AccessControlInfoID.Resources:
                    return true;
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
                    // If gets here, System IntelCoverage is Basic, but a member could be allowing access
                    SystemData sysData = _data as SystemData;
                    bool starHasAccess = sysData.StarData.InfoAccessCntlr.HasAccessToInfo(player, infoID);
                    if (starHasAccess) {
                        return true;
                    }
                    else {
                        bool anyPlanetoidHasAccess = sysData.AllPlanetoidData.Select(pData => pData.InfoAccessCntlr).Any(iac => iac.HasAccessToInfo(player, infoID));
                        if (anyPlanetoidHasAccess) {
                            return true;
                        }
                        // WARNING: Do not inquire about Settlement Owner as Settlement inquires about System Owner creating a circular loop
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

        // 7.22.16 version : AInfoAccessController

        //public override bool HasAccessToInfo(Player player, AccessControlInfoID infoID) {
        //    SystemData sysData = _data as SystemData;
        //    bool starHasAccess;
        //    switch (infoID) {
        //        case AccessControlInfoID.Name:
        //        case AccessControlInfoID.Position:
        //        case AccessControlInfoID.SectorIndex:
        //            return true;
        //        case AccessControlInfoID.Owner:
        //            // if know any member's owner, you know the system's owner
        //            starHasAccess = sysData.StarData.InfoAccessCntlr.HasAccessToInfo(player, infoID);
        //            if (starHasAccess) {
        //                return true;
        //            }
        //            else {
        //                bool anyPlanetoidHasAccess = sysData.AllPlanetoidData.Select(pData => pData.InfoAccessCntlr).Any(iac => iac.HasAccessToInfo(player, infoID));
        //                if (anyPlanetoidHasAccess) {
        //                    return true;
        //                }
        //                else if (sysData.SettlementData != null && sysData.SettlementData.InfoAccessCntlr.HasAccessToInfo(player, infoID)) {
        //                    return true;
        //                }
        //            }
        //            return false;
        //        case AccessControlInfoID.Capacity:
        //        case AccessControlInfoID.Resources:
        //            // if you know all member's values, you know the system's value
        //            starHasAccess = sysData.StarData.InfoAccessCntlr.HasAccessToInfo(player, infoID);
        //            if (starHasAccess) {
        //                bool allPlanetoidsHaveAccess = sysData.AllPlanetoidData.Select(pData => pData.InfoAccessCntlr).All(iac => iac.HasAccessToInfo(player, infoID));
        //                if (allPlanetoidsHaveAccess) {
        //                    if (sysData.SettlementData == null || sysData.SettlementData.InfoAccessCntlr.HasAccessToInfo(player, infoID)) {
        //                        return true;
        //                    }
        //                }
        //            }
        //            return false;
        //        default:
        //            return false;
        //    }
        //}

        #endregion

    }
}

