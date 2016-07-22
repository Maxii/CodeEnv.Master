// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Stars.
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
    /// Factory that makes instances of text containing info about Stars.
    /// </summary>
    public class StarDisplayInfoFactory : AIntelItemDisplayInfoFactory<StarReport, StarDisplayInfoFactory> {

        private static AccessControlInfoID[] _infoIDsToDisplay = new AccessControlInfoID[] {
            AccessControlInfoID.Name,
            AccessControlInfoID.ParentName,
            AccessControlInfoID.Category,
            AccessControlInfoID.Owner,
            AccessControlInfoID.Capacity,
            AccessControlInfoID.Resources,
            AccessControlInfoID.SectorIndex,

            AccessControlInfoID.IntelState,
            AccessControlInfoID.CameraDistance
        };

        protected override AccessControlInfoID[] InfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private StarDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(AccessControlInfoID infoID, StarReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case AccessControlInfoID.ParentName:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.ParentName != null ? report.ParentName : _unknown);
                        break;
                    case AccessControlInfoID.Category:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Category != StarCategory.None ? report.Category.GetValueName() : _unknown);
                        break;
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

