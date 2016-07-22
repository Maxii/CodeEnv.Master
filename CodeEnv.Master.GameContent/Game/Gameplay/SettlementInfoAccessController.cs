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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Settlement info.
    /// </summary>
    public class SettlementInfoAccessController : AIntelInfoAccessController {

        public SettlementInfoAccessController(SettlementCmdData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(AccessControlInfoID infoID) {
            switch (infoID) {
                case AccessControlInfoID.CurrentCmdEffectiveness:
                case AccessControlInfoID.UnitScience:
                case AccessControlInfoID.UnitNetIncome:
                case AccessControlInfoID.UnitCulture:
                case AccessControlInfoID.Capacity:
                case AccessControlInfoID.Approval:
                case AccessControlInfoID.Resources:
                case AccessControlInfoID.Population:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(AccessControlInfoID infoID) {
            switch (infoID) {
                case AccessControlInfoID.UnitWeaponsRange:
                case AccessControlInfoID.UnitSensorRange:
                case AccessControlInfoID.UnitHealth:
                case AccessControlInfoID.UnitMaxHitPts:
                case AccessControlInfoID.UnitCurrentHitPts:
                case AccessControlInfoID.Composition:
                case AccessControlInfoID.Formation:
                case AccessControlInfoID.UnitDefense:
                case AccessControlInfoID.UnitOffense:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Essential(AccessControlInfoID infoID) {
            switch (infoID) {
                case AccessControlInfoID.Owner:
                case AccessControlInfoID.Category:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Basic(AccessControlInfoID infoID) {
            switch (infoID) {
                case AccessControlInfoID.Name:
                case AccessControlInfoID.ParentName:
                case AccessControlInfoID.Position:
                case AccessControlInfoID.SectorIndex:
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

