// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Sectors.
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
    /// Factory that makes instances of text containing info about Sectors.
    /// </summary>
    public class SectorDisplayInfoFactory : AItemDisplayInfoFactory<SectorReport, SectorDisplayInfoFactory> {

        private static AccessControlInfoID[] _infoIDsToDisplay = new AccessControlInfoID[] {
            AccessControlInfoID.Name,
            AccessControlInfoID.Owner,
            AccessControlInfoID.SectorIndex,

            AccessControlInfoID.CameraDistance
        };

        protected override AccessControlInfoID[] InfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private SectorDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(AccessControlInfoID infoID, SectorReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case AccessControlInfoID.SectorIndex:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.SectorIndex.ToString());
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

