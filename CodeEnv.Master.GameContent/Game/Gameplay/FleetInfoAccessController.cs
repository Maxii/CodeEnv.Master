// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetInfoAccessController.cs
// Controls access to Fleet info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Fleet info.
    /// </summary>
    public class FleetInfoAccessController : AIntelInfoAccessController {

        public FleetInfoAccessController(FleetCmdData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.CurrentCmdEffectiveness:
                case ItemInfoID.Formation:
                case ItemInfoID.UnitScience:
                case ItemInfoID.UnitCulture:
                case ItemInfoID.Resources:
                case ItemInfoID.UnitNetIncome:
                case ItemInfoID.Capacity:
                case ItemInfoID.UnitCurrentHitPts:
                case ItemInfoID.UnitMaxTurnRate:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.UnitWeaponsRange:
                case ItemInfoID.UnitSensorRange:
                case ItemInfoID.Composition:
                case ItemInfoID.UnitHealth:
                case ItemInfoID.UnitMaxHitPts:
                case ItemInfoID.UnitOffense:
                case ItemInfoID.UnitDefense:
                case ItemInfoID.UnitFullSpeed:
                case ItemInfoID.CurrentHeading:
                case ItemInfoID.Target:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Essential(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Owner:
                case ItemInfoID.Category:
                case ItemInfoID.CurrentSpeedSetting:
                case ItemInfoID.AlertStatus:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Basic(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Name:
                case ItemInfoID.ParentName:
                case ItemInfoID.Position:
                case ItemInfoID.SectorID:
                    return true;
                default:
                    return false;
            }
        }

    }
}

