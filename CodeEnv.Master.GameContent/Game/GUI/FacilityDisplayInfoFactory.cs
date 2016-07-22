// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Facilities.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Factory that makes instances of text containing info about Facilities.
    /// </summary>
    public class FacilityDisplayInfoFactory : AElementItemDisplayInfoFactory<FacilityReport, FacilityDisplayInfoFactory> {

        private static AccessControlInfoID[] _infoIDsToDisplay = new AccessControlInfoID[] {
            AccessControlInfoID.Name,
            AccessControlInfoID.ParentName,
            AccessControlInfoID.Owner,
            AccessControlInfoID.Category,
            AccessControlInfoID.Health,
            AccessControlInfoID.Defense,
            AccessControlInfoID.Offense,
            AccessControlInfoID.WeaponsRange,
            //AccessControlInfoID.SensorRange,  // makes no sense
            AccessControlInfoID.Science,
            AccessControlInfoID.Culture,
            AccessControlInfoID.NetIncome,
            AccessControlInfoID.Mass,

            AccessControlInfoID.CameraDistance,
            AccessControlInfoID.IntelState
        };

        protected override AccessControlInfoID[] InfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private FacilityDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(AccessControlInfoID infoID, FacilityReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case AccessControlInfoID.Category:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Category != FacilityHullCategory.None ? report.Category.GetValueName() : _unknown);
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(infoID));
                }
            }
            return isSuccess;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

