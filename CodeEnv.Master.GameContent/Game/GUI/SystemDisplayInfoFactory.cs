// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Systems.
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
    /// Factory that makes instances of text containing info about Systems.
    /// </summary>
    public class SystemDisplayInfoFactory : AItemDisplayInfoFactory<SystemReport, SystemDisplayInfoFactory> {

        private static AccessControlInfoID[] _infoIDsToDisplay = new AccessControlInfoID[] {
            AccessControlInfoID.Name,
            AccessControlInfoID.Owner,
            AccessControlInfoID.SectorIndex,
            AccessControlInfoID.Capacity,
            AccessControlInfoID.Resources,

            AccessControlInfoID.CameraDistance
        };

        protected override AccessControlInfoID[] InfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private SystemDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(AccessControlInfoID infoID, SystemReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case AccessControlInfoID.SectorIndex:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.SectorIndex.ToString());
                        break;
                    case AccessControlInfoID.Capacity:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Capacity.HasValue ? GetFormat(infoID).Inject(report.Capacity.Value) : _unknown);
                        break;
                    case AccessControlInfoID.Resources:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Resources.HasValue ? report.Resources.Value.ToString() : _unknown);
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

