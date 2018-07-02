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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to System info.
    /// </summary>
    public class SystemInfoAccessController : AIntelInfoAccessController {

        private IDictionary<Player, bool> _hasBasicAccessToOwnerLookup = new Dictionary<Player, bool>();

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
                    // 10.13.17 Removed Resources as Report now handles Resources without AccessCntlr
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
                case ItemInfoID.SectorID:
                    return true;
                case ItemInfoID.Owner:
                    // If gets here, System IntelCoverage is Basic, but a member could be allowing access
                    // 6.16.18 Cntlr has multiple clients so can't consolidate logic in Item.AssessWhetherToFireOwnerInfoAccessChgdEventFor

                    // Note: Once a System grants access to Owner it can't be rescinded as Stars and Planetoids use NonRegressibleIntel.
                    // However, a 'defacto' rescind could theoretically take place if 1) one or more planetoid(s) was the source of the grant 
                    // of access, 2) all those planetoids were destroyed, and 3) the HasAccessToInfo approach below was used without 
                    // the memory of the dictionary. That is why the memory of the dictionary is used, to remedy that corner case.
                    bool hasBasicAccessToOwner;
                    if (!_hasBasicAccessToOwnerLookup.TryGetValue(player, out hasBasicAccessToOwner)) {
                        hasBasicAccessToOwner = false;
                        _hasBasicAccessToOwnerLookup.Add(player, hasBasicAccessToOwner);
                    }

                    if (hasBasicAccessToOwner) {
                        return true;
                    }

                    SystemData sysData = _data as SystemData;
                    hasBasicAccessToOwner = sysData.StarData.InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner);
                    if (!hasBasicAccessToOwner) {
                        hasBasicAccessToOwner = sysData.AllPlanetoidData.Select(pData => pData.InfoAccessCntlr).
                            Any(iac => iac.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner));
                    }
                    _hasBasicAccessToOwnerLookup[player] = hasBasicAccessToOwner;   // Reqd assignment when the out is not a ReferenceType

                    return hasBasicAccessToOwner;
                // WARNING: Do not inquire about Settlement Owner as Settlement inquires about System Owner creating a circular loop
                default:
                    return false;
            }
        }

        #region Archive

        // 7.22.16 version : AInfoAccessController

        //public override bool HasAccessToInfo(Player player, ItemInfoID infoID) {
        //    SystemData sysData = _data as SystemData;
        //    bool starHasAccess;
        //    switch (infoID) {
        //        case ItemInfoID.Name:
        //        case ItemInfoID.Position:
        //        case ItemInfoID.SectorID:
        //            return true;
        //        case ItemInfoID.Owner:
        //            // if know any member's owner, you know the system's owner
        //            starHasAccess = sysData.StarData.InfoAccessCntlr.HasAccessToInfo(player, infoID);
        //            if (starHasAccess) {
        //                return true;
        //            }
        //            else {
        //                bool anyPlanetoidHasAccess = sysData.AllPlanetoidData.Select(pData => pData.InfoAccessCntlr).Any(ac => ac.HasAccessToInfo(player, infoID));
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
        //                bool allPlanetoidsHaveAccess = sysData.AllPlanetoidData.Select(pData => pData.InfoAccessCntlr).All(ac => ac.HasAccessToInfo(player, infoID));
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

