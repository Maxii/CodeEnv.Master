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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Fleet info.
    /// </summary>
    public class FleetInfoAccessController : AIntelInfoAccessController {

        public FleetInfoAccessController(FleetCmdData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                case AccessControlInfoID.CurrentCmdEffectiveness:
                case AccessControlInfoID.Formation:
                case AccessControlInfoID.UnitScience:
                case AccessControlInfoID.UnitCulture:
                case AccessControlInfoID.Resources:
                case AccessControlInfoID.UnitNetIncome:
                case AccessControlInfoID.Capacity:
                case AccessControlInfoID.UnitCurrentHitPts:
                case AccessControlInfoID.UnitFullSpeed:
                case AccessControlInfoID.UnitMaxTurnRate:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                case AccessControlInfoID.UnitWeaponsRange:
                case AccessControlInfoID.UnitSensorRange:
                case AccessControlInfoID.Composition:
                case AccessControlInfoID.UnitHealth:
                case AccessControlInfoID.UnitMaxHitPts:
                case AccessControlInfoID.UnitOffense:
                case AccessControlInfoID.UnitDefense:
                case AccessControlInfoID.Target:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Essential(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                case AccessControlInfoID.Owner:
                case AccessControlInfoID.Category:
                case AccessControlInfoID.CurrentSpeed:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Basic(AccessControlInfoID infoID, Player player) {
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

