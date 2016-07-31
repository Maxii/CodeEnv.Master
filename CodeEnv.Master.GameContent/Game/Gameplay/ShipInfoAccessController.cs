// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipInfoAccessController.cs
// Controls access to Ship info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Ship info.
    /// </summary>
    public class ShipInfoAccessController : AIntelInfoAccessController {

        public ShipInfoAccessController(ShipData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Target:
                case ItemInfoID.CombatStance:
                case ItemInfoID.Culture:
                case ItemInfoID.Science:
                case ItemInfoID.NetIncome:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.CurrentHitPoints:
                case ItemInfoID.MaxTurnRate:
                case ItemInfoID.FullSpeed:
                case ItemInfoID.Offense:
                case ItemInfoID.Defense:
                case ItemInfoID.Health:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Essential(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Category:
                case ItemInfoID.Owner:
                case ItemInfoID.CurrentSpeedSetting:                //case ItemInfoID.ActualSpeed:
                case ItemInfoID.WeaponsRange:
                case ItemInfoID.MaxHitPoints:
                case ItemInfoID.SensorRange:
                case ItemInfoID.Mass:
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
                case ItemInfoID.SectorIndex:
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

