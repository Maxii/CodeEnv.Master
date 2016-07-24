// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidInfoAccessController.cs
// Controls access to Planetoid info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Planetoid info.
    /// </summary>
    public class PlanetoidInfoAccessController : AIntelInfoAccessController {

        public PlanetoidInfoAccessController(PlanetoidData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                case AccessControlInfoID.CurrentHitPoints:
                case AccessControlInfoID.Health:
                case AccessControlInfoID.Resources:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                case AccessControlInfoID.MaxHitPoints:
                case AccessControlInfoID.Defense:
                case AccessControlInfoID.Mass:
                case AccessControlInfoID.Capacity:
                    return true;
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Essential(AccessControlInfoID infoID, Player player) {
            switch (infoID) {
                case AccessControlInfoID.Category:
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
                case AccessControlInfoID.OrbitalSpeed:
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

