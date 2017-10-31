﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityInfoAccessController.cs
// Controls access to Facility info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Facility info.
    /// </summary>
    public class FacilityInfoAccessController : AIntelInfoAccessController {

        public FacilityInfoAccessController(FacilityData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(ItemInfoID infoID, Player player) {
            switch (infoID) {
                // 10.18.17 Outputs now handled by Report without AccessCntlr
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.CurrentHitPoints:
                case ItemInfoID.Health:
                case ItemInfoID.Defense:
                case ItemInfoID.Offense:
                case ItemInfoID.ConstructionCost:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Essential(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Category:
                case ItemInfoID.MaxHitPoints:
                case ItemInfoID.Mass:
                case ItemInfoID.WeaponsRange:
                case ItemInfoID.SensorRange:
                case ItemInfoID.Owner:
                case ItemInfoID.AlertStatus:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Basic(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Name:
                case ItemInfoID.UnitName:
                case ItemInfoID.Position:
                case ItemInfoID.SectorID:
                    return true;
                default:
                    return false;
            }
        }

    }
}

