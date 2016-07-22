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
    /// <remarks>As Systems do not have an IntelCoverage value, access to some values
    /// here are determined by whether the Star, Planetoids and Settlement, if any, provide access.</remarks>
    /// </summary>
    public class SystemInfoAccessController : AInfoAccessController {

        public SystemInfoAccessController(SystemData data) : base(data) { }

        public override bool HasAccessToInfo(Player player, AccessControlInfoID infoID) {
            SystemData sysData = _data as SystemData;
            bool starHasAccess;
            switch (infoID) {
                case AccessControlInfoID.Name:
                case AccessControlInfoID.Position:
                case AccessControlInfoID.SectorIndex:
                    return true;
                case AccessControlInfoID.Owner:
                    // if know any member's owner, you know the system's owner
                    starHasAccess = sysData.StarData.InfoAccessCntlr.HasAccessToInfo(player, infoID);
                    if (starHasAccess) {
                        return true;
                    }
                    else {
                        bool anyPlanetoidHasAccess = sysData.AllPlanetoidData.Select(pData => pData.InfoAccessCntlr).Any(iac => iac.HasAccessToInfo(player, infoID));
                        if (anyPlanetoidHasAccess) {
                            return true;
                        }
                        else if (sysData.SettlementData != null && sysData.SettlementData.InfoAccessCntlr.HasAccessToInfo(player, infoID)) {
                            return true;
                        }
                    }
                    return false;
                case AccessControlInfoID.Capacity:
                case AccessControlInfoID.Resources:
                    // if you know all member's values, you know the system's value
                    starHasAccess = sysData.StarData.InfoAccessCntlr.HasAccessToInfo(player, infoID);
                    if (starHasAccess) {
                        bool allPlanetoidsHaveAccess = sysData.AllPlanetoidData.Select(pData => pData.InfoAccessCntlr).All(iac => iac.HasAccessToInfo(player, infoID));
                        if (allPlanetoidsHaveAccess) {
                            if (sysData.SettlementData == null || sysData.SettlementData.InfoAccessCntlr.HasAccessToInfo(player, infoID)) {
                                return true;
                            }
                        }
                    }
                    return false;
                default:
                    return false;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

