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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Starbase info.
    /// </summary>
    public class StarbaseInfoAccessController : AIntelInfoAccessController {

        public StarbaseInfoAccessController(StarbaseCmdData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(AccessControlInfoID infoID) {
            switch (infoID) {
                case AccessControlInfoID.CurrentCmdEffectiveness:
                case AccessControlInfoID.UnitScience:
                case AccessControlInfoID.UnitNetIncome:
                case AccessControlInfoID.Resources:
                case AccessControlInfoID.UnitCulture:
                case AccessControlInfoID.Capacity:
                case AccessControlInfoID.UnitCurrentHitPts:
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
                case AccessControlInfoID.Formation:
                case AccessControlInfoID.Composition:
                case AccessControlInfoID.UnitOffense:
                case AccessControlInfoID.UnitDefense:
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

