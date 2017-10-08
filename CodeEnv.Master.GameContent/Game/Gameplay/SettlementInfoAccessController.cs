// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementInfoAccessController.cs
// Controls access to Settlement info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Settlement info.
    /// </summary>
    public class SettlementInfoAccessController : AIntelInfoAccessController {

        public SettlementInfoAccessController(SettlementCmdData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.CurrentCmdEffectiveness:
                case ItemInfoID.UnitScience:
                case ItemInfoID.UnitNetIncome:
                case ItemInfoID.UnitCulture:
                case ItemInfoID.UnitProduction:
                case ItemInfoID.Capacity:
                case ItemInfoID.Approval:
                case ItemInfoID.Resources:
                case ItemInfoID.Population:
                case ItemInfoID.CurrentConstruction:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.UnitWeaponsRange:
                case ItemInfoID.UnitSensorRange:
                case ItemInfoID.UnitHealth:
                case ItemInfoID.UnitMaxHitPts:
                case ItemInfoID.UnitCurrentHitPts:
                case ItemInfoID.Composition:
                case ItemInfoID.Formation:
                case ItemInfoID.UnitDefense:
                case ItemInfoID.UnitOffense:
                case ItemInfoID.Hero:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Essential(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.Owner:
                case ItemInfoID.Category:
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
                case ItemInfoID.Owner:
                    // If gets here, Settlement IntelCoverage is Basic, but ParentSystem could be allowing access.
                    // System uses NonRegressibleIntel where Coverage can't regress. Settlement uses RegressibleIntel which allows regress
                    SystemInfoAccessController parentSysAccessCntlr = (_data as SettlementCmdData).ParentSystemData.InfoAccessCntlr;
                    return parentSysAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner);
                default:
                    return false;
            }
        }

    }
}

