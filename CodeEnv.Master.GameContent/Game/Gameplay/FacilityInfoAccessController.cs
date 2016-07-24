// --------------------------------------------------------------------------------------------------------------------
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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Facility info.
    /// </summary>
    public class FacilityInfoAccessController : AIntelInfoAccessController {

        public FacilityInfoAccessController(FacilityData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                case AccessControlInfoID.Culture:
                case AccessControlInfoID.Science:
                case AccessControlInfoID.NetIncome:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                case AccessControlInfoID.CurrentHitPoints:
                case AccessControlInfoID.Health:
                case AccessControlInfoID.Defense:
                case AccessControlInfoID.Offense:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Essential(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                case AccessControlInfoID.Category:
                case AccessControlInfoID.MaxHitPoints:
                case AccessControlInfoID.Mass:
                case AccessControlInfoID.WeaponsRange:
                case AccessControlInfoID.SensorRange:
                case AccessControlInfoID.Owner:
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

