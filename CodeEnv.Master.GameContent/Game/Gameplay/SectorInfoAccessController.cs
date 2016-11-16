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
                case ItemInfoID.SectorID:
                    return true;
                case ItemInfoID.Owner:
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

    }
}

