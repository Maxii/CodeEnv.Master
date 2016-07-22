// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarInfoAccessController.cs
// Controls access to Star info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Controls access to Star info.
    /// </summary>
    public class StarInfoAccessController : AIntelInfoAccessController {

        public StarInfoAccessController(StarData data) : base(data) { }

        protected override bool HasAccessToInfo_Comprehensive(AccessControlInfoID infoID) {
            switch (infoID) {
                case AccessControlInfoID.Resources:
                default:
                    return false;
            }
        }

        protected override bool HasAccessToInfo_Broad(AccessControlInfoID infoID) {
            switch (infoID) {
                case AccessControlInfoID.Capacity:
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

