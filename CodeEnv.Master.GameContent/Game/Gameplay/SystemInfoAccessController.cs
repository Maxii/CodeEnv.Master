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

        protected override bool HasAccessToInfo_Comprehensive(ItemInfoID infoID, Player player) {
            switch (infoID) {
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Capacity:
                case ItemInfoID.Resources:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Essential(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Owner:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Basic(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Name:
                case ItemInfoID.Position:
                case ItemInfoID.SectorIndex:
                    return true;
                case ItemInfoID.Owner:
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

        //public override bool HasAccessToInfo(Player player, ItemInfoID infoID) {
        //    SystemData sysData = _data as SystemData;
        //    bool starHasAccess;
        //    switch (infoID) {
        //        case ItemInfoID.Name:
        //        case ItemInfoID.Position:
        //        case ItemInfoID.SectorIndex:
        //            return true;
        //        case ItemInfoID.Owner:
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
        //        case ItemInfoID.Capacity:
        //        case ItemInfoID.Resources:
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

