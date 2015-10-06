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

        private static ContentID[] _contentIDsToDisplay = new ContentID[] {                         
            ContentID.Name,
            ContentID.ParentName,
            ContentID.Owner,
            ContentID.Category,
            ContentID.Health,
            ContentID.Defense,
            ContentID.Offense,
            ContentID.WeaponsRange,
            ContentID.SensorRange,
            ContentID.Science,
            ContentID.Culture,
            ContentID.NetIncome,
            ContentID.Mass,

            ContentID.CameraDistance,
            ContentID.IntelState
        };

        protected override ContentID[] ContentIDsToDisplay { get { return _contentIDsToDisplay; } }

        private FacilityDisplayInfoFactory() {
            Initialize();
        }

        protected override void Initialize() { }

        protected override bool TryMakeColorizedText(ContentID contentID, FacilityReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(contentID, report, out colorizedText);
            if (!isSuccess) {
                switch (contentID) {
                    case ContentID.Category:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Category != FacilityHullCategory.None ? report.Category.GetValueName() : _unknown);
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(contentID));
                }
            }
            return isSuccess;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

