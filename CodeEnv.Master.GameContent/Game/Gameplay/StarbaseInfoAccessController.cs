// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseInfoAccessController.cs
// Controls access to Starbase info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Starbase info.
    /// </summary>
    public class StarbaseInfoAccessController : AIntelInfoAccessController {

        public StarbaseInfoAccessController(StarbaseCmdData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(ItemInfoID infoID, Player player) {
            switch (infoID) {
                case ItemInfoID.CurrentCmdEffectiveness:
                case ItemInfoID.UnitScience:
                case ItemInfoID.UnitNetIncome:
                case ItemInfoID.Resources:
                case ItemInfoID.UnitCulture:
                case ItemInfoID.UnitFood:
                case ItemInfoID.UnitProduction:
                case ItemInfoID.Capacity:
                case ItemInfoID.UnitCurrentHitPts:
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
                case ItemInfoID.Formation:
                case ItemInfoID.Composition:
                case ItemInfoID.UnitOffense:
                case ItemInfoID.UnitDefense:
                case ItemInfoID.Hero:
                    //D.Log(ShowDebugLog, "{0}.HasAccesstoInfo_Broad({1}, {2}) called.", DebugName, infoID.GetValueName(), player);
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
                default:
                    return false;
            }
        }

    }
}

