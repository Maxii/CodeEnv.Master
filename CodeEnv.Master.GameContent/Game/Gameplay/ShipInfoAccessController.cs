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

        protected override bool HasAccessToInfo_Comprehensive(AccessControlInfoID infoID) {
            switch (infoID) {
                case AccessControlInfoID.Target:
                case AccessControlInfoID.CombatStance:
                case AccessControlInfoID.Culture:
                case AccessControlInfoID.Science:
                case AccessControlInfoID.NetIncome:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(AccessControlInfoID infoID) {
            switch (infoID) {
                case AccessControlInfoID.CurrentHitPoints:
                case AccessControlInfoID.MaxTurnRate:
                case AccessControlInfoID.FullSpeed:
                case AccessControlInfoID.Offense:
                case AccessControlInfoID.Defense:
                case AccessControlInfoID.Health:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Essential(AccessControlInfoID infoID) {
            switch (infoID) {
                case AccessControlInfoID.Category:
                case AccessControlInfoID.Owner:
                case AccessControlInfoID.CurrentSpeed:
                case AccessControlInfoID.WeaponsRange:
                case AccessControlInfoID.MaxHitPoints:
                case AccessControlInfoID.SensorRange:
                case AccessControlInfoID.Mass:
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

